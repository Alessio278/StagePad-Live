using System.Collections.Concurrent;
using System.IO;
using StagePad.Core;

namespace StagePad.App;
internal sealed class AudioCoordinator : IDisposable
{
    private readonly Mixer mixer = new();
    private readonly AudioDevice device;
    private readonly MemoryBudget budget;
    private readonly BlockingCollection<Action> jobs = new(256);
    private readonly SemaphoreSlim importLimit = new(2);
    private readonly CancellationTokenSource stop = new();
    private readonly Thread control;
    private readonly IAudioSource?[] sources = new IAudioSource?[Projects.MaxSlots];
    private readonly string?[] errors = new string?[Projects.MaxSlots];
    private readonly bool[] loading = new bool[Projects.MaxSlots], seeking=new bool[Projects.MaxSlots];
    private readonly int[] seekSerial=new int[Projects.MaxSlots];
    private readonly SeekBuffer?[] seekBuffers=new SeekBuffer?[Projects.MaxSlots];
    private readonly List<(long Ticket,SeekBuffer Buffer)> retiredBuffers=[];
    private readonly ConcurrentQueue<int> failedSeeks=new();
    private long seekEpoch;
    private readonly int[] generation = new int[Projects.MaxSlots];
    private readonly List<(long Ticket, IAudioSource Source)> retired = [];
    private readonly ConcurrentQueue<(int Index, int Stamp)> failedCompletions = new();
    private readonly Dictionary<int, int> heldNotes = [];
    private readonly HashSet<int> learnConsumed = [];
    private readonly string instance = Guid.NewGuid().ToString("N"), cache, pipe;
    private readonly RecoveryWriter recovery = new();
    private readonly OscOutput oscOutput=new();
    private readonly MidiLink inputLink, outputLink;
    private StateInfo? snapshot;
    private ProjectDocument project = new();
    private MidiPortState? midiIn, midiOut;
    private string? projectError, projectPath, inputConnection;
    private bool locked, readingProject, savingProject;
    private int learn = -1, lastStarted;
    private string? volumeLearn;
    private MidiCcBinding? learnTemplate;
    private readonly VolumeControl masterPickup=new(),trackPickup=new();
    private bool maintenance;
    private readonly long[] audioOrder=new long[Projects.MaxSlots];
    private long audioSequence,lastTriggerTick;
    private long ticket, triggerSerial, projectRevision, lastPanicTick;
    private int controlEpoch;
    public CancellationToken Shutdown => stop.Token;
    public AudioCoordinator(string pipe)
    {
        this.pipe = pipe;
        project=ProjectFiles.ReadStartup(out projectPath,out projectError);
        budget = new(project.MemoryMiB * 1048576L);
        cache = System.IO.Path.Combine(Log.Root, "cache", instance);
        device = new(mixer); inputLink = new(pipe + ".in"); outputLink = new(pipe + ".out");
        control = new Thread(Run) { IsBackground = true, Name = "StagePad audio control" };
        jobs.Add(() => ApplyProject(project)); control.Start();
        _ = Task.Run(MonitorMidi);
    }
    public Task<Reply> Handle(Request r)
    {
        if (r.Op == "state") return Task.FromResult(new Reply(true, State: Volatile.Read(ref snapshot)));
        if (r.Op == "panic") { Interlocked.Increment(ref controlEpoch);Interlocked.Increment(ref seekEpoch); Volatile.Write(ref lastPanicTick, Environment.TickCount64); mixer.Panic(); outputLink.Panic();oscOutput.Panic(); return Task.FromResult(new Reply(true)); }
        if (r.Op == "shutdown") { Interlocked.Increment(ref seekEpoch); mixer.Panic(); outputLink.Panic();oscOutput.Panic(); stop.Cancel(); return Task.FromResult(new Reply(true)); }
        int acceptedEpoch=Volatile.Read(ref controlEpoch);
        var tcs = new TaskCompletionSource<Reply>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!jobs.TryAdd(() =>
        {
            try
            {
                if(r.Op is "play" or "pad-down" or "midi-down" or "seek" && acceptedEpoch!=Volatile.Read(ref controlEpoch)){tcs.TrySetResult(new(false,"Sequenza annullata."));return;}
                if(r.Op=="save-project")StartSave(r,tcs);
                else if(r.Op=="load-project")StartOpen(r,tcs);
                else if(r.Op is "export-show" or "import-show")StartBundle(r,tcs);
                else if(r.Op=="test-command")StartCommandTest(r,tcs);
                else if(r.Op is "rename-project" or "duplicate-project" or "delete-project")StartManage(r,tcs);
                else tcs.TrySetResult(Execute(r));
            }
            catch (Exception ex) { Log.Error("command", ex); tcs.TrySetResult(new(false, ex.Message)); }
        })) tcs.SetResult(new(false, "Coda comandi piena. PANIC resta disponibile."));
        return tcs.Task;
    }
    private void StartCommandTest(Request request,TaskCompletionSource<Reply> completion)
    {
        if(locked || maintenance || Busy() || readingProject || savingProject || heldNotes.Count>0 || oscOutput.Pending>0 || (midiOut?.ActiveNotes??0)>0 || (midiOut?.PendingCommands??0)>0 || loading.Any(v=>v)){completion.TrySetResult(new(false,"Ferma audio e sequenze prima del test."));return;}
        lastTriggerTick=Environment.TickCount64;
        var settings=request.Settings??throw new InvalidDataException("Parametri mancanti.");
        if(request.Origin=="midi")
        {
            var commands=SlotActions.MidiList(settings);if(commands.Length!=1)throw new InvalidDataException("Seleziona un solo comando per il test.");Projects.ValidateMidi(commands[0]);
            _=Task.Run(async()=>{try{completion.TrySetResult(await Wire.Call(pipe+".out",new("test-send",Settings:settings),4000));}catch(Exception ex){completion.TrySetResult(new(false,ex.Message));}});
        }
        else if(request.Origin=="osc" && settings.OscCommands.Length==1)
        {OscCodec.Validate(settings.OscCommands[0]);_=Task.Run(async()=>completion.TrySetResult(await oscOutput.Test(settings.OscCommands[0])));}
        else completion.TrySetResult(new(false,"Seleziona un solo comando per il test."));
    }
    private void StartBundle(Request request,TaskCompletionSource<Reply> completion)
    {
        if(locked || maintenance || Busy() || readingProject || savingProject || heldNotes.Count>0 || oscOutput.Pending>0 || (midiOut?.ActiveNotes??0)>0 || (midiOut?.PendingCommands??0)>0 || loading.Any(v=>v) || seeking.Any(v=>v)){completion.TrySetResult(new(false,"Ferma audio e sequenze prima di gestire il pacchetto."));return;}
        if(string.IsNullOrWhiteSpace(request.Path) || string.IsNullOrWhiteSpace(request.TargetPath))throw new InvalidDataException("Percorso mancante.");
        maintenance=true;var snapshot=project;
        _=Task.Run(()=>
        {
            string? failure=null;
            try
            {
                if(request.Op=="export-show")ShowBundle.Export(request.Path,string.Equals(projectPath,request.TargetPath,StringComparison.OrdinalIgnoreCase)?snapshot:ProjectFiles.Read(request.TargetPath),stop.Token);
                else ShowBundle.Import(request.Path,System.IO.Path.GetDirectoryName(request.TargetPath)!,System.IO.Path.GetFileName(request.TargetPath),stop.Token);
            }
            catch(Exception ex){failure=ex.Message;Log.Error("show-bundle",ex);}
            if(!jobs.TryAdd(()=>{maintenance=false;completion.TrySetResult(new(failure==null,failure));},100))completion.TrySetResult(new(false,failure??"Operazione annullata."));
        });
    }
    private void StartSave(Request request,TaskCompletionSource<Reply> completion)
    {
        if(maintenance){completion.TrySetResult(new(false,"Manutenzione in corso: attendi il completamento."));return;}
        if(string.IsNullOrWhiteSpace(request.Path)){completion.TrySetResult(new(false,"Percorso progetto mancante."));return;}
        if(readingProject){completion.TrySetResult(new(false,"Lettura progetto in corso."));return;}
        if(savingProject){completion.TrySetResult(new(false,"Salvataggio già in corso."));return;}
        string path=System.IO.Path.GetFullPath(request.Path);var value=project with {Name=System.IO.Path.GetFileNameWithoutExtension(path)[..Math.Min(100,System.IO.Path.GetFileNameWithoutExtension(path).Length)]};savingProject=true;
        _=Task.Run(()=>
        {
            string? error=null;
            try{ProjectFiles.Write(path,value,true);}catch(Exception ex){error="Salvataggio non riuscito: "+ex.Message;Log.Error("project-save",ex);}
            if(!jobs.TryAdd(()=>
            {
                savingProject=false;projectError=error;
                if(error==null){projectPath=path;project=project with {Name=value.Name};Changed();}
                completion.TrySetResult(new(error==null,error));
            },100))
            {
                savingProject=false;
                completion.TrySetResult(new(false,error??"File salvato, ma stato non aggiornato. Controlla l’elenco progetti."));
            }
        });
    }
    private void StartOpen(Request request,TaskCompletionSource<Reply> completion)
    {
        if(maintenance){completion.TrySetResult(new(false,"Manutenzione in corso: attendi il completamento."));return;}
        void Fail(string error)=>completion.TrySetResult(new(false,error));
        if(locked){Fail("SHOW LOCK attivo: modifiche bloccate.");return;}
        if(readingProject){Fail("Lettura progetto in corso.");return;}
        if(savingProject){Fail("Salvataggio già in corso.");return;}
        if(Busy() || loading.Any(x=>x) || seeking.Any(x=>x)){Fail("Ferma le tracce e attendi i caricamenti prima di aprire un progetto.");return;}
        if(string.IsNullOrWhiteSpace(request.Path)){Fail("Percorso mancante.");return;}
        string path=System.IO.Path.GetFullPath(request.Path);long revision=projectRevision;readingProject=true;
        _=Task.Run(()=>
        {
            ProjectDocument? next=null;string? error=null;
            try{next=ProjectFiles.Read(path);}catch(Exception ex){error=ex.Message;Log.Error("project-open",ex);}
            if(!jobs.TryAdd(()=>
            {
                readingProject=false;
                if(error!=null){projectError=error;Fail(error);return;}
                if(locked || Busy() || seeking.Any(x=>x) || revision!=projectRevision)
                {projectError="Apertura annullata: stato del progetto cambiato durante la lettura.";Fail(projectError);return;}
                if(mixer.CommandCapacityRemaining<Projects.MaxSlots*3+1)
                {projectError="Motore occupato: riprova ad aprire il progetto.";Fail(projectError);return;}
                projectPath=path;projectError=null;ApplyProject(next! with {Name=System.IO.Path.GetFileNameWithoutExtension(path)[..Math.Min(100,System.IO.Path.GetFileNameWithoutExtension(path).Length)]});completion.TrySetResult(new(true));
            },100))
            {readingProject=false;Fail("Coda piena, apertura annullata.");}
        });
    }
    private void StartManage(Request request,TaskCompletionSource<Reply> completion)
    {
        if(maintenance){completion.TrySetResult(new(false,"Manutenzione in corso: attendi il completamento."));return;}
        if(locked){completion.TrySetResult(new(false,"SHOW LOCK attivo: modifiche bloccate."));return;}
        if(readingProject || savingProject){completion.TrySetResult(new(false,"Operazione sui progetti in corso."));return;}
        string path=System.IO.Path.GetFullPath(request.Path??throw new InvalidDataException("Percorso mancante."));
        ProjectManagement.Validate(path,request.TargetPath,request.Op);
        savingProject=true;
        _=Task.Run(()=>
        {
            string? error=null;
            try{ProjectManagement.Execute(path,request.TargetPath,request.Op);}catch(Exception ex){error=ex.Message;Log.Error("project-manage",ex);}
            if(!jobs.TryAdd(()=>
            {
                savingProject=false;projectError=error;
                if(error==null && string.Equals(projectPath,path,StringComparison.OrdinalIgnoreCase))
                {
                    if(request.Op=="rename-project")
                    {projectPath=System.IO.Path.GetFullPath(request.TargetPath!);project=project with {Name=System.IO.Path.GetFileNameWithoutExtension(projectPath)};Changed();}
                    else if(request.Op=="delete-project"){projectPath=null;Changed();}
                }
                completion.TrySetResult(new(error==null,error));
            },100)){savingProject=false;completion.TrySetResult(new(false,error??"Coda comandi piena."));}
        });
    }
    private bool Busy(int? slot = null) => slot is int i ? mixer.View(i).State is VoiceState.Playing or VoiceState.Paused or VoiceState.Stopping : Enumerable.Range(0, project.SlotCount).Any(i => Busy(i));
    private void Changed()
    { projectRevision++; recovery.Schedule(project,projectPath); }
    private void SetSlot(int i, SlotSettings settings)
    { var slots = (SlotSettings[])project.Slots.Clone(); slots[i] = settings; var next = project with { Slots = slots }; Projects.Validate(next); project = next; Changed(); }
    private Reply Execute(Request r)
    {
        if (r.Op == "lock") { locked = r.Flag; if (locked){ learn = -1;volumeLearn=null;} return new(true); }
        if (r.Op == "wave") return new(true, Peaks: sources[(uint)r.Slot < project.SlotCount ? r.Slot : 0] is OwnedSource owned ? owned.Peaks : []);
        if (r.Op is "midi-down" or "pad-down" or "play" or "seek" && r.Timestamp > 0 && r.Timestamp < Volatile.Read(ref lastPanicTick)) return new(false, "Comando precedente a PANIC scartato.");
        if(r.Op=="midi-disconnected"){if(r.Origin==inputConnection)ReleaseInput();return new(true);}
        if(maintenance && r.Op!="cancel-update")return new(false,"Manutenzione in corso: attendi il completamento.");
        if(r.Op=="cancel-update"){maintenance=false;return new(true);}
        if(r.Op=="prepare-update"){if(readingProject || savingProject || Busy() || loading.Any(v=>v) || heldNotes.Count>0 || oscOutput.Pending>0 || (midiOut?.ActiveNotes??0)>0 || (midiOut?.PendingCommands??0)>0 || Environment.TickCount64-lastTriggerTick<2000)return new(false,"Ferma audio e sequenze prima di aggiornare.");maintenance=true;return new(true);}
        if(r.Op=="midi-cc")return MidiControl(r);
        if (r.Op is "midi-down" or "midi-up") return MidiInput(r);
        if (r.Op is "test" or "test-midi" && locked) return new(false, "SHOW LOCK: test di uscita bloccati.");
        if (r.Op is "load-project" or "settings" or "load" or "clear" or "configure" or "learn" or "budget" or "grid" or "slot-count")
        { if (locked) return new(false, "SHOW LOCK attivo: modifiche bloccate."); if (readingProject) return new(false, "Lettura progetto in corso."); }
        if(r.Op is "midi-volume-controls" or "learn-volume")
        {
            if(locked || readingProject)return new(false,"SHOW LOCK attivo: modifiche bloccate.");
            if(r.Op=="learn-volume")
            {
                if(r.Origin is not ("master" or "track"))return new(false,"Assegnazione volume MIDI non valida: canale 1-16 e CC da -1 a 127.");
                volumeLearn=r.Flag?r.Origin:null;
                learnTemplate=r.Origin=="master"?r.MasterControl??project.MasterControl:r.TrackControl??project.CurrentTrackControl;
                MidiCcBinding.Validate(learnTemplate with{Controller=-1},new());return new(true);
            }
            MidiCcBinding.Validate(r.MasterControl,r.TrackControl);
            project=project with{MasterControl=r.MasterControl!,CurrentTrackControl=r.TrackControl!};volumeLearn=null;masterPickup.Reset();trackPickup.Reset();Changed();return new(true);
        }
        if (r.Op == "configure")
        {
            var route = r.Routing ?? project.Routing with { AudioDevice = r.DeviceId, BufferMs = r.Latency };
            bool audioChanged = route.BufferMs != project.Routing.BufferMs || (route.AudioDevice != project.Routing.AudioDevice && route.AudioDevice != device.DeviceId);
            if (audioChanged && (Busy() || loading.Any(x => x) || seeking.Any(x=>x))) return new(false, "Ferma le tracce prima di cambiare uscita audio o buffer.");
            var next = project with { Routing = route }; Projects.Validate(next);
            project = next; Changed(); if (audioChanged) device.Configure(route.AudioDevice, route.BufferMs);
            inputLink.Configure(route.MidiInput); outputLink.Configure(route.MidiOutput); return new(true);
        }
        if(r.Op=="slot-count")
        {
            int count=(int)r.Value;
            if(r.Value!=count || count is <1 or >Projects.MaxSlots)return new(false,"Numero slot: da 1 a 100.");
            if(count==project.SlotCount)return new(true);
            if(Busy() || loading.Any(x=>x) || seeking.Any(x=>x))return new(false,"Ferma le tracce e attendi i caricamenti prima di cambiare il numero di slot.");
            var next=Projects.Resize(project,count);
            int previous=project.SlotCount;
            // Reserve enough queue space before mutating any voice or project.
            if(mixer.CommandCapacityRemaining<Projects.MaxSlots*3)return new(false,"Motore occupato: riprova tra un istante.");
            project=next;learn=-1;heldNotes.Clear();learnConsumed.Clear();outputLink.Panic();oscOutput.Panic();
            lastStarted=Math.Min(lastStarted,count-1);
            for(int index=count;index<previous;index++)
            { generation[index]++;seekSerial[index]++;Install(index,null); }
            for(int index=previous;index<count;index++)
            {
                ConfigureVoice(index);Posted(new(AudioOp.Volume,index,project.Slots[index].Volume));
                if(project.Slots[index].File is {} file)StartImport(index,file);
            }
            Changed();return new(true);
        }
        if(r.Op=="grid")
        {
            var layout=new GridLayout(r.Rows,r.Columns);
            var next=r.Origin=="setup" ? project with { SetupGrid=layout } : project with { Grid=layout };Projects.Validate(next);
            project=next;Changed();return new(true);
        }
        if (r.Op == "budget")
        {
            int value = (int)r.Value;
            if (value is < 128 or > 4096 || !budget.SetLimit(value * 1048576L)) return new(false, "Budget 128–4096 MiB richiesto, non inferiore alla RAM già riservata.");
            project = project with { MemoryMiB = value }; Changed(); return new(true);
        }
        if (r.Op == "test") return Posted(new(AudioOp.Test));
        if (r.Op == "test-midi")
        {
            if ((uint)r.Slot >= project.SlotCount) return new(false, "Slot non valido.");
            return outputLink.Post(new("send", r.Slot, Settings: project.Slots[r.Slot] with { MidiCommands=SlotActions.MidiList(project.Slots[r.Slot]).Select(m=>m with {Behavior=NoteBehavior.Pulse}).ToArray() })) ? new(true) : new(false, outputLink.Alert);
        }
        if (r.Op == "master")
        {
            if (!float.IsFinite(r.Value) || r.Value is < 0 or > 1) return new(false, "Volume non valido.");
            var reply = Posted(new(AudioOp.Master, Value: r.Value));
            if (reply.Ok) { project = project with { Master = r.Value }; Changed(); } return reply;
        }
        if ((uint)r.Slot >= project.SlotCount) return new(false, "Slot non valido.");
        int i = r.Slot; var settings = project.Slots[i];
        switch (r.Op)
        {
            case "settings":
                if (r.Settings == null) return new(false, "Parametri mancanti.");
                if (Busy(i) || loading[i] || seeking[i]) return new(false, "Ferma lo slot prima di modificarlo.");
                var proposed=r.Settings with {File=settings.File,Volume=settings.Volume};
                if(proposed.Name!=settings.Name && !string.IsNullOrWhiteSpace(proposed.Name) && proposed.Name!=$"Slot {i+1}")proposed=proposed with{AutoName=false};
                proposed=SlotTitles.Normalize(proposed,i);
                SetSlot(i, proposed); return ConfigureVoice(i);
            case "learn": learn = r.Flag ? i : -1; return new(true);
            case "load":
                if (string.IsNullOrWhiteSpace(r.Path)) return new(false, "Seleziona un file.");
                if (loading[i] || seeking[i] || Busy(i)) return new(false, "Ferma lo slot e attendi il caricamento.");
                string path = System.IO.Path.GetFullPath(r.Path);
                SetSlot(i, SlotTitles.Normalize(settings,i) with { File = path });
                StartImport(i, path); return new(true);
            case "clear":
                if (loading[i] || seeking[i] || Busy(i)) return new(false, "Ferma lo slot e attendi il caricamento.");
                generation[i]++; var cleared = Install(i, null); if (cleared.Ok) SetSlot(i, settings with { File = null }); return cleared;
            case "play": case "pad-down": return Trigger(i);
            case "pad-up":
                outputLink.Post(new("release", i)); return Posted(new(AudioOp.Release, i));
            case "pause":
                if (settings.Mode == PlayMode.Protected) return new(false, "Senza interruzione: usa STOP con il mouse.");
                return Posted(new(AudioOp.Pause, i));
            case "seek": return BeginSeek(i,r.Value);
            case "stop": seekSerial[i]++;oscOutput.Cancel(i); outputLink.Post(new("cancel-slot", i)); return Posted(new(AudioOp.Stop, i));
            case "volume":
                if (!float.IsFinite(r.Value) || r.Value is < 0 or > 1) return new(false, "Volume non valido.");
                var volume = Posted(new(AudioOp.Volume, i, r.Value)); if (volume.Ok) SetSlot(i, settings with { Volume = r.Value }); return volume;
            default: return new(false, "Comando sconosciuto.");
        }
    }
    private Reply MidiInput(Request r)
    {
        if (r.Channel is < 1 or > 16 || r.Note is < 0 or > 127) return new(false, "Nota MIDI non valida.");
        if (inputConnection != r.Origin) { ReleaseInput();inputConnection = r.Origin; }
        int key = r.Channel * 128 + r.Note;
        if (r.Op == "midi-up")
        {
            if (learnConsumed.Remove(key)) return new(true);
            if (!heldNotes.Remove(key, out int slot)) return new(true);
            outputLink.Post(new("release", slot)); return Posted(new(AudioOp.Release, slot));
        }
        if (learn >= 0 && !locked)
        {
            int target = learn;
            if (project.Slots.Where((_, i) => i != target).Any(s => s.PadChannel == r.Channel && s.PadNote == r.Note)) return new(false, "Questo pad è già associato a un altro slot.");
            SetSlot(target, project.Slots[target] with { PadChannel = r.Channel, PadNote = r.Note }); learn = -1; learnConsumed.Add(key); return new(true);
        }
        if (heldNotes.ContainsKey(key)) return new(true);
        int index = Array.FindIndex(project.Slots, 0, project.SlotCount, s => s.PadChannel == r.Channel && s.PadNote == r.Note);
        if (index < 0) return new(true);
        heldNotes[key] = index; return Trigger(index);
    }
    private void ReleaseInput()
    {
        foreach(int slot in heldNotes.Values.Distinct()){outputLink.Post(new("release",slot));Posted(new(AudioOp.Release,slot));}
        heldNotes.Clear();learnConsumed.Clear();
    }
    private int CurrentPlaying()
    {
        int current=-1;long latest=-1;
        for(int i=0;i<project.SlotCount;i++)if(mixer.View(i).State==VoiceState.Playing && audioOrder[i]>latest){current=i;latest=audioOrder[i];}
        return current;
    }
    private Reply MidiControl(Request r)
    {
        if(r.Channel is <1 or >16 || r.Controller is <0 or >127 || !float.IsFinite(r.Value) || r.Value is <0 or >127 || r.Value!=(int)r.Value)return new(false,"Valore MIDI CC non valido.");
        if(volumeLearn!=null && !locked)
        {
            var binding=(learnTemplate??new()) with{Channel=r.Channel,Controller=r.Controller};
            var master=volumeLearn=="master"?binding:project.MasterControl;
            var track=volumeLearn=="track"?binding:project.CurrentTrackControl;
            MidiCcBinding.Validate(master,track);project=project with{MasterControl=master,CurrentTrackControl=track};volumeLearn=null;masterPickup.Reset();trackPickup.Reset();Changed();return new(true);
        }
        float value;
        if(project.MasterControl.Matches(r.Channel,r.Controller))
        {
            if(masterPickup.Apply(project.MasterControl,(int)r.Value,project.Master,-1) is not float masterValue)return new(true);value=masterValue;
            if(value==project.Master)return new(true);
            var reply=Posted(new(AudioOp.Master,Value:value));if(reply.Ok){project=project with{Master=value};Changed();}return reply;
        }
        if(project.CurrentTrackControl.Matches(r.Channel,r.Controller) && CurrentPlaying() is >=0 and var current)
        {
            if(trackPickup.Apply(project.CurrentTrackControl,(int)r.Value,project.Slots[current].Volume,current) is not float trackValue)return new(true);value=trackValue;
            if(value==project.Slots[current].Volume)return new(true);
            var reply=Posted(new(AudioOp.Volume,current,value));if(reply.Ok)SetSlot(current,project.Slots[current] with{Volume=value});return reply;
        }
        return new(true);
    }
    private Reply Trigger(int i)
    {
        seekSerial[i]++;lastTriggerTick=Environment.TickCount64;
        var s = project.Slots[i]; Reply result = new(true);
        if(s.EmergencyPauseAll)
        {
            Interlocked.Increment(ref controlEpoch);Interlocked.Increment(ref seekEpoch);
            for(int slot=0;slot<seekSerial.Length;slot++)seekSerial[slot]++;
            mixer.EmergencyPause();outputLink.Panic();oscOutput.Panic();
            foreach(int key in heldNotes.Where(p=>p.Value!=i).Select(p=>p.Key).ToArray())heldNotes.Remove(key);
        }
        else if (SlotActions.Audio(s.Action))
        {
            result = loading[i] || sources[i] == null || errors[i] != null || sources[i]?.Error != null ? new(false, "Audio slot non pronto.") : Posted(new(AudioOp.Trigger, i));
        }
        if(!s.EmergencyPauseAll && SlotActions.Audio(s.Action) && result.Ok && !(mixer.View(i).State==VoiceState.Playing && s.Mode is PlayMode.Protected or PlayMode.TogglePause))audioOrder[i]=++audioSequence;
        // Independent dispatch: missing audio never prevents MIDI or OSC commands.
        var failures=new List<string>();if(!result.Ok)failures.Add(result.Error??"Audio slot non pronto.");
        if(SlotActions.Midi(s.Action) && !outputLink.Post(new("send",i,Settings:s)))failures.Add(outputLink.Alert??"Comando non riuscito.");
        if(SlotActions.Osc(s.Action) && !oscOutput.Post(s.OscCommands,i))failures.Add(oscOutput.Error??"Comando non riuscito.");
        result=failures.Count==0?new(true):new(false,string.Join(" · ",failures));
        lastStarted = i; triggerSerial++; return result;
    }
    private Reply BeginSeek(int i,float normalized)
    {
        if(readingProject)return new(false,"Lettura progetto in corso.");
        if(!float.IsFinite(normalized) || normalized is <0 or >1)return new(false,"Posizione non valida.");
        if(loading[i] || sources[i]==null || errors[i]!=null)return new(false,"Audio dello slot non pronto.");
        if(seeking[i])return new(false,"Spostamento già in preparazione.");
        var source=sources[i]!;long frame=(long)Math.Round(normalized*source.Frames);int stamp=++seekSerial[i];long epoch=Volatile.Read(ref seekEpoch);
        seeking[i]=true;
        _=Task.Run(()=>
        {
            SeekBuffer? buffer=null;string? error=null;
            try { if(source is OwnedSource owned)buffer=owned.PrepareSeek(frame,stop.Token); }
            catch(Exception ex){error=ex.Message;Log.Error("audio-seek",ex);}
            var prepared=buffer;var problem=error;
            Action complete=()=>
            {
                seeking[i]=false;
                if(stamp!=seekSerial[i] || epoch!=Volatile.Read(ref seekEpoch) || !ReferenceEquals(source,sources[i])) { if(prepared!=null)budget.Release(prepared.ResidentBytes);return; }
                if(problem!=null){if(prepared!=null)budget.Release(prepared.ResidentBytes);projectError="Spostamento non riuscito: "+problem;return;}
                long next=++ticket;
                if(!mixer.Post(new(AudioOp.Seek,i,Ticket:next,Frame:frame,Seek:prepared)))
                { if(prepared!=null)budget.Release(prepared.ResidentBytes);projectError="Coda audio piena: spostamento annullato.";return; }
                if(seekBuffers[i] is {} old)retiredBuffers.Add((next,old));seekBuffers[i]=prepared;
            };
            if(!jobs.TryAdd(complete,100))
            { if(prepared!=null)budget.Release(prepared.ResidentBytes);failedSeeks.Enqueue(i); }
        });
        return new(true);
    }
    private void ApplyProject(ProjectDocument value)
    {
        Projects.Validate(value); project = value; learn = -1; heldNotes.Clear(); learnConsumed.Clear();
        budget.SetLimit(value.MemoryMiB * 1048576L, true); mixer.Panic(); outputLink.Panic();oscOutput.Panic();
        device.Configure(value.Routing.AudioDevice, value.Routing.BufferMs);
        inputLink.Configure(value.Routing.MidiInput); outputLink.Configure(value.Routing.MidiOutput);
        Posted(new(AudioOp.Master, Value: value.Master));
        for (int i = 0; i < Projects.MaxSlots; i++)
        {
            generation[i]++;seekSerial[i]++;Install(i,null);
            if(i>=value.SlotCount)continue;
            ConfigureVoice(i);Posted(new(AudioOp.Volume,i,value.Slots[i].Volume));
            if(value.Slots[i].File is {} file)StartImport(i,file);
        }
        lastStarted=0;volumeLearn=null;masterPickup.Reset();trackPickup.Reset();Array.Clear(audioOrder);
        Changed();
    }
    private Reply ConfigureVoice(int i)
    { var s = project.Slots[i]; return Posted(new(AudioOp.Configure, i, Mode: s.Mode, FadeInMs: s.FadeInMs, FadeOutMs: s.FadeOutMs)); }
    private void StartImport(int i, string path)
    {
        loading[i] = true; errors[i] = null; int stamp = ++generation[i];
        _ = Task.Run(async () =>
        {
            IAudioSource? prepared = null; string? error = null; bool entered = false;
            try { await importLimit.WaitAsync(stop.Token); entered = true; prepared = Importer.Import(path, cache, budget, stop.Token); }
            catch (Exception ex) { error = ex.Message; Log.Error("import", ex); }
            finally { if (entered) importLimit.Release(); }
            var result = prepared; var problem = error;
            if (!jobs.TryAdd(() => Complete(i, stamp, result, problem)))
            { result?.Dispose(); failedCompletions.Enqueue((i, stamp)); }
        });
    }
    private Reply Posted(AudioCommand command) => mixer.Post(command) ? new(true) : new(false, "Coda audio piena. PANIC resta disponibile.");
    private Reply Install(int i, IAudioSource? source)
    {
        long next = ++ticket;
        if (!mixer.Post(new(AudioOp.Install, i, Source: source, Ticket: next))) return new(false, "Coda audio piena.");
        if (sources[i] is { } previous) retired.Add((next, previous));
        if(seekBuffers[i] is {} preview){retiredBuffers.Add((next,preview));seekBuffers[i]=null;}
        sources[i] = source; errors[i] = null; return new(true);
    }
    private void Complete(int i, int stamp, IAudioSource? source, string? error)
    {
        if (generation[i] != stamp) { source?.Dispose(); return; }
        loading[i] = false;
        if (error != null) { errors[i] = error; source?.Dispose(); return; }
        var reply = Install(i, source);
        if (!reply.Ok) { errors[i] = reply.Error; source?.Dispose(); }
    }
    private async Task MonitorMidi()
    {
        string? previousIn = null, previousOut = null;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                var results = await Task.WhenAll(ReadPort(pipe + ".in"), ReadPort(pipe + ".out"));
                if((results[0]==null || results[0]?.Error!=null || results[0]?.Status=="Disabilitato"))jobs.TryAdd(ReleaseInput);
                Volatile.Write(ref midiIn, results[0]); Volatile.Write(ref midiOut, results[1]);
                if (results[0] is { } input && input.Instance != previousIn && jobs.TryAdd(() => inputLink.Configure(project.Routing.MidiInput))) previousIn = input.Instance;
                if (results[1] is { } output && output.Instance != previousOut && jobs.TryAdd(() => outputLink.Configure(project.Routing.MidiOutput))) previousOut = output.Instance;
            }
            catch (Exception ex) { Log.Warn("midi-monitor", ex.Message); }
            try { await Task.Delay(500, stop.Token); } catch (OperationCanceledException) { return; }
        }
    }
    private static async Task<MidiPortState?> ReadPort(string name)
    { try { return (await Wire.Call(name, new("state"), 500)).Midi; } catch { return null; } }
    private void Run()
    {
        try
        {
            long nextSnapshot = 0; int previousFaults = 0;
            while (!stop.IsCancellationRequested)
            {
                if (jobs.TryTake(out var job, 20)) job();
                while(failedSeeks.TryDequeue(out int failed)){seeking[failed]=false;projectError="Coda piena: spostamento annullato.";}
                for(int j=retiredBuffers.Count-1;j>=0;j--)if(mixer.AppliedTicket>=retiredBuffers[j].Ticket){budget.Release(retiredBuffers[j].Buffer.ResidentBytes);retiredBuffers.RemoveAt(j);}
                while (failedCompletions.TryDequeue(out var failed)) if (generation[failed.Index] == failed.Stamp) { loading[failed.Index] = false; errors[failed.Index] = "Coda piena alla fine del caricamento: riprova."; }
                for (int j = retired.Count - 1; j >= 0; j--) if (mixer.AppliedTicket >= retired[j].Ticket) { var old = retired[j].Source; retired.RemoveAt(j); _ = Task.Run(old.Dispose); }
                if (Environment.TickCount64 < nextSnapshot) continue; nextSnapshot = Environment.TickCount64 + 100;
                if (mixer.CallbackFaults != previousFaults) { Log.Warn("audio-callback", $"Errori isolati: {mixer.CallbackFaults}"); previousFaults = mixer.CallbackFaults; }
                var slots = Enumerable.Range(0, project.SlotCount).Select(i => { var v = mixer.View(i); return new SlotInfo(i, SlotTitles.Display(project.Slots[i],i), v.State.ToString(), v.Frame / (double)Mixer.Rate, v.Frames / (double)Mixer.Rate, v.Volume, v.Starvations, errors[i] ?? sources[i]?.Error, loading[i], generation[i],seeking[i]); }).ToArray();
                Volatile.Write(ref snapshot, new(instance, device.Status, device.Error, device.DeviceId, device.Latency, mixer.Master, budget.Used, mixer.CallbackFaults,
                    Log.Unavailable, device.Devices, slots, project, locked, learn, projectError ?? recovery.Error, outputLink.Alert, Volatile.Read(ref midiIn), Volatile.Read(ref midiOut),
                    Environment.TickCount64, mixer.RenderHeartbeat, device.Heartbeat, lastStarted, triggerSerial, projectPath, readingProject, projectRevision,oscOutput.Error,oscOutput.Sent,volumeLearn,CurrentPlaying()));
            }
        }
        catch (Exception ex) { Log.Error("control-thread", ex); stop.Cancel(); }
    }
    public void Dispose() { stop.Cancel(); control.Join(1000);recovery.Dispose();oscOutput.Dispose();inputLink.Dispose();outputLink.Dispose();device.Dispose(); }
}
