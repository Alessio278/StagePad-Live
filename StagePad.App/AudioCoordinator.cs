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
    private readonly LoopRegion?[] loopBuffers=new LoopRegion?[Projects.MaxSlots];
    private readonly bool[] loopPreparing=new bool[Projects.MaxSlots];
    private readonly SeekBuffer?[] seekBuffers=new SeekBuffer?[Projects.MaxSlots];
    private readonly List<(long Ticket,SeekBuffer Buffer)> retiredBuffers=[];
    private readonly ConcurrentQueue<int> failedSeeks=new();
    private long seekEpoch;
    private readonly int[] markerVersions=new int[Projects.MaxSlots],signalEpochs=new int[Projects.MaxSlots];
    private long markerDrops;
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
    private bool markerLearn,markerSetup;
    private string? markerUi;
    private long markerUiSequence,markerUiTick;
    private int markerSlot,markerSourceRevision;
    private bool MarkerSetupActive=>markerSetup && Environment.TickCount64-markerUiTick<=1500;
    private Reply MarkerContext(Request request)
    {
        if(markerLearn && !MarkerSetupActive)markerLearn=false;
        if(!Guid.TryParseExact(request.Origin,"N",out _) || request.DeviceId!=instance)return new(false,"Contesto marker non aggiornato.");
        if(markerUi!=request.Origin)
        {
            if(MarkerSetupActive)return new(false,"Contesto marker non aggiornato.");
            markerUi=request.Origin;markerUiSequence=-1;
        }
        if(request.Epoch<=markerUiSequence)return new(true);
        markerUiSequence=request.Epoch;markerUiTick=Environment.TickCount64;
        markerSetup=request.Flag;markerSlot=request.Slot;markerSourceRevision=request.SourceRevision??-1;
        if(!markerSetup)markerLearn=false;
        return new(true);
    }
    private Reply CaptureMarker(Request request)
    {
        int i=markerSlot;
        if(!MarkerSetupActive || locked || maintenance || readingProject || (uint)i>=project.SlotCount || markerSourceRevision!=generation[i] || loading[i] || seeking[i] || errors[i]!=null || device.Error!=null)return new(true);
        long now=Environment.TickCount64;
        if(request.Timestamp<=0 || request.Timestamp>now || now-request.Timestamp>250 || now-mixer.RenderHeartbeat>250)return new(true);
        var view=mixer.View(i);if(view.State!=VoiceState.Playing || sources[i]?.Error!=null)return new(true);
        var settings=project.Slots[i];
        var marker=new AudioMarker{Seconds=view.Frame/(double)Mixer.Rate,Label="Marker "+(settings.Markers.Length+1)};
        var markers=AudioMarkers.Change(settings.Markers,"marker-add",marker,null,view.Frames/(double)Mixer.Rate);
        var reply=UpdateMarkerPlan(i,markers);if(!reply.Ok)return reply;
        SetSlot(i,settings with{Markers=markers});return new(true);
    }
    private readonly long[] audioOrder=new long[Projects.MaxSlots];
    private long audioSequence,lastTriggerTick;
    private long ticket, triggerSerial, projectRevision, lastPanicTick;
    private long savedProjectRevision,autoSaveDue,lastSavedAt;
    private bool autoSaveEnabled=true;
    private string autoSaveDirectory=ProjectFiles.DefaultDirectory;
    private Task? saveWorker;
    private string? pendingSavePath;
    private static string UntitledPath(string directory)=>System.IO.Path.Combine(directory,"Show "+DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")+"-"+Guid.NewGuid().ToString("N")[..6]+".json");
    private bool ProjectDirty=>projectRevision!=savedProjectRevision;
    private int controlEpoch,disposed;
    public CancellationToken Shutdown => stop.Token;
    public AudioCoordinator(string pipe)
    {
        this.pipe = pipe;
        try
        {
            string preferences=System.IO.Path.Combine(Log.Root,"settings.json");
            if(File.Exists(preferences))using(var settings=System.Text.Json.JsonDocument.Parse(File.ReadAllText(preferences)))
            {
                var values=settings.RootElement;autoSaveEnabled=!values.TryGetProperty("autoSaveProjects",out var enabled) || enabled.ValueKind!=System.Text.Json.JsonValueKind.False;
                if(values.TryGetProperty("projectDirectory",out var folder) && folder.ValueKind==System.Text.Json.JsonValueKind.String && folder.GetString() is {} directory)autoSaveDirectory=ProjectDirectoryPolicy.DirectoryPath(directory);
            }
        }
        catch(Exception ex){Log.Warn("autosave-settings",ex.Message);}
        project=ProjectFiles.ReadStartup(out projectPath,out projectError);
        budget = new(project.MemoryMiB * 1048576L);
        cache = System.IO.Path.Combine(Log.Root, "cache", instance);
        device = new(mixer); inputLink = new(pipe + ".in"); outputLink = new(pipe + ".out");
        control = new Thread(Run) { IsBackground = true, Name = "StagePad audio control" };
        jobs.Add(()=>
        {
            ApplyProject(project);
            bool clean=project.Slots.All(s=>s.File==null && s.Markers.Length==0 && s.PadNote<0);
            if(projectPath!=null)try{clean=System.Text.Json.JsonSerializer.Serialize(project,Projects.Json)==System.Text.Json.JsonSerializer.Serialize(ProjectFiles.Read(projectPath),Projects.Json);}catch{clean=false;}
            savedProjectRevision=clean?projectRevision:-1;
        });control.Start();
        _ = Task.Run(MonitorMidi);
    }
    public Task<Reply> Handle(Request r)
    {
        if (r.Op == "state" && r.Origin==null) return Task.FromResult(new Reply(true, State: Volatile.Read(ref snapshot)));
        if (r.Op == "panic") { Interlocked.Increment(ref controlEpoch);Interlocked.Increment(ref seekEpoch); Volatile.Write(ref lastPanicTick, Environment.TickCount64); mixer.Panic(); outputLink.Panic();oscOutput.Panic(); return Task.FromResult(new Reply(true)); }
        if (r.Op == "shutdown") { Interlocked.Increment(ref seekEpoch); mixer.Panic(); outputLink.Panic();oscOutput.Panic(); stop.Cancel(); return Task.FromResult(new Reply(true)); }
        int acceptedEpoch=Volatile.Read(ref controlEpoch);
        var tcs = new TaskCompletionSource<Reply>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!jobs.TryAdd(() =>
        {
            try
            {
                if(r.Op is "play" or "pad-down" or "midi-down" or "seek" && acceptedEpoch!=Volatile.Read(ref controlEpoch)){tcs.TrySetResult(new(false,"Sequenza annullata."));return;}
                if(r.Op=="marker-loop")StartLoop(r,tcs);
                else if(r.Op=="save-project")StartSave(r,tcs);
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
        if(string.IsNullOrWhiteSpace(request.Path) && request.Origin!="auto-save"){completion.TrySetResult(new(false,"Percorso progetto mancante."));return;}
        if(readingProject){completion.TrySetResult(new(false,"Lettura progetto in corso."));return;}
        if(savingProject){completion.TrySetResult(new(false,"Salvataggio già in corso."));return;}
        bool automatic=request.Origin=="auto-save",newAutomatic=automatic && projectPath==null;
        long savingRevision=projectRevision;string path=System.IO.Path.GetFullPath(request.Path??projectPath??UntitledPath(autoSaveDirectory));pendingSavePath=path;var value=project with {Name=System.IO.Path.GetFileNameWithoutExtension(path)[..Math.Min(100,System.IO.Path.GetFileNameWithoutExtension(path).Length)]};savingProject=true;
        saveWorker=Task.Run(()=>
        {
            string? error=null;
            try{if(newAutomatic && File.Exists(path))throw new IOException("Nome show già presente: salvataggio rinviato.");ProjectFiles.Write(path,value,true);}catch(Exception ex){error="Salvataggio non riuscito: "+ex.Message;Log.Error("project-save",ex);}
            if(!jobs.TryAdd(()=>
            {
                savingProject=false;pendingSavePath=null;projectError=error;
                if(error==null){lastSavedAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds();bool unchanged=projectRevision==savingRevision;projectPath=path;project=project with {Name=value.Name};Changed();if(unchanged)savedProjectRevision=projectRevision;}
                if(error!=null && automatic)autoSaveDue=Environment.TickCount64+5000;
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
        if(ProjectDirty && !request.Flag){Fail("Salva le modifiche prima di continuare, oppure scegli di scartarle.");return;}
        if(request.Epoch>0 && request.Epoch!=projectRevision){Fail("Il progetto è cambiato: riprova l’operazione.");return;}
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
                projectPath=path;projectError=null;ApplyProject(next! with {Name=System.IO.Path.GetFileNameWithoutExtension(path)[..Math.Min(100,System.IO.Path.GetFileNameWithoutExtension(path).Length)]});savedProjectRevision=projectRevision;completion.TrySetResult(new(true));
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
    { projectRevision++;autoSaveDue=Environment.TickCount64+1500; recovery.Schedule(project,projectPath); }
    private void SetSlot(int i, SlotSettings settings)
    { var slots = (SlotSettings[])project.Slots.Clone(); slots[i] = settings; var next = project with { Slots = slots }; Projects.Validate(next); project = next; Changed(); }
    private Reply Execute(Request r)
    {
        if(r.Op=="autosave-settings")
        {
            if(locked)return new(false,"SHOW LOCK attivo: modifiche bloccate.");
            if(r.Path!=null)autoSaveDirectory=ProjectDirectoryPolicy.DirectoryPath(r.Path);
            autoSaveEnabled=r.Flag;autoSaveDue=Environment.TickCount64+1500;return new(true);
        }
        if(r.Op=="new-project")
        {
            if(locked || maintenance || readingProject || savingProject)return new(false,"SHOW LOCK o operazione sui progetti in corso.");
            if(Busy() || loading.Any(v=>v) || seeking.Any(v=>v) || heldNotes.Count>0 || oscOutput.Pending>0 || (midiOut?.ActiveNotes??0)>0 || (midiOut?.PendingCommands??0)>0)return new(false,"Ferma le tracce e attendi le sequenze prima di creare un nuovo show.");
            if(ProjectDirty && !r.Flag)return new(false,"Salva le modifiche prima di continuare, oppure scegli di scartarle.");
            if(r.Epoch>0 && r.Epoch!=projectRevision)return new(false,"Il progetto è cambiato: riprova l’operazione.");
            if(mixer.CommandCapacityRemaining<Projects.MaxSlots*3+1)return new(false,"Motore occupato: riprova tra un istante.");
            var empty=new ProjectDocument{Name="Nuovo show",Routing=project.Routing,MemoryMiB=project.MemoryMiB,MasterControl=project.MasterControl,CurrentTrackControl=project.CurrentTrackControl,MarkerCapture=project.MarkerCapture};
            projectPath=null;projectError=null;ApplyProject(empty);savedProjectRevision=projectRevision;return new(true);
        }
        if(r.Op is "marker-context" or "state")
        {
            var reply=MarkerContext(r);return r.Op=="state"?new(true,State:Volatile.Read(ref snapshot)):reply;
        }
        if(r.Op is "learn-marker" or "clear-marker-learn")
        {
            if(r.Op=="learn-marker" && !r.Flag){markerLearn=false;return new(true);}
            if(locked || readingProject || maintenance || !MarkerSetupActive)return new(false,"Il tasto marker si configura solo in Setup, con SHOW LOCK disattivato.");
            if(r.Op=="clear-marker-learn"){markerLearn=false;project=project with{MarkerCapture=null};Changed();return new(true);}
            markerLearn=r.Flag;if(markerLearn){learn=-1;volumeLearn=null;}return new(true);
        }
        if (r.Op == "lock") { locked = r.Flag; if (locked){ learn = -1;volumeLearn=null;markerLearn=false;} return new(true); }
        if(r.Op=="wave")
        {
            int slot=r.Slot;if((uint)slot>=project.SlotCount)return new(false,"Slot non valido.");
            bool matches=(r.Origin==null || r.Origin==instance) && (!r.SourceRevision.HasValue || r.SourceRevision==generation[slot]);
            bool ready=matches && !loading[slot] && errors[slot]==null && sources[slot] is OwnedSource;
            return new(matches,Peaks:ready && sources[slot] is OwnedSource owned?owned.Peaks:[],WaveInstance:instance,WaveSlot:slot,WaveRevision:generation[slot],WaveReady:ready);
        }
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
                if(r.Flag){markerLearn=false;learn=-1;}
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
            if (value is < 128 or > 4096 || !budget.SetLimit(value * 1048576L)) return new(false, "Budget 128-4096 MiB richiesto, non inferiore alla RAM già riservata.");
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
            case "marker-add":case "marker-edit":case "marker-delete":
                if(locked || readingProject)return new(false,"SHOW LOCK: modifiche ai marker bloccate.");
                if(r.Origin!=instance || r.SourceRevision!=generation[i])return new(false,"La traccia è cambiata. Riapri la gestione marker.");
                if(loading[i] || sources[i]==null || errors[i]!=null)return new(false,"Attendi che la traccia sia pronta.");
                if(settings.MarkerLoop is {Enabled:true} loop && r.Op!="marker-add" && (r.MarkerId==loop.StartId || r.MarkerId==loop.EndId) && (r.Op=="marker-delete" || r.Marker?.Seconds!=settings.Markers.First(m=>m.Id==r.MarkerId).Seconds))return new(false,"Rimuovi il loop prima di spostare o eliminare i marker di inizio/fine.");
                var updated=AudioMarkers.Change(settings.Markers,r.Op,r.Marker,r.MarkerId,sources[i]!.Frames/(double)Mixer.Rate);
                var retainedLoop=settings.MarkerLoop;
                if(retainedLoop!=null && (!updated.Any(m=>m.Id==retainedLoop.StartId) || !updated.Any(m=>m.Id==retainedLoop.EndId)))retainedLoop=null;
                if(retainedLoop!=null){try{MarkerLoops.Validate(retainedLoop,updated);}catch(InvalidDataException){retainedLoop=null;}}
                var markerSettings=settings with{Markers=updated,MarkerLoop=retainedLoop};Projects.ValidateSlot(markerSettings);
                var markerReply=UpdateMarkerPlan(i,updated);if(!markerReply.Ok)return markerReply;
                SetSlot(i,markerSettings);return new(true);
            case "settings":
                if (r.Settings == null) return new(false, "Parametri mancanti.");
                if (Busy(i) || loading[i] || seeking[i]) return new(false, "Ferma lo slot prima di modificarlo.");
                var proposed=r.Settings with {File=settings.File,Volume=settings.Volume,Markers=settings.Markers,MarkerLoop=settings.MarkerLoop};
                if(proposed.Name!=settings.Name && !string.IsNullOrWhiteSpace(proposed.Name) && proposed.Name!=$"Slot {i+1}")proposed=proposed with{AutoName=false};
                proposed=SlotTitles.Normalize(proposed,i);
                SetSlot(i, proposed); return ConfigureVoice(i);
            case "learn": if(r.Flag){markerLearn=false;volumeLearn=null;}learn = r.Flag ? i : -1; return new(true);
            case "load":
                if (string.IsNullOrWhiteSpace(r.Path)) return new(false, "Seleziona un file.");
                if (loading[i] || seeking[i] || Busy(i)) return new(false, "Ferma lo slot e attendi il caricamento.");
                string path = System.IO.Path.GetFullPath(r.Path);
                SetSlot(i, SlotTitles.Normalize(settings,i) with { File = path,Markers=string.Equals(settings.File,path,StringComparison.OrdinalIgnoreCase)?settings.Markers:[],MarkerLoop=string.Equals(settings.File,path,StringComparison.OrdinalIgnoreCase)?settings.MarkerLoop:null });
                StartImport(i, path); return new(true);
            case "clear":
                if (loading[i] || seeking[i] || Busy(i)) return new(false, "Ferma lo slot e attendi il caricamento.");
                generation[i]++; var cleared = Install(i, null); if (cleared.Ok) SetSlot(i, settings with { File = null,Markers=[],MarkerLoop=null }); return cleared;
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
        if(learnConsumed.Contains(key))return new(true);
        if(markerLearn && MarkerSetupActive && !locked)
        {
            learnConsumed.Add(key);
            int conflict=Array.FindIndex(project.Slots,s=>s.PadChannel==r.Channel && s.PadNote==r.Note);
            if(conflict>=0)
            {
                if(conflict<project.SlotCount && project.Slots[conflict].EmergencyPauseAll){learnConsumed.Remove(key);heldNotes[key]=conflict;return Trigger(conflict);}
                return new(false,"Il tasto marker è già assegnato a uno slot. Scegli un tasto libero.");
            }
            project=project with{MarkerCapture=new(r.Channel,r.Note)};markerLearn=false;Changed();return new(true);
        }
        if(project.MarkerCapture is {} capture && capture.Channel==r.Channel && capture.Note==r.Note)
        {learnConsumed.Add(key);return CaptureMarker(r);}
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
        if(result.Ok && !s.EmergencyPauseAll && SlotActions.Audio(s.Action) && MarkerSetupActive){markerSlot=i;markerSourceRevision=generation[i];}
        lastStarted = i; triggerSerial++; return result;
    }
    private void StartLoop(Request r,TaskCompletionSource<Reply> completion)
    {
        void Fail(string error)=>completion.TrySetResult(new(false,error));
        int i=r.Slot;
        if((uint)i>=project.SlotCount){Fail("Slot non valido.");return;}
        if(locked || readingProject || maintenance){Fail("SHOW LOCK attivo: modifiche bloccate.");return;}
        if(r.Origin!=instance || r.SourceRevision!=generation[i]){Fail("La traccia è cambiata. Riapri la gestione marker.");return;}
        if(Busy(i) || loading[i] || seeking[i]){Fail("Ferma lo slot e attendi il caricamento prima di configurare il loop.");return;}
        var settings=project.Slots[i];var loop=r.MarkerLoop;
        MarkerLoops.Validate(loop,settings.Markers);
        if(loop is not {Enabled:true})
        {
            long next=++ticket;
            if(!mixer.Post(new(AudioOp.Loop,i,Ticket:next))){Fail("Coda audio piena.");return;}
            if(loopBuffers[i] is {} old)retiredBuffers.Add((next,old.Buffer));loopBuffers[i]=null;
            SetSlot(i,settings with{MarkerLoop=loop});completion.TrySetResult(new(true));return;
        }
        if(sources[i] is not OwnedSource source || errors[i]!=null){Fail("Attendi che la traccia sia pronta.");return;}
        var range=MarkerLoops.Frames(loop,settings.Markers,source.Frames/(double)Mixer.Rate);
        loading[i]=loopPreparing[i]=true;int stamp=generation[i],epoch=Volatile.Read(ref controlEpoch);
        _=Task.Run(async()=>
        {
            LoopRegion? buffer=null;string? error=null;bool entered=false;
            try{await importLimit.WaitAsync(stop.Token);entered=true;buffer=source.PrepareLoop(range.Start,range.End,stop.Token);}
            catch(Exception ex){error=ex.Message;Log.Error("loop-prepare",ex);}
            finally{if(entered)importLimit.Release();}
            if(!jobs.TryAdd(()=>
            {
                loading[i]=loopPreparing[i]=false;
                if(error!=null || stamp!=generation[i] || epoch!=Volatile.Read(ref controlEpoch) || locked || !ReferenceEquals(source,sources[i]))
                {if(buffer!=null)budget.Release(buffer.Buffer.ResidentBytes);Fail(error??"Preparazione loop annullata.");return;}
                long next=++ticket;
                if(!mixer.Post(new(AudioOp.Loop,i,Ticket:next,Loop:buffer)))
                {if(buffer!=null)budget.Release(buffer.Buffer.ResidentBytes);Fail("Coda audio piena.");return;}
                if(loopBuffers[i] is {} old)retiredBuffers.Add((next,old.Buffer));loopBuffers[i]=buffer;
                SetSlot(i,project.Slots[i] with{MarkerLoop=loop});completion.TrySetResult(new(true));
            },100))
            {if(buffer!=null)budget.Release(buffer.Buffer.ResidentBytes);failedCompletions.Enqueue((i,stamp));Fail("Coda comandi piena.");}
        });
    }
    private Reply BeginSeek(int i,float normalized)
    {
        if(readingProject)return new(false,"Lettura progetto in corso.");
        if(!float.IsFinite(normalized) || normalized is <0 or >1)return new(false,"Posizione non valida.");
        if(loading[i] || sources[i]==null || errors[i]!=null)return new(false,"Audio dello slot non pronto.");
        if(seeking[i])return new(false,"Spostamento già in preparazione.");
        var markerPlan=UpdateMarkerPlan(i,project.Slots[i].Markers,true);if(!markerPlan.Ok)return markerPlan;CancelMarkerOutputs(i);
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
                if(!mixer.Post(new(AudioOp.Seek,i,Ticket:next,Frame:frame,Seek:prepared,SignalEpoch:signalEpochs[i])))
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
        Projects.Validate(value); project = value; learn = -1;markerLearn=false;markerSetup=false; heldNotes.Clear(); learnConsumed.Clear();
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
        loading[i] = true; errors[i] = null; int stamp = ++generation[i];var markerSettings=project.Slots[i];
        _ = Task.Run(async () =>
        {
            IAudioSource? prepared = null;LoopRegion? preparedLoop=null; string? error = null; bool entered = false;
            try { await importLimit.WaitAsync(stop.Token); entered = true; prepared = Importer.Import(path, cache, budget, stop.Token);
                if(markerSettings.MarkerLoop is {Enabled:true} activeLoop && prepared is OwnedSource owned){var range=MarkerLoops.Frames(activeLoop,markerSettings.Markers,owned.Frames/(double)Mixer.Rate);preparedLoop=owned.PrepareLoop(range.Start,range.End,stop.Token);} }
            catch (Exception ex) { error = ex.Message; Log.Error("import", ex); }
            finally { if (entered) importLimit.Release(); }
            var result = prepared; var problem = error;
            if (!jobs.TryAdd(() => Complete(i, stamp, result, problem,preparedLoop)))
            { if(preparedLoop!=null)budget.Release(preparedLoop.Buffer.ResidentBytes);result?.Dispose(); failedCompletions.Enqueue((i, stamp)); }
        });
    }
    private Reply Posted(AudioCommand command)
    {
        bool signal=command.Op is AudioOp.Play or AudioOp.Trigger or AudioOp.Stop or AudioOp.Pause || command.Op==AudioOp.Release && project.Slots[command.Slot].Mode==PlayMode.Hold;
        int i=command.Slot,next=signal?signalEpochs[i]+1:0;
        if(!mixer.Post(signal?command with{SignalEpoch=next}:command))return new(false,"Coda audio piena. PANIC resta disponibile.");
        if(signal){signalEpochs[i]=next;if(command.Op is AudioOp.Stop or AudioOp.Pause or AudioOp.Release || command.Op==AudioOp.Trigger && project.Slots[i].Mode==PlayMode.TogglePause && mixer.View(i).State==VoiceState.Playing)CancelMarkerOutputs(i);}
        return new(true);
    }
    private Reply UpdateMarkerPlan(int i,AudioMarker[] markers,bool cancelSignals=false)
    {
        int version=markerVersions[i]+1,signal=signalEpochs[i]+(cancelSignals?1:0);
        if(!mixer.Post(new(AudioOp.Markers,i,Markers:markers,MarkerVersion:version,SignalEpoch:signal)))return new(false,"Coda audio piena.");
        markerVersions[i]=version;signalEpochs[i]=signal;return new(true);
    }
    private void CancelMarkerOutputs(int i)
    {
        oscOutput.CancelMarkers(i);outputLink.Post(new("cancel-markers",i));
    }
    private void DispatchMarkers()
    {
        long now=Environment.TickCount64;
        for(int n=0;n<128 && mixer.TryReadMarker(out var signal);n++)
        {
            int i=signal.Slot;
            if((uint)i>=project.SlotCount || signal.Epoch!=mixer.EventEpoch || signal.Version!=markerVersions[i] || signal.SignalEpoch!=signalEpochs[i] || now-signal.Tick>250 || loading[i] || seeking[i] || errors[i]!=null || sources[i]?.Error!=null || device.Error!=null)continue;
            int scope=MarkerTriggers.Scope(i,signal.Index);var marker=signal.Marker;
            if(marker.MidiTrigger is {} midi)outputLink.Post(new("send",scope,Settings:new(){MidiCommands=[midi]}));
            if(marker.OscTrigger is {} osc)oscOutput.Post([osc],scope);
        }
        if(mixer.MarkerDrops!=markerDrops){markerDrops=mixer.MarkerDrops;projectError="Coda trigger marker piena: alcuni comandi non inviati.";Log.Warn("marker-trigger",projectError);}
    }
    private Reply Install(int i, IAudioSource? source,LoopRegion? loop=null)
    {
        long next = ++ticket;
        if (!mixer.Post(new(AudioOp.Install, i, Source: source, Ticket: next,Loop:loop,Markers:(uint)i<project.Slots.Length?project.Slots[i].Markers:[],MarkerVersion:markerVersions[i]+1,SignalEpoch:signalEpochs[i]+1))) return new(false, "Coda audio piena.");
        markerVersions[i]++;signalEpochs[i]++;
        if (sources[i] is { } previous) retired.Add((next, previous));
        if(seekBuffers[i] is {} preview){retiredBuffers.Add((next,preview));seekBuffers[i]=null;}
        if(loopBuffers[i] is {} oldLoop)retiredBuffers.Add((next,oldLoop.Buffer));loopBuffers[i]=loop;
        sources[i] = source; errors[i] = null; return new(true);
    }
    private void Complete(int i, int stamp, IAudioSource? source, string? error,LoopRegion? loop=null)
    {
        if (generation[i] != stamp) { if(loop!=null)budget.Release(loop.Buffer.ResidentBytes);source?.Dispose(); return; }
        loading[i] = false;
        if (error != null) { errors[i] = error;if(loop!=null)budget.Release(loop.Buffer.ResidentBytes); source?.Dispose(); return; }
        var reply = Install(i, source,loop);
        if (!reply.Ok) { errors[i] = reply.Error;if(loop!=null)budget.Release(loop.Buffer.ResidentBytes); source?.Dispose(); }
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
                while (failedCompletions.TryDequeue(out var failed)) if (generation[failed.Index] == failed.Stamp) { loading[failed.Index] = loopPreparing[failed.Index]=false; errors[failed.Index] = "Coda piena alla fine del caricamento: riprova."; }
                for (int j = retired.Count - 1; j >= 0; j--) if (mixer.AppliedTicket >= retired[j].Ticket) { var old = retired[j].Source; retired.RemoveAt(j); _ = Task.Run(old.Dispose); }
                if(autoSaveEnabled && ProjectDirty && !savingProject && !readingProject && !maintenance && Environment.TickCount64>=autoSaveDue)
                    StartSave(new("save-project",Path:projectPath,Origin:"auto-save"),new TaskCompletionSource<Reply>(TaskCreationOptions.RunContinuationsAsynchronously));
                DispatchMarkers();
                if (Environment.TickCount64 < nextSnapshot) continue; nextSnapshot = Environment.TickCount64 + 100;
                if (mixer.CallbackFaults != previousFaults) { Log.Warn("audio-callback", $"Errori isolati: {mixer.CallbackFaults}"); previousFaults = mixer.CallbackFaults; }
                var slots = Enumerable.Range(0, project.SlotCount).Select(i => { var v = mixer.View(i); return new SlotInfo(i, SlotTitles.Display(project.Slots[i],i), v.State.ToString(), v.Frame / (double)Mixer.Rate, v.Frames / (double)Mixer.Rate, v.Volume, v.Starvations, errors[i] ?? sources[i]?.Error, loading[i], generation[i],seeking[i],v.LoopPass,loopPreparing[i]); }).ToArray();
                Volatile.Write(ref snapshot, new(instance, device.Status, device.Error, device.DeviceId, device.Latency, mixer.Master, budget.Used, mixer.CallbackFaults,
                    Log.Unavailable, device.Devices, slots, project, locked, learn, projectError ?? recovery.Error, outputLink.Alert, Volatile.Read(ref midiIn), Volatile.Read(ref midiOut),
                    Environment.TickCount64, mixer.RenderHeartbeat, device.Heartbeat, lastStarted, triggerSerial, projectPath, readingProject, projectRevision,oscOutput.Error,oscOutput.Sent,volumeLearn,CurrentPlaying(),markerLearn && MarkerSetupActive,MarkerSetupActive,ProjectDirty,autoSaveEnabled,savingProject,lastSavedAt));
            }
        }
        catch (Exception ex) { Log.Error("control-thread", ex); stop.Cancel(); }
    }
    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        stop.Cancel();control.Join(1000);
        if(autoSaveEnabled && ProjectDirty)
        {
            var final=project;string target=projectPath??pendingSavePath??UntitledPath(autoSaveDirectory);var active=saveWorker;
            var flush=Task.Run(async()=>
            {
                try{if(active!=null)await active;ProjectFiles.Write(target,final with{Name=System.IO.Path.GetFileNameWithoutExtension(target)},true);recovery.Schedule(final,target);}
                catch(Exception ex){Log.Error("autosave-exit",ex);}
            });
            flush.Wait(1500);
        }
        recovery.Dispose();oscOutput.Dispose();inputLink.Dispose();outputLink.Dispose();device.Dispose(); }
}
