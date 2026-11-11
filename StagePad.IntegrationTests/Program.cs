using StagePad.App;
using StagePad.Core;
using NAudio.Midi;

if(args.Contains("--lifecycle-child")){Thread.Sleep(Timeout.Infinite);return;}
if(args.Length==2 && args[0]=="--lifecycle-parent")
{
    var childStart=new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};
    if(Path.GetFileNameWithoutExtension(Environment.ProcessPath)=="dotnet")childStart.ArgumentList.Add(typeof(StagePad.App.Reply).Assembly.Location);
    childStart.ArgumentList.Add("--lifecycle-child");using var descendant=System.Diagnostics.Process.Start(childStart)!;
    File.WriteAllText(args[1],descendant.Id.ToString());Thread.Sleep(Timeout.Infinite);return;
}

string folder = Path.Combine(Path.GetTempPath(), "stagepad-integration-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder); Environment.SetEnvironmentVariable("STAGEPAD_DATA_DIR", folder);
Log.Start("integration");
int checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
string pipe = "stagepad-tests-" + Guid.NewGuid().ToString("N");
using var input = new MidiInputModule(pipe); using var output = new MidiOutputModule(pipe);
input.Start(); output.Start();

using var audio = new AudioCoordinator(pipe);
Wire.TestRoute = (name, request) => name.EndsWith(".in", StringComparison.Ordinal) ? input.Handle(request) : name.EndsWith(".out", StringComparison.Ordinal) ? output.Handle(request) : audio.Handle(request);
async Task<StateInfo> State() => (await audio.Handle(new("state"))).State!;
async Task<StateInfo> Until(Func<StateInfo, bool> predicate, string name, int timeout = 7000)
{
    long limit = Environment.TickCount64 + timeout;
    while (Environment.TickCount64 < limit) { var s = await State(); if (s != null && predicate(s)) return s; await Task.Delay(15); }
    throw new Exception("TIMEOUT: " + name + " · " + System.Text.Json.JsonSerializer.Serialize(await State()));
}
async Task Do(Request r) { var reply = await audio.Handle(r); Check(reply.Ok, "accepted " + r.Op + (reply.Ok ? "" : ": " + reply.Error)); }
async Task Configure(int slot, Func<SlotSettings, SlotSettings> change)
{ var s = await State(); await Do(new("settings", slot, Settings: change(s.Project!.Slots[slot]))); }
void Emit(int note, bool down) => FakeMidi.Emit(MidiCodec.Note(1,note,down ? 100 : 0,down));
try
{
    await Until(s => s.Project != null, "startup");
    Check((await State()).Master == 1, "coordinator master 100% at startup");
    Check(new DeviceInfo("opaque-endpoint-id","Altoparlanti di prova").ToString()=="Altoparlanti di prova","device selector fallback uses friendly name");
    await Do(new("grid",Rows:2,Columns:8));
    await Until(s=>s.Project!.Grid==new GridLayout(2,8),"grid update");
    Check(!(await audio.Handle(new("grid",Rows:3,Columns:5))).Ok && (await State()).Project!.Grid==new GridLayout(2,8),"invalid grid preserves current layout");
    await Do(new("configure", Routing: new() { MidiInput = "1:2:Fake MIDI", MidiOutput = "1:2:Fake MIDI" }));
    await Until(s => s.MidiIn?.Status == "Connesso" && s.MidiOut?.Status.StartsWith("Porta aperta") == true, "MIDI open");
    string file = Path.Combine(folder, "audio.wav"); await File.WriteAllTextAsync(file, "OK");
    await Do(new("load", Path: file)); await Until(s => !s.Slots[0].Loading && s.Slots[0].State == "Ready", "load");
    var wire = await Wire.Call(pipe, new("state")); Check(wire.State?.Project?.Slots.Length == 16, "serialized protocol project snapshot roundtrip (in-process transport)");
    Check((await Wire.Call(pipe, new("wave"))).Peaks?.Length == 5, "waveform serialized separately");
    await Do(new("learn", Flag:true)); Emit(36,true);
    await Until(s => s.Project!.Slots[0].PadNote == 36, "learn"); Emit(36,false);
    Check((await State()).Slots[0].State == "Ready", "learn does not trigger audio or lights");
    Emit(36,true); await Until(s => s.Slots[0].State == "Playing" && s.Slots[0].Position > .05, "MIDI play"); Emit(36,false);
    double before = (await State()).Slots[0].Position;
    FakeMidi.InputPresent = false; await Until(s => s.MidiIn?.Error != null, "input unplug");
    Check((await State()).Slots[0].Position > before && (await State()).Slots[0].State == "Playing", "controller unplug preserves playback");
    FakeMidi.InputPresent = true; await Until(s => s.MidiIn?.Status == "Connesso", "input reconnect");
    await Do(new("stop")); await Until(s => s.Slots[0].State == "Stopped", "stop");
    await Configure(0,s => s with { Mode = PlayMode.Hold }); Emit(36,true); await Until(s => s.Slots[0].State == "Playing", "hold down");
    Emit(36,false); await Until(s => s.Slots[0].State == "Stopped", "hold release"); Check(true, "MIDI hold release end-to-end");
    await Configure(0,s => s with { Mode = PlayMode.Protected }); Emit(36,true); Emit(36,false);
    await Until(s => s.Slots[0].Position > .05, "protected start"); double protectedAt = (await State()).Slots[0].Position;
    Emit(36,true); Emit(36,false); await Task.Delay(180);
    Check((await State()).Slots[0].Position > protectedAt, "protected MIDI retrigger does not restart or stop audio");
    Check(!(await audio.Handle(new("pause"))).Ok, "protected rejects pause");
    await Do(new("stop")); await Until(s => s.Slots[0].State == "Stopped", "protected stop");
    await Configure(0,s => s with { Mode = PlayMode.OneShot, Action = SlotAction.Both });
    int noteCount = FakeMidi.Notes.Count; await Do(new("pad-down")); await Do(new("pad-up"));
    await Until(s => s.Slots[0].State == "Playing", "both playing"); await Task.Delay(220);
    Check(FakeMidi.Notes.Count >= noteCount + 2, "Note ON and timed OFF while audio plays");
    FakeMidi.OutputError = true; await Do(new("pad-down"));
    await Until(s => s.MidiOut?.Error != null, "output error");
    Check((await State()).Slots[0].State == "Playing", "MIDI output exception never stops audio");
    FakeMidi.OutputError = false;
    await Do(new("stop")); await Until(s => s.Slots[0].State == "Stopped", "stop before MSC");
    await Configure(0,s => s with { Action = SlotAction.Midi, Midi = s.Midi with { Kind = MidiKind.MSC } });
    await Until(s => s.MidiOut?.Status.StartsWith("Porta aperta") == true, "output recovered");
    await Do(new("pad-down")); await Task.Delay(120);
    Check(FakeMidi.SysEx.TryPeek(out var msc) && msc[0] == 0xF0 && msc[^1] == 0xF7 && (await State()).Slots[0].State == "Stopped", "MSC-only sends SysEx without starting audio");
    await Configure(1,s => s with { Action = SlotAction.Both });
    int count = FakeMidi.Notes.Count; Check(!(await audio.Handle(new("pad-down",1))).Ok, "missing audio is reported"); await Task.Delay(200);
    Check(FakeMidi.Notes.Count > count, "missing audio does not block light command");
    await Configure(0,s=>s with { Action=SlotAction.Audio,Mode=PlayMode.TogglePause });
    await Do(new("play"));await Until(s=>s.Slots[0].State=="Playing","play before seek");
    await Do(new("grid",Rows:8,Columns:2,Origin:"setup"));
    await Until(s=>s.Project!.SetupGrid==new GridLayout(8,2),"setup grid snapshot update");
    Check((await State()).Project!.SetupGrid==new GridLayout(8,2) && (await State()).Project!.Grid==new GridLayout(2,8),"setup grid command leaves show layout unchanged");
    await Do(new("lock",Flag:true));
    Check(!(await audio.Handle(new("grid",Rows:4,Columns:4,Origin:"setup"))).Ok,"SHOW LOCK protects setup layout");
    Check(!(await audio.Handle(new("grid",Rows:4,Columns:4))).Ok,"SHOW LOCK protects custom layout");
    int seekNotes=FakeMidi.Notes.Count,seekSysEx=FakeMidi.SysEx.Count;
    await Do(new("seek",Value:.5f));
    await Until(s=>!s.Slots[0].Seeking && s.Slots[0].Position>=20 && s.Slots[0].Position<21,"seek while locked");
    Check(FakeMidi.Notes.Count==seekNotes && FakeMidi.SysEx.Count==seekSysEx,"manual seek under SHOW LOCK sends no light commands");
    await Do(new("pause"));await Until(s=>s.Slots[0].State=="Paused","pause before seek");
    await Do(new("seek",Value:.25f));
    await Until(s=>!s.Slots[0].Seeking && s.Slots[0].State=="Paused" && s.Slots[0].Position==10,"paused seek coordinator");
    Check(true,"coordinator seek preserves paused state and exact cursor");
    Check(!(await audio.Handle(new("learn",Flag:true))).Ok && !(await audio.Handle(new("clear"))).Ok && !(await audio.Handle(new("configure",Routing:new()))).Ok, "show lock enforced in engine for learn, clear and routing");
    await Do(new("master",Value:.8f)); await Do(new("panic")); await Do(new("lock",Flag:false));
    string save = Path.Combine(folder, "show.json"); await Do(new("save-project",Path:save));
    await Until(s => s.ProjectPath == save, "project save");
    var saved = ProjectFiles.Read(save); Check(saved.Slots[0].File == file && saved.Slots[0].Midi.Kind == MidiKind.MSC && saved.Grid==new GridLayout(2,8) && saved.SetupGrid==new GridLayout(8,2), "atomic project save keeps routing, grid and file paths");
    long revision = (await State()).ProjectRevision;
    await Do(new("load-project",Path:save)); await Until(s => s.ProjectRevision > revision && !s.ProjectLoading && !s.Slots.Any(x => x.Loading), "project reload");
    Check((await State()).Slots.All(s => s.State != "Playing"), "project restore never autoplays");
    string bad = Path.Combine(folder,"bad.json"); await File.WriteAllTextAsync(bad,"{");
    Check(!(await audio.Handle(new("load-project",Path:bad))).Ok,"corrupt project open reports actual failure"); await Until(s => s.ProjectError != null, "bad project error"); Check((await State()).Project?.Slots[0].File == file, "corrupt JSON preserves current project");
    string broken = Path.Combine(folder,"bad.wav"); await File.WriteAllTextAsync(broken,"CORRUPT"); await Do(new("load",2,Path:broken));
    await Until(s => s.Slots[2].Error != null && !s.Slots[2].Loading,"bad audio");
    await Configure(0,s => s with { Action=SlotAction.Audio }); await Do(new("pad-down")); await Until(s => s.Slots[0].State == "Playing", "play before MIDI block");
    await Configure(1,s => s with { Action=SlotAction.Midi, Midi=s.Midi with { Kind=MidiKind.Note } });
    FakeMidi.OutGate.Reset(); await Do(new("pad-down",1)); double atBlock=(await State()).Slots[0].Position;
    await Task.Delay(300); Check((await State()).Slots[0].Position > atBlock, "blocked MIDI native call cannot stall audio coordinator or render");
    await Do(new("panic")); await Until(s => s.Slots.All(x => x.State != "Playing"),"panic while MIDI blocked");
    Check(true,"PANIC audio works while MIDI output is blocked"); FakeMidi.OutGate.Set();
    await Task.Delay(400);
    Check(File.Exists(ProjectFiles.Recovery),"automatic last-project recovery written");
    // Shared-note gates: releasing one slot must not release another still-held slot.
    await Configure(1,s => s with { Action=SlotAction.Midi, Midi=s.Midi with { Kind=MidiKind.Note, Behavior=NoteBehavior.Gate, Note=70 } });
    await Configure(2,s => s with { Action=SlotAction.Midi, Midi=s.Midi with { Kind=MidiKind.Note, Behavior=NoteBehavior.Gate, Note=70 } });
    int OffCount() => FakeMidi.Notes.Count(x => (x & 0xF0) == 0x80 && ((x >> 8) & 127) == 70);
    int offBefore = OffCount();
    await Do(new("pad-down",1)); await Do(new("pad-down",2)); await Task.Delay(100);
    await Do(new("pad-up",1)); await Task.Delay(100);
    Check(OffCount() == offBefore, "shared gate does not release another held slot");
    await Do(new("pad-up",2)); await Task.Delay(100);
    Check(OffCount() == offBefore+1, "last shared gate sends one Note Off");
    await Do(new("pad-down",0)); await Until(s => s.Slots[0].State == "Playing", "before output busy");
    var route = (await State()).Project!.Routing;
    await Do(new("configure", Routing:route with { MidiOutput=null }));
    await Until(s => s.MidiOut?.Status == "Disabilitato", "disable output");
    FakeMidi.OutputBusy = true;
    await Do(new("configure", Routing:route));
    await Until(s => s.MidiOut?.Error?.Contains("occupato") == true, "output busy");
    Check((await State()).Slots[0].State == "Playing", "busy MIDI port and routing changes preserve live audio");
    FakeMidi.OutputBusy = false; await Until(s => s.MidiOut?.Status.StartsWith("Porta aperta") == true,"busy port recovered");
    await Do(new("panic"));
    Check(!(await audio.Handle(new("pad-down",0,Timestamp:1))).Ok, "stale pre-PANIC trigger rejected by coordinator");
    Check(!(await audio.Handle(new("seek",Value:.5f,Timestamp:1))).Ok,"stale pre-PANIC seek rejected by coordinator");
    Check(!(await audio.Handle(new("seek",Value:float.NaN))).Ok,"invalid seek position rejected");
    await Until(s => s.Slots[0].State == "Stopped", "panic settles");
    await Do(new("load",0,Path:broken)); await Until(s => s.Slots[0].Error != null && !s.Slots[0].Loading,"replacement corrupt");
    Check(!(await audio.Handle(new("pad-down",0))).Ok, "failed replacement never plays old cached audio under a new file mapping");
    string pcm=Path.Combine(folder,"seek.pcm");
    float[] pcmSamples=Enumerable.Range(0,48000*2*26).Select(i=>(i%997)/997f).ToArray();
    using(var stream=File.Create(pcm))stream.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(pcmSamples.AsSpan()));
    var realDisk=new DiskSource(pcm,pcmSamples.Length/2);
    var seekBudget=new MemoryBudget(realDisk.ResidentBytes);seekBudget.Reserve(realDisk.ResidentBytes);
    using(var owned=new OwnedSource(realDisk,pcm,seekBudget,[]))
    {
        bool noBudget=false;try{owned.PrepareSeek(48000*20,CancellationToken.None);}catch(InvalidOperationException){noBudget=true;}
        Check(noBudget && seekBudget.Used==realDisk.ResidentBytes && owned.Error==null,"seek budget failure preserves source and reservation");
        seekBudget.SetLimit(realDisk.ResidentBytes+48000*8);
        var prepared=owned.PrepareSeek(48000*20,CancellationToken.None)!;var block=new float[2048];
        Check(prepared.ReadAt(48000*20,block)==1024 && block.AsSpan().SequenceEqual(pcmSamples.AsSpan(48000*20*2,2048)),"streaming seek preview contains exact destination PCM");
        Check(seekBudget.Used==realDisk.ResidentBytes+prepared.ResidentBytes,"seek preview charged against shared memory budget");
        owned.Hint(48000*20);await Task.Delay(100);
        Check(owned.ReadAt(48000*21,block)==1024 && block.AsSpan().SequenceEqual(pcmSamples.AsSpan(48000*21*2,2048)),"read-ahead catches destination beyond prepared second");
        seekBudget.Release(prepared.ResidentBytes);
    }
    Check(seekBudget.Used==0,"source and seek preview reservations fully released");

    await Do(new("panic"));await Until(s=>s.Slots.All(x=>x.State is not ("Playing" or "Paused" or "Stopping")),"idle before 100 slots");
    await Do(new("slot-count",Value:100));
    await Until(s=>s.Slots.Length==100,"100-slot snapshot");
    Check((await State()).Project!.Grid==new GridLayout(10,10) && (await State()).Project!.SetupGrid==new GridLayout(10,10),"both grids expand to 10x10 for 100 slots");
    Check(!(await audio.Handle(new("grid",Rows:11,Columns:10))).Ok,"coordinator rejects grid above 10x10");
    Check(!(await audio.Handle(new("slot-count",Value:101))).Ok && !(await audio.Handle(new("slot-count",Value:0))).Ok,"coordinator enforces 1-100 slot count");
    await Configure(99,s=>s with { Name="Finale slot 100",Action=SlotAction.Audio,Mode=PlayMode.TogglePause,PadNote=98 });
    await Do(new("load",99,Path:file));await Until(s=>!s.Slots[99].Loading && s.Slots[99].State=="Ready","load slot 100");
    await Do(new("pad-down",99));await Until(s=>s.Slots[99].State=="Playing" && s.Slots[99].Position>.02,"play slot 100");
    Check(!(await audio.Handle(new("slot-count",Value:16))).Ok,"slot count cannot change during playback");
    await Do(new("seek",99,Value:.25f));await Until(s=>!s.Slots[99].Seeking && s.Slots[99].Position>9,"seek slot 100");
    await Do(new("panic"));await Until(s=>s.Slots[99].State=="Stopped","panic slot 100");
    await Do(new("lock",Flag:true));Check(!(await audio.Handle(new("slot-count",Value:16))).Ok,"SHOW LOCK protects slot count");await Do(new("lock",Flag:false));
    string hundredSave=Path.Combine(folder,"hundred.json");await Do(new("save-project",Path:hundredSave));
    await Until(s=>s.ProjectPath==hundredSave,"save 100 slots");
    Check(ProjectFiles.Read(hundredSave).Slots[99].Name=="Finale slot 100","slot 100 persists on disk");
    var hundredWire=await Wire.Call(pipe,new("state"));Check(hundredWire.State!.Slots.Length==100,"IPC serializes all 100 slot states");
    await Do(new("slot-count",Value:16));await Until(s=>s.Slots.Length==16,"hide tail slots");
    Check(!(await audio.Handle(new("play",99))).Ok && (await State()).Project!.Slots[99].File==file,"hidden slots cannot play and retain audio path");
    await Do(new("slot-count",Value:100));await Until(s=>s.Slots.Length==100 && !s.Slots[99].Loading && s.Slots[99].State=="Ready","reactivate slot 100");
    Check((await State()).Project!.Slots[99].Name=="Finale slot 100","reactivating hidden slot restores its configuration");
    await Do(new("panic"));

    string invalidSave=Path.Combine(folder,"cannot-save.json");Directory.CreateDirectory(invalidSave);
    string? previousSavePath=(await State()).ProjectPath;
    var failedSave=await audio.Handle(new("save-project",Path:invalidSave));
    Check(!failedSave.Ok && failedSave.Error!=null,"project save returns failure when disk destination cannot be written");
    await Until(s=>s.ProjectError!=null,"save failure snapshot");
    Check((await State()).ProjectPath==previousSavePath,"failed save preserves previous project association");
    string completeSave=Path.Combine(folder,"completed-save.json");
    var completed=await audio.Handle(new("save-project",Path:completeSave));
    Check(completed.Ok && File.Exists(completeSave) && ProjectFiles.Read(completeSave).SlotCount==100,"successful save reply means file is already committed to disk");
    await Until(s=>s.ProjectPath==completeSave,"saved project association");
    Check(ProjectFiles.Read(completeSave).Name=="completed-save" && (await State()).Project!.Name=="completed-save","saved show name persists in document and live state");
    await Until(s=>{ProjectFiles.ReadStartup(out var remembered,out _,folder);return remembered==completeSave;},"last show association persisted by recovery writer");
    var restartShow=ProjectFiles.ReadStartup(out var restartPath,out _,folder);
    Check(restartPath==completeSave && restartShow.Name=="completed-save" && restartShow.SlotCount==100,"startup restores last show identity and configuration");
    using(var savedJson=System.Text.Json.JsonDocument.Parse(File.ReadAllText(completeSave)))
        Check(!savedJson.RootElement.TryGetProperty("lastProjectPath",out _),"portable manual projects do not contain local session paths");
    string duplicatePath=Path.Combine(folder,"show-copy.json"),renamedPath=Path.Combine(folder,"show-renamed.json");
    await Do(new("duplicate-project",Path:completeSave,TargetPath:duplicatePath));
    Check(File.Exists(completeSave) && File.ReadAllBytes(completeSave).SequenceEqual(File.ReadAllBytes(duplicatePath)),"duplicate preserves project and audio references");
    Check(!(await audio.Handle(new("duplicate-project",Path:completeSave,TargetPath:duplicatePath))).Ok,"duplicate refuses to overwrite an existing project");
    await Do(new("rename-project",Path:completeSave,TargetPath:renamedPath));
    await Until(s=>s.ProjectPath==renamedPath,"rename current association");
    Check(!File.Exists(completeSave) && File.Exists(renamedPath) && (await State()).Project!.Name=="show-renamed","rename updates current show without reloading audio");
    Check(!(await audio.Handle(new("rename-project",Path:renamedPath,TargetPath:duplicatePath))).Ok && File.Exists(renamedPath),"rename refuses to overwrite another project");
    await Do(new("lock",Flag:true));
    Check(!(await audio.Handle(new("delete-project",Path:renamedPath))).Ok && File.Exists(renamedPath),"show lock blocks project deletion");
    await Do(new("lock",Flag:false));
    Check(!(await audio.Handle(new("delete-project",Path:ProjectFiles.Recovery))).Ok,"manual project management protects automatic recovery");
    Check(!(await audio.Handle(new("rename-project",Path:renamedPath,TargetPath:Path.Combine(folder,"outside","x.json")))).Ok,"rename cannot move a project and break relative audio paths");
    await Do(new("load-project",Path:duplicatePath));
    await Until(s=>s.ProjectPath==duplicatePath && s.Project!.Name=="show-copy","opening a copy takes show name from selected filename");
    await Do(new("delete-project",Path:duplicatePath));
    await Until(s=>s.ProjectPath==null,"deleted current project detaches save path");
    Check(!File.Exists(duplicatePath) && File.Exists(file) && (await State()).Project!.SlotCount==100,"delete preserves audio files and loaded slot configuration");
    string legacyRoot=Path.Combine(folder,"legacy-case");
    string legacyFile=Path.Combine(legacyRoot,"recovery","last-project.json");
    ProjectFiles.Write(legacyFile,new ProjectDocument { Name="Legacy project",Master=.3f },false);
    var legacyRestored=ProjectFiles.ReadRecovery(out var legacyWarning,legacyRoot);
    Check(legacyRestored.Name=="Legacy project" && legacyWarning==null,"legacy recovery folder remains readable after project-folder rename");
    string modernFile=Path.Combine(legacyRoot,"project","last-project.json");
    ProjectFiles.Write(modernFile,legacyRestored with { Name="New project" },false);
    Check(ProjectFiles.ReadRecovery(out _,legacyRoot).Name=="New project" && File.Exists(legacyFile),"new project folder takes precedence without removing legacy data");
    await File.WriteAllTextAsync(modernFile,"{");
    Check(ProjectFiles.ReadRecovery(out var recoveryWarning,legacyRoot).Name=="Legacy project" && recoveryWarning!=null,"corrupt new recovery falls back safely to legacy copy");
    Check(Path.GetFileName(ProjectFiles.DefaultDirectory)=="project","default project directory uses project rather than recovery");
    string sessionRoot=Path.Combine(folder,"startup-case"),sessionFile=Path.Combine(sessionRoot,"project","last-project.json");
    ProjectFiles.Write(sessionFile,new ProjectDocument {Name="Last show",Master=.42f},false,true,renamedPath);
    var startupProject=ProjectFiles.ReadStartup(out var rememberedPath,out var startupWarning,sessionRoot);
    Check(rememberedPath==renamedPath && startupProject.Name=="Last show" && startupProject.Master==.42f && startupWarning==null,"startup keeps recovered edits and last show path together");
    ProjectFiles.Write(sessionFile,startupProject,false,true,Path.Combine(folder,"missing-show.json"));
    var missingStartup=ProjectFiles.ReadStartup(out var missingPath,out var missingWarning,sessionRoot);
    Check(missingPath==null && missingStartup.Name=="Last show" && missingWarning!=null,"missing original show falls back to recovered configuration with a warning");
    ProjectFiles.Write(sessionFile,startupProject,false,true,null);
    Check(ProjectFiles.ReadStartup(out var detachedPath,out _,sessionRoot).Name=="Last show" && detachedPath==null,"deleted current show stays detached after restart");
    await File.WriteAllTextAsync(sessionFile,"{");
    Check(ProjectFiles.ReadStartup(out _,out var backupWarning,sessionRoot).Name=="Last show" && backupWarning!=null,"corrupt latest session falls back to backup");
    Check(ProjectFiles.ReadStartup(out var legacyStartupPath,out _,legacyRoot).Name=="Legacy project" && legacyStartupPath==null,"startup remains compatible with legacy recovery without session metadata");

    using(var oscReceiver=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))
    {
        int oscPort=((System.Net.IPEndPoint)oscReceiver.Client.LocalEndPoint!).Port;
        var firstOsc=new OscCommand {Port=oscPort,Address="/show/go",Arguments=[new(OscArgumentType.Int,"12")]};
        var secondOsc=firstOsc with{Address="/show/name",Arguments=[new(OscArgumentType.String,"Finale")]};
        var midiBatch=new MidiSettings[]{new(){Note=90,Behavior=NoteBehavior.Gate},new(){Note=91,Behavior=NoteBehavior.Gate},new(){Kind=MidiKind.ControlChange,Controller=12,ControlValue=64},new(){Kind=MidiKind.ProgramChange,Program=5},new(){Kind=MidiKind.MSC,Cue="7.000"}};
        await Until(s=>s.Slots.All(v=>!v.Loading && !v.Seeking),"idle imports before combined-output tests");
        await Do(new("configure",Routing:(await State()).Project!.Routing with{MidiInput="1:2:Fake MIDI",MidiOutput="1:2:Fake MIDI"}));
        await Until(s=>s.MidiOut?.Status.StartsWith("Porta aperta")==true,"MIDI ready for combinations");
        await Do(new("load",3,Path:file));await Until(s=>!s.Slots[3].Loading && s.Slots[3].State=="Ready","combined audio ready");
        long observedOsc=(await State()).OscSent;
        foreach(var combination in Enum.GetValues<SlotAction>())
        {
            long panicAt=Environment.TickCount64;await Do(new("panic"));
            // A non-audio combination is already idle: wait for the callback to
            // acknowledge reset before starting the next independent scenario.
            await Until(s=>s.RenderHeartbeat>panicAt+20 && s.Slots.All(v=>v.State is not ("Playing" or "Paused" or "Stopping")),"panic acknowledged between combinations");
            await Configure(3,s=>s with{Action=combination,Mode=PlayMode.OneShot,PadChannel=16,PadNote=100,MidiCommands=midiBatch,OscCommands=[firstOsc,secondOsc]});
            while(FakeMidi.Notes.TryDequeue(out _)){}while(FakeMidi.SysEx.TryDequeue(out _)){}
            long beforeOsc=(await State()).OscSent;await Task.Delay(2);
            await Do(new("midi-down",Channel:16,Note:100,Origin:"combined-controller",Timestamp:Environment.TickCount64));
            if(SlotActions.Osc(combination))
            {
                using var receiveTimeout=new CancellationTokenSource(2500);
                var packet1=await oscReceiver.ReceiveAsync(receiveTimeout.Token);var packet2=await oscReceiver.ReceiveAsync(receiveTimeout.Token);
                Check(packet1.Buffer.SequenceEqual(OscCodec.Encode(firstOsc)) && packet2.Buffer.SequenceEqual(OscCodec.Encode(secondOsc)),"two real UDP OSC packets from MIDI pad: "+combination);
                observedOsc+=2;await Until(s=>s.OscSent==observedOsc,"OSC diagnostics count");
            }
            if(SlotActions.Midi(combination))
            {
                await Until(s=>FakeMidi.SysEx.Count==1 && FakeMidi.Notes.Count(x=>(x&0xF0)==0x90)==2,"all MIDI batch messages arrive");
                Check(FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,12,64)) && FakeMidi.Notes.Contains(MidiCodec.ProgramChange(1,5)),"CC and Program Change in multi-command batch: "+combination);
            }
            if(SlotActions.Audio(combination))await Until(s=>s.Slots[3].State=="Playing","audio snapshot updated for "+combination);
            await Task.Delay(120);
            var combinationState=await State();
            Check((combinationState.Slots[3].State=="Playing")==SlotActions.Audio(combination),"audio only when selected: "+combination);
            if(!SlotActions.Midi(combination))Check(!FakeMidi.Notes.Any(x=>(x&0xF0)==0x90) && FakeMidi.SysEx.IsEmpty,"MIDI disabled produces no output: "+combination);
            if(!SlotActions.Osc(combination))Check(oscReceiver.Available==0 && combinationState.OscSent==beforeOsc,"OSC disabled produces no packets: "+combination);
            await Do(new("midi-up",Channel:16,Note:100,Origin:"combined-controller"));
            if(SlotActions.Midi(combination))
            {
                await Until(s=>new[]{90,91}.All(note=>FakeMidi.Notes.Any(x=>(x&0xF0)==0x80 && ((x>>8)&127)==note)),"release all batch gates");
                Check(true,"pad release sends both gate Note Offs: "+combination);
            }
        }
        await Do(new("panic"));await Until(s=>s.Slots[3].State=="Stopped","idle before blocked MIDI OSC test");
        FakeMidi.OutGate.Reset();await Do(new("pad-down",3));
        using(var timeout=new CancellationTokenSource(2500)){await oscReceiver.ReceiveAsync(timeout.Token);await oscReceiver.ReceiveAsync(timeout.Token);}observedOsc+=2;
        await Until(s=>s.Slots[3].State=="Playing","audio while MIDI blocked and OSC active");
        Check(true,"blocked MIDI driver does not block audio or either OSC message");FakeMidi.OutGate.Set();await Do(new("pad-up",3));
        await Do(new("panic"));await Until(s=>s.Slots[3].State=="Stopped","idle before missing audio combination");
        await Do(new("clear",3));await Configure(3,s=>s with{Action=SlotAction.All});
        Check(!(await audio.Handle(new("pad-down",3))).Ok,"missing audio remains an explicit error with all outputs enabled");
        using(var timeout=new CancellationTokenSource(2500)){await oscReceiver.ReceiveAsync(timeout.Token);await oscReceiver.ReceiveAsync(timeout.Token);}observedOsc+=2;
        Check(true,"missing audio does not block multiple OSC commands");await Do(new("pad-up",3));
        string combinedFile=Path.Combine(folder,"combined-outputs.json");await Do(new("save-project",Path:combinedFile));
        var persisted=ProjectFiles.Read(combinedFile).Slots[3];
        Check(persisted.Action==SlotAction.All && persisted.MidiCommands!.Length==5 && persisted.OscCommands.Length==2,"all output selections and command lists persist to disk");
        await Do(new("panic"));
        await Until(s=>s.OscSent==observedOsc,"all received OSC transmissions reflected in snapshot");
        long sentBeforeReopen=(await State()).OscSent;await Do(new("load-project",Path:combinedFile));
        await Until(s=>s.ProjectPath==combinedFile && s.Slots.All(v=>!v.Loading),"reopen multi-output project");
        await Task.Delay(120);
        Check(oscReceiver.Available==0 && (await State()).OscSent==sentBeforeReopen,"project reopen never transmits configured OSC commands automatically");
        var resolveStarted=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseResolve=new TaskCompletionSource<System.Net.IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using(var oscWorker=new OscOutput(async(_,ct)=>{resolveStarted.SetResult();return await releaseResolve.Task.WaitAsync(ct);}))
        {
            oscWorker.Post([firstOsc with{Host="hold.example"}]);await resolveStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            oscWorker.Panic();releaseResolve.SetResult([System.Net.IPAddress.Loopback]);await Task.Delay(100);
            Check(oscReceiver.Available==0 && oscWorker.Sent==0,"PANIC cancels OSC batch waiting on DNS");
            oscWorker.Post([firstOsc]);using var timeout=new CancellationTokenSource(2500);var afterPanic=await oscReceiver.ReceiveAsync(timeout.Token);
            Check(afterPanic.Buffer.SequenceEqual(OscCodec.Encode(firstOsc)),"fresh OSC command works after PANIC without replaying the cancelled batch");
            using(var hostnameWorker=new OscOutput())
            {
                hostnameWorker.Post([firstOsc with{Host="localhost"}]);using var dnsTimeout=new CancellationTokenSource(2500);var viaHost=await oscReceiver.ReceiveAsync(dnsTimeout.Token);
                Check(viaHost.Buffer.SequenceEqual(OscCodec.Encode(firstOsc)),"OSC hostname resolves to IPv4 receiver when localhost has both address families");
            }
        }
    }

    await Do(new("panic"));await Until(s=>s.Slots.All(v=>v.State is not ("Playing" or "Paused" or "Stopping")) && s.Slots.All(v=>!v.Loading),"idle for automatic titles and faders");
    string titleA=Path.Combine(folder,"track-one.wav"),titleB=Path.Combine(folder,"track-two.wav");
    await File.WriteAllTextAsync(titleA,"OK");await File.WriteAllTextAsync(titleB,"OK");
    await Configure(6,s=>s with{Name="Slot 7",AutoName=true,Action=SlotAction.Audio});
    await Do(new("load",6,Path:titleA));await Until(s=>!s.Slots[6].Loading && s.Slots[6].Name=="track-one","automatic first track title");
    await Do(new("load",6,Path:titleB));await Until(s=>!s.Slots[6].Loading && s.Slots[6].Name=="track-two","automatic replacement track title");
    Check((await State()).Project!.Slots[6].AutoName==true,"automatic title keeps explicit mode after replacing audio");
    await Configure(6,s=>s with{Name="Slot Drums",AutoName=false});await Do(new("load",6,Path:titleA));
    await Until(s=>!s.Slots[6].Loading && s.Slots[6].Name=="Slot Drums","custom Slot-prefixed title retained");
    await Configure(6,s=>s with{Name="",AutoName=true});await Until(s=>s.Slots[6].Name=="track-one","clearing title restores dynamic track name");
    await Do(new("clear",6));await Until(s=>s.Slots[6].Name=="Slot 7","empty automatic slot has default title");
    Check(true,"dynamic, custom and cleared slot titles behave correctly");
    await Do(new("learn-volume",Origin:"master",Flag:true));
    float beforeLearn=(await State()).Master;FakeMidi.Emit(MidiCodec.ControlChange(2,7,0));
    await Until(s=>s.Project!.MasterControl==new MidiCcBinding(2,7) && s.VolumeLearn==null,"master fader learned");
    Check((await State()).Master==beforeLearn,"learn consumes first CC without a master volume jump");
    await Do(new("learn-volume",Origin:"track",Flag:true));FakeMidi.Emit(MidiCodec.ControlChange(3,11,64));
    await Until(s=>s.Project!.CurrentTrackControl==new MidiCcBinding(3,11) && s.VolumeLearn==null,"current-track fader learned independently");
    Check(!(await audio.Handle(new("midi-volume-controls",MasterControl:new(2,7),TrackControl:new(2,7)))).Ok,"same controller cannot be assigned to both volumes");
    await Do(new("load",4,Path:file));await Do(new("load",5,Path:file));await Until(s=>!s.Slots[4].Loading && !s.Slots[5].Loading,"two audio tracks prepared for faders");
    await Configure(4,s=>s with{Action=SlotAction.Audio,Mode=PlayMode.OneShot});await Configure(5,s=>s with{Action=SlotAction.Audio,Mode=PlayMode.OneShot});
    await Do(new("play",4));await Until(s=>s.CurrentPlaying==4,"first current audio");
    FakeMidi.Emit(MidiCodec.ControlChange(2,7,32));await Until(s=>Math.Abs(s.Master-32/127f)<.0001,"master CC applied");
    Check((await State()).Project!.Slots[4].Volume==1,"master CC leaves track volume independent");
    FakeMidi.Emit(MidiCodec.ControlChange(3,11,64));await Until(s=>Math.Abs(s.Project!.Slots[4].Volume-64/127f)<.0001,"track CC applies to currently playing audio");
    Check(Math.Abs((await State()).Master-32/127f)<.0001,"track CC leaves master independent");
    await Do(new("play",5));await Until(s=>s.CurrentPlaying==5,"newest playing audio selected");
    FakeMidi.Emit(MidiCodec.ControlChange(3,11,20));await Until(s=>Math.Abs(s.Project!.Slots[5].Volume-20/127f)<.0001,"fader follows newest playing track");
    Check(Math.Abs((await State()).Project!.Slots[4].Volume-64/127f)<.0001,"other playing track volume preserved");
    await Configure(3,s=>s with{Action=SlotAction.Midi});await Do(new("pad-down",3));await Do(new("pad-up",3));await Until(s=>s.LastStarted==3,"MIDI-only slot triggered");
    FakeMidi.Emit(MidiCodec.ControlChange(3,11,1));await Until(s=>Math.Abs(s.Project!.Slots[5].Volume-1/127f)<.0001,"MIDI-only activation does not steal current audio fader");
    await Do(new("pause",5));await Until(s=>s.Slots[5].State=="Paused" && s.CurrentPlaying==4,"paused newest track falls back to other playing audio");
    FakeMidi.Emit(MidiCodec.ControlChange(3,11,0));await Until(s=>s.Project!.Slots[4].Volume==0,"absolute CC zero mutes current track");
    await Do(new("stop",4));await Until(s=>s.Slots[4].State=="Stopped" && s.CurrentPlaying==-1,"no current track while all audio stopped or paused");
    FakeMidi.Emit(MidiCodec.ControlChange(3,11,100));await Task.Delay(120);
    Check((await State()).Project!.Slots[4].Volume==0 && Math.Abs((await State()).Project!.Slots[5].Volume-1/127f)<.0001,"current-track CC with no playing audio changes no slot");
    await Do(new("lock",Flag:true));FakeMidi.Emit(MidiCodec.ControlChange(2,7,127));await Until(s=>s.Master==1,"master fader stays usable under SHOW LOCK");
    Check(!(await audio.Handle(new("learn-volume",Origin:"track",Flag:true))).Ok,"SHOW LOCK protects controller assignment");await Do(new("lock",Flag:false));
    await Do(new("panic"));await Until(s=>s.Slots[5].State=="Stopped","all idle before controller persistence");
    string faderProject=Path.Combine(folder,"volume-controls.json");await Do(new("save-project",Path:faderProject));
    var faderCopy=ProjectFiles.Read(faderProject);
    Check(faderCopy.MasterControl==new MidiCcBinding(2,7) && faderCopy.CurrentTrackControl==new MidiCcBinding(3,11) && faderCopy.Slots[6].AutoName==true,"independent fader assignments and auto titles persist in project");
    await Do(new("midi-volume-controls",MasterControl:new(),TrackControl:new()));
    FakeMidi.Emit(MidiCodec.ControlChange(2,7,0));await Task.Delay(120);Check((await State()).Master==1,"disabled CC mapping no longer changes volume");

    // Emergency pauses audio while sending its independent output commands.
    using(var emergencyReceiver=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))
    {
        int emergencyPort=((System.Net.IPEndPoint)emergencyReceiver.Client.LocalEndPoint!).Port;
        var emergencyOsc=new OscCommand{Host="127.0.0.1",Port=emergencyPort,Address="/emergency"};
        await Configure(7,s=>s with{EmergencyPauseAll=true,Action=SlotAction.All,MidiCommands=[new(){Kind=MidiKind.ControlChange,Controller=98,ControlValue=45}],OscCommands=[emergencyOsc]});
        await Configure(4,s=>s with{Mode=PlayMode.Hold});
        await Do(new("play",4));await Do(new("play",5));await Until(s=>s.Slots[4].State=="Playing" && s.Slots[5].State=="Playing","audio before emergency");
        await Do(new("pad-down",7));
        await Until(s=>s.Slots[4].State=="Paused" && s.Slots[5].State=="Paused","all voices emergency paused");
        var pausedEmergency=await State();await Do(new("pad-up",4));await Task.Delay(120);
        Check((await State()).Slots[4].State=="Paused" && (await State()).Slots[4].Position==pausedEmergency.Slots[4].Position,"emergency retains Hold cursor after physical release");
        using var emergencyTimeout=new CancellationTokenSource(2500);var emergencyPacket=await emergencyReceiver.ReceiveAsync(emergencyTimeout.Token);
        Check(emergencyPacket.Buffer.SequenceEqual(OscCodec.Encode(emergencyOsc)),"EMERGENCY slot transmits its own OSC command while pausing all audio");
        await Until(s=>s.MidiOut?.LastMessage=="CC 98 · 45","emergency MIDI command");
        Check(true,"EMERGENCY slot transmits MIDI independently of missing local audio");
        await Do(new("pad-up",7));await Do(new("panic"));await Until(s=>s.Slots[4].State=="Stopped","emergency cleanup");
    }
    // Gate and Hold are released if the input disappears; one-shot playback is preserved.
    await Configure(4,s=>s with{Mode=PlayMode.Hold,PadChannel=1,PadNote=95});Emit(95,true);
    await Until(s=>s.Slots[4].State=="Playing","Hold before USB disconnect");
    FakeMidi.InputPresent=false;await Until(s=>s.MidiIn?.Error!=null && s.Slots[4].State=="Stopped","USB loss releases held audio");
    Check(true,"input USB disconnect releases held audio without a stuck Hold");
    FakeMidi.InputPresent=true;await Until(s=>s.MidiIn?.Status=="Connesso","input reconnected after held release");
    Check((await State()).Slots[4].State=="Stopped","USB reconnect never replays held trigger");
    // Portable packages carry original bytes, relative media and Emergency settings.
    var bundleProject=(await State()).Project!;
    string bundleZip=Path.Combine(folder,"roundtrip.stagepad.zip"),bundleDir=Path.Combine(folder,"imported");
    ShowBundle.Export(bundleZip,bundleProject);string bundleJson=ShowBundle.Import(bundleZip,bundleDir,"Restored");
    var bundleCopy=ProjectFiles.Read(bundleJson);
    Check(bundleCopy.Slots[7].EmergencyPauseAll && bundleCopy.Slots[4].File!=null && File.ReadAllBytes(bundleCopy.Slots[4].File!).SequenceEqual(File.ReadAllBytes(file)),"portable show roundtrip preserves media bytes and Emergency slot");
    Check(!File.ReadAllText(bundleJson).Contains(folder.Replace("\\","/")),"portable show stores relative audio paths");
    bool duplicateBundle=false;try{ShowBundle.Import(bundleZip,bundleDir,"Restored");}catch(IOException){duplicateBundle=true;}Check(duplicateBundle,"portable import never overwrites an existing show");
    string unsafeZip=Path.Combine(folder,"unsafe.zip");
    using(var zip=System.IO.Compression.ZipFile.Open(unsafeZip,System.IO.Compression.ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("../outside.txt").Open());writer.Write("bad");}
    bool unsafeBundle=false;try{ShowBundle.Import(unsafeZip,bundleDir,"Unsafe");}catch(InvalidDataException){unsafeBundle=true;}
    Check(unsafeBundle && !File.Exists(Path.Combine(folder,"outside.txt")) && !Directory.GetDirectories(bundleDir,".import-*").Any(),"portable import rejects traversal and removes staging files");
    using(var delayedReceiver=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))
    using(var delayedOutput=new OscOutput())
    {
        int portValue=((System.Net.IPEndPoint)delayedReceiver.Client.LocalEndPoint!).Port;
        var delayedCommand=new OscCommand{Host="127.0.0.1",Port=portValue,Address="/delayed",DelayMs=300};
        delayedOutput.Post([delayedCommand],1);delayedOutput.Post([delayedCommand with{Address="/immediate",DelayMs=0}],2);
        using var immediateTimeout=new CancellationTokenSource(2000);var immediatePacket=await delayedReceiver.ReceiveAsync(immediateTimeout.Token);
        Check(immediatePacket.Buffer.SequenceEqual(OscCodec.Encode(delayedCommand with{Address="/immediate",DelayMs=0})),"OSC delay in one slot does not block another slot");
        delayedOutput.Cancel(1);await Task.Delay(400);Check(delayedReceiver.Available==0,"cancel slot prevents pending OSC from being transmitted");
        var testResult=await delayedOutput.Test(delayedCommand);using var testTimeout=new CancellationTokenSource(2000);await delayedReceiver.ReceiveAsync(testTimeout.Token);
        Check(testResult.Ok,"OSC command test reports actual local UDP transmission and skips saved delay");
    }

    while(FakeMidi.Notes.TryDequeue(out _)){}
    await Configure(8,s=>s with{Action=SlotAction.Midi,MidiCommands=[new(){Kind=MidiKind.ControlChange,Controller=97,ControlValue=1},new(){Kind=MidiKind.ControlChange,Controller=97,ControlValue=2,DelayMs=300}]});
    await Do(new("pad-down",8));await Until(s=>FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,97,1)),"first ordered MIDI command");
    Check(!FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,97,2)),"MIDI command respects saved delay");
    await Until(s=>FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,97,2)),"delayed MIDI command");
    Check(FakeMidi.Notes.ToArray().SequenceEqual(new[]{MidiCodec.ControlChange(1,97,1),MidiCodec.ControlChange(1,97,2)}),"MIDI sequence preserves exact configured order");
    await Do(new("pad-up",8));while(FakeMidi.Notes.TryDequeue(out _)){}
    await Do(new("pad-down",8));await Until(s=>FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,97,1)),"MIDI before cancel");await Do(new("stop",8));await Task.Delay(400);
    Check(!FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,97,2)),"STOP cancels delayed MIDI commands");
    var midiTestReply=await output.Handle(new("test-send",Settings:new(){Action=SlotAction.Midi,MidiCommands=[new(){Kind=MidiKind.ControlChange,Controller=96,ControlValue=42,DelayMs=2000}]})).WaitAsync(TimeSpan.FromSeconds(3));
    Check(midiTestReply.Ok && FakeMidi.Notes.Contains(MidiCodec.ControlChange(1,96,42)),"MIDI test acknowledges actual send after earlier PANIC epochs and skips saved delay");
    // Updater never replaces on a bad digest, and a committed replacement is reversible.
    string updateTarget=Path.Combine(folder,"test-target.exe"),updateCandidate=Path.Combine(folder,"test-candidate.exe");
    File.WriteAllText(updateTarget,"previous");File.WriteAllText(updateCandidate,"new");
    bool badDigest=false;try{UpdateFiles.Replace(updateTarget,updateCandidate,new string('0',64));}catch(InvalidDataException){badDigest=true;}
    Check(badDigest && File.ReadAllText(updateTarget)=="previous","invalid update checksum leaves installed executable intact");
    string updateBackup=UpdateFiles.Replace(updateTarget,updateCandidate,UpdateFiles.Hash(updateCandidate));
    Check(File.ReadAllText(updateTarget)=="new" && File.ReadAllText(updateBackup)=="previous","atomic update stores previous executable");
    UpdateFiles.Restore(updateTarget,updateBackup);Check(File.ReadAllText(updateTarget)=="previous","rollback restores previous executable bytes");
    using(var updateHttp=new System.Net.Http.HttpClient(new UpdateHttpFixture()))
    {
        var updateService=new AppUpdates(updateHttp,Path.Combine(folder,"updates"));var available=await updateService.Check(new Version(0,12,0));
        Check(available?.Version==new Version(0,13,0) && available.Sha256==new string('a',64),"stable GitHub release resolves allowed executable and checksum");
        Check(await updateService.Check(new Version(0,13,0))==null,"updater never offers same version");
    }
    var children=new List<System.Diagnostics.Process>();
    System.Diagnostics.Process? externalChild=null;
    try
    {
        for(int n=0;n<2;n++)
        {
            var start=new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};
            if(Path.GetFileNameWithoutExtension(Environment.ProcessPath)=="dotnet")start.ArgumentList.Add(typeof(StagePad.App.Reply).Assembly.Location);
            start.ArgumentList.Add("--lifecycle-child");children.Add(System.Diagnostics.Process.Start(start)!);
        }
        string childMarker=Path.Combine(folder,"external-child.pid");
        var parentStart=new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};
        if(Path.GetFileNameWithoutExtension(Environment.ProcessPath)=="dotnet")parentStart.ArgumentList.Add(typeof(StagePad.App.Reply).Assembly.Location);
        parentStart.ArgumentList.Add("--lifecycle-parent");parentStart.ArgumentList.Add(childMarker);children.Add(System.Diagnostics.Process.Start(parentStart)!);
        long markerDeadline=Environment.TickCount64+3000;
        while(!File.Exists(childMarker) && Environment.TickCount64<markerDeadline)await Task.Delay(20);
        externalChild=System.Diagnostics.Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(childMarker)));
        var cleanupTimer=System.Diagnostics.Stopwatch.StartNew();ChildProcessCleanup.StopAll(children,80);
        Check(children.All(p=>p.HasExited) && cleanupTimer.ElapsedMilliseconds<3000,"bounded shutdown terminates real unresponsive child processes");
        Check(!externalChild.HasExited,"cleanup does not terminate external descendants of an application process");
    }
    finally{foreach(var child in children){try{if(!child.HasExited)child.Kill();}catch{}child.Dispose();}if(externalChild!=null){try{if(!externalChild.HasExited)externalChild.Kill();}catch{}externalChild.Dispose();}}
    Console.WriteLine($"{checks} integration checks passed with simulated hardware. Native IPC, Windows drivers and WPF are NOT covered.");
}
finally
{
    FakeMidi.OutGate.Set(); await audio.Handle(new("shutdown")); await input.Handle(new("shutdown")); await output.Handle(new("shutdown"));
    Wire.TestRoute = null;
}

sealed class UpdateHttpFixture : System.Net.Http.HttpMessageHandler
{
    protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var content=System.Text.Json.JsonSerializer.Serialize(new{draft=false,prerelease=false,tag_name="v0.13.0",assets=new[]{new{name="StagePad.exe",browser_download_url="https://github.com/Alessio278/StagePad-Live/releases/download/v0.13.0/StagePad.exe",digest="sha256:"+new string('a',64)}}});
        return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent(content)});
    }
}
