using StagePad.Core;
using System.Runtime.InteropServices;

int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
var mixer = new Mixer();
float[] output = new float[2048];
float[] audio = Enumerable.Range(0, 48000 * 2).Select(i => (float)Math.Sin(i / 30.0) * .1f).ToArray();
Check(mixer.Post(new(AudioOp.Install, 0, Source: new MemorySource(audio), Ticket: 1)), "install accepted");
mixer.Render(output); Check(mixer.AppliedTicket == 1 && mixer.View(0).State == VoiceState.Ready, "installation acknowledged");
mixer.Post(new(AudioOp.Play)); mixer.Render(output);
Check(mixer.View(0).Frame == 1024 && output.Any(x => x != 0), "play advances in frames");
mixer.Post(new(AudioOp.Pause)); mixer.Render(output);
Check(mixer.View(0).Frame == 1024 && output.All(x => x == 0), "pause preserves exact cursor");
mixer.Post(new(AudioOp.Play)); mixer.Render(output);
Check(mixer.View(0).Frame == 2048, "resume continues exact cursor");
mixer.Post(new(AudioOp.Stop)); mixer.Render(output); mixer.Render(output);
Check(mixer.View(0).Frame == 0 && output.All(x => x == 0), "stop resets cursor");
for (int i = 0; i < 16; i++) { mixer.Post(new(AudioOp.Install, i, Source: new MemorySource(audio))); mixer.Post(new(AudioOp.Play, i)); }
mixer.Render(output);
Check(Enumerable.Range(0,16).All(i => mixer.View(i).State == VoiceState.Playing), "16 simultaneous voices");
Check(output.All(x => float.IsFinite(x) && Math.Abs(x) <= 1), "finite output and clipping guard");
int filled = 0; while (mixer.Post(new(AudioOp.Play))) filled++;
Check(filled > 0 && filled <= 512, "bounded control queue");
mixer.Panic(); for (int i = 0; i < 12; i++) mixer.Render(output);
Check(Enumerable.Range(0,16).All(i => mixer.View(i).State != VoiceState.Playing) && output.All(x => x == 0), "panic survives saturated queue and cancels delayed starts");
mixer.Freeze(true); mixer.Post(new(AudioOp.Play)); mixer.Render(output);
Check(mixer.View(0).Frame == 0 && output.All(x => x == 0), "device loss freezes cursor");
mixer.Freeze(false); mixer.Render(output); Check(mixer.View(0).Frame == 1024, "device recovery resumes cursor");
var bad = new Mixer(); bad.Post(new(AudioOp.Install, Source: new ThrowingSource())); bad.Post(new(AudioOp.Play));
bad.Post(new(AudioOp.Install, 1, Source: new MemorySource(audio))); bad.Post(new(AudioOp.Play, 1)); bad.Render(output);
Check(bad.View(0).State == VoiceState.Error && bad.View(1).Frame == 1024 && output.Any(x => x != 0), "one source exception preserves other voices");
var starving = new Mixer(); starving.Post(new(AudioOp.Install, Source: new EmptySource())); starving.Post(new(AudioOp.Play)); starving.Render(output);
Check(starving.View(0).Starvations == 1 && starving.View(0).Frame == 0, "starvation is visible without false cursor advance");
starving.Post(new(AudioOp.Stop)); starving.Render(output);
Check(starving.View(0).State == VoiceState.Stopped, "stop remains effective when the source has no buffered frames");
// Allocation test excludes first-call JIT and setup. Render must not allocate.
var steady = new Mixer(); steady.Post(new(AudioOp.Install, Source: new MemorySource(new float[48000 * 2 * 30]))); steady.Post(new(AudioOp.Play));
for (int i = 0; i < 10; i++) steady.Render(output);
long before = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 1000; i++) steady.Render(output);
Check(GC.GetAllocatedBytesForCurrentThread() == before, "zero managed allocations in 1000 steady render blocks");
string file = Path.Combine(Path.GetTempPath(), "stagepad-test-" + Guid.NewGuid() + ".pcm");
try
{
    float[] original = Enumerable.Range(0, 48000 * 2 * 26).Select(i => (float)(i % 997) / 997).ToArray();
    using (var stream = File.Create(file)) stream.Write(MemoryMarshal.AsBytes(original.AsSpan()));
    using var disk = new DiskSource(file, original.Length / 2);
    int count = disk.ReadAt(0, output);
    Check(count == 1024 && output.AsSpan().SequenceEqual(original.AsSpan(0, output.Length)), "disk source preloaded head");
    count = disk.ReadAt(48000 * 5L - 500, output);
    Check(count == 1024 && output.AsSpan().SequenceEqual(original.AsSpan((48000 * 5 - 500) * 2, output.Length)), "head to read-ahead boundary");
    count = disk.ReadAt(48000 * 20L, output);
    var limit = DateTime.UtcNow.AddSeconds(3);
    while (count != 1024 && DateTime.UtcNow < limit) { Thread.Sleep(5); count = disk.ReadAt(48000 * 20L, output); }
    Check(count == 1024 && output.AsSpan().SequenceEqual(original.AsSpan(48000 * 20 * 2, output.Length)), "read-ahead refill after discontinuity");
    count = disk.ReadAt(0, output); Check(count == 1024 && output.AsSpan().SequenceEqual(original.AsSpan(0, output.Length)), "immediate restart from resident head");
    Check(disk.ResidentBytes == DiskSource.EstimateBytes(disk.Frames, 48000), "streaming memory estimate matches reservation");
}
finally { File.Delete(file); }
// Real concurrent producer/consumer stress for queue publication ordering.
var queue = new SpscQueue<AudioCommand>(64); int received = 0; Exception? problem = null;
var producer = new Thread(() => { for (int i = 0; i < 200000; i++) { while (!queue.TryWrite(new(AudioOp.Volume, Slot: i))) Thread.Yield(); } });
var consumer = new Thread(() => { try { for (int i = 0; i < 200000; i++) { AudioCommand c; while (!queue.TryRead(out c)) Thread.Yield(); if (c.Slot != i) throw new Exception("Queue ordering"); received++; } } catch (Exception ex) { problem = ex; } });
producer.Start(); consumer.Start(); Check(producer.Join(10000) && consumer.Join(10000) && problem == null && received == 200000, "SPSC concurrent ordering over 200000 commands");
Check(new Mixer().Master == 1, "master defaults to 100 percent");
var modes = new Mixer(); modes.Post(new(AudioOp.Install, Source: new MemorySource(audio)));
modes.Post(new(AudioOp.Configure, Mode: PlayMode.Protected)); modes.Post(new(AudioOp.Trigger)); modes.Render(output);
modes.Post(new(AudioOp.Trigger)); modes.Post(new(AudioOp.Release)); modes.Render(output);
Check(modes.View(0).Frame == 2048 && modes.View(0).State == VoiceState.Playing, "protected ignores repeated pad and release");
modes.Post(new(AudioOp.Stop)); modes.Render(output); modes.Render(output);
Check(modes.View(0).State == VoiceState.Stopped, "mouse stop still stops protected mode");
modes.Post(new(AudioOp.Configure, Mode: PlayMode.Hold)); modes.Post(new(AudioOp.Trigger)); modes.Render(output);
modes.Post(new(AudioOp.Release)); modes.Render(output); modes.Render(output);
Check(modes.View(0).State == VoiceState.Stopped && modes.View(0).Frame == 0, "hold releases through fade and resets cursor");
modes.Post(new(AudioOp.Configure, Mode: PlayMode.TogglePause)); modes.Post(new(AudioOp.Trigger)); modes.Render(output);
long pausedAt = modes.View(0).Frame; modes.Post(new(AudioOp.Trigger)); modes.Render(output);
Check(modes.View(0).State == VoiceState.Paused && modes.View(0).Frame == pausedAt, "pad toggle pauses at exact sample");
modes.Post(new(AudioOp.Trigger)); modes.Render(output);
Check(modes.View(0).Frame == pausedAt + 1024, "pad toggle resumes exact sample");
Check(MidiCodec.TryNote(0x003C90, out int channel, out int note, out bool down) && channel == 1 && note == 60 && !down, "MIDI note on velocity zero means release");
Check(MidiCodec.Note(16,127,127,true) == 0x7F7F9F, "MIDI channel and data encoding");
var msc = new MidiSettings { Kind = MidiKind.MSC, DeviceId = 127, CommandFormat = 127, Cue = "37.2", Address = MscAddress.ExecDotPage, Executor = 5 };
Check(Convert.ToHexString(MidiCodec.Msc(msc)) == "F07F7F027F0133372E32303000352E31F7", "dot2 manual GO vector cue 37.2 executor 5 page 1");
Check(Convert.ToHexString(MidiCodec.Msc(msc with { Command = MscCommand.Resume, Address = MscAddress.Default })) == "F07F7F027F03F7", "dot2 manual default RESUME vector");
Check(Projects.NormalizeCue("21.5") == "21.500", "MSC invariant cue decimals");
var project = new ProjectDocument(); Projects.Validate(project);
string json = System.Text.Json.JsonSerializer.Serialize(project, Projects.Json);
Projects.Validate(System.Text.Json.JsonSerializer.Deserialize<ProjectDocument>(json, Projects.Json)!);
Check(project.Master == 1 && project.Slots.Length == 16, "project JSON roundtrip and defaults");
project.Slots[0] = project.Slots[0] with { PadNote = 36 }; project.Slots[1] = project.Slots[1] with { PadNote = 36 };
bool rejected = false; try { Projects.Validate(project); } catch (InvalidDataException) { rejected = true; }
Check(rejected, "duplicate pad assignments rejected");
string waveformFile = Path.Combine(Path.GetTempPath(), "stagepad-wave-" + Guid.NewGuid() + ".pcm");
try
{
    float[] wave = [.1f,-.2f, .3f,-.8f, 2f,-.1f, 0f,0f];
    using (var stream = File.Create(waveformFile)) stream.Write(MemoryMarshal.AsBytes(wave.AsSpan()));
    Check(WavePeaks.Build(waveformFile,4).SequenceEqual(new float[] {.2f,.8f,1f,0f}), "offline waveform aggregates stereo peaks and clamps display");
}
finally { File.Delete(waveformFile); }
var fades = new Mixer(); var shortBlock = new float[960];
fades.Post(new(AudioOp.Install,Source:new MemorySource(Enumerable.Repeat(1f,48000*2*3).ToArray())));
fades.Post(new(AudioOp.Volume,Value:.25f)); fades.Post(new(AudioOp.Configure,FadeInMs:1000,FadeOutMs:1000)); fades.Post(new(AudioOp.Trigger));
for(int i=0;i<25;i++) fades.Render(shortBlock);
Check(Math.Abs(shortBlock[^1]-.0625f)<.001f, "fade-in duration is independent of slot volume");
fades.Post(new(AudioOp.Stop)); for(int i=0;i<25;i++) fades.Render(shortBlock);
Check(Math.Abs(shortBlock[^1]-.046875f)<.001f, "early stop fades from current envelope over configured duration");
for(int i=0;i<76;i++) fades.Render(shortBlock);
Check(fades.View(0).State == VoiceState.Stopped, "fade-out completes and stops");
var delayed=StagePad.App.UiPlaybackClock.Read(9.75,30,true,false,1000,1300);
Check(Math.Abs(delayed.Position-10.05)<.000001 && delayed.Elapsed==10 && delayed.Remaining==20, "display compensates age of engine timestamp, not GUI receipt time");
var edgeBefore=StagePad.App.UiPlaybackClock.Read(.99,30,false,false,1000,1000);
var edgeAfter=StagePad.App.UiPlaybackClock.Read(1.01,30,false,false,1000,1000);
Check(edgeBefore.Elapsed==0 && edgeBefore.Remaining==30 && edgeAfter.Elapsed==1 && edgeAfter.Remaining==29, "elapsed and remaining change together at one second boundary");
bool complementary=true;
for(int i=0;i<=1000;i++)
{
 var r=StagePad.App.UiPlaybackClock.Read(i*30.4/1000,30.4,false,i==1000,1000,1000);
 complementary &= r.Elapsed+r.Remaining==r.Duration;
}
Check(complementary,"fractional track duration never desynchronizes elapsed and remaining");
var pausedDisplay=StagePad.App.UiPlaybackClock.Read(5.5,30,false,false,1000,10000);
Check(pausedDisplay.Position==5.5,"paused display never extrapolates");
var staleDisplay=StagePad.App.UiPlaybackClock.Read(5,30,true,false,1000,9000);
Check(staleDisplay.Position==5.5,"stale display prediction is bounded to 500 milliseconds");
var endedDisplay=StagePad.App.UiPlaybackClock.Read(30.4,30.4,false,true,1000,1000);
Check(endedDisplay.Elapsed==31 && endedDisplay.Remaining==0 && endedDisplay.Duration==31,"track end sets remaining to zero including fractional final second");
var restartedDisplay=StagePad.App.UiPlaybackClock.Read(.02,30,true,false,2000,2020);
Check(Math.Abs(restartedDisplay.Position-.04)<.000001,"retrigger uses new engine position without previous monotonic clamp");
var predictedEnd=StagePad.App.UiPlaybackClock.Read(30.3,30.4,true,false,1000,1200);
Check(predictedEnd.Remaining==0 && predictedEnd.Elapsed==predictedEnd.Duration,"last countdown reaches zero without waiting for a later Ended snapshot");
Check(StagePad.App.UiPlaybackClock.Format(3723)=="1:02:03","display clock formats hours consistently");
var seekMixer=new Mixer();
seekMixer.Post(new(AudioOp.Install,Source:new ConstantSource()));seekMixer.Post(new(AudioOp.Play));seekMixer.Render(output);
seekMixer.Post(new(AudioOp.Seek,Frame:96000));seekMixer.Render(output);
Check(seekMixer.View(0).Frame==97024 && seekMixer.View(0).State==VoiceState.Playing,"forward seek preserves playback");
seekMixer.Post(new(AudioOp.Seek,Frame:24000));seekMixer.Render(output);
Check(seekMixer.View(0).Frame==25024,"backward seek changes cursor at callback boundary");
seekMixer.Post(new(AudioOp.Pause));seekMixer.Post(new(AudioOp.Seek,Frame:48000));seekMixer.Render(output);
Check(seekMixer.View(0).Frame==48000 && seekMixer.View(0).State==VoiceState.Paused && output.All(x=>x==0),"paused seek stays paused at exact destination");
seekMixer.Post(new(AudioOp.Play));seekMixer.Render(output);
Check(seekMixer.View(0).Frame==49024,"resume after paused seek starts at destination");
seekMixer.Post(new(AudioOp.Stop));seekMixer.Render(output);seekMixer.Render(output);
seekMixer.Post(new(AudioOp.Seek,Frame:144000));seekMixer.Render(output);
Check(seekMixer.View(0).State==VoiceState.Stopped && seekMixer.View(0).Frame==144000,"stopped seek cues silently");
seekMixer.Post(new(AudioOp.Trigger));seekMixer.Render(output);
Check(seekMixer.View(0).Frame==145024,"pad starts from manually cued position");
seekMixer.Post(new(AudioOp.Configure,Mode:PlayMode.Protected));seekMixer.Post(new(AudioOp.Seek,Frame:192000));seekMixer.Render(output);
Check(seekMixer.View(0).Frame==193024 && seekMixer.View(0).State==VoiceState.Playing,"protected mode permits deliberate mouse seek");
var bridge=new SeekBuffer(2000,Enumerable.Repeat(.3f,600).ToArray());
seekMixer.Post(new(AudioOp.Seek,Frame:2000,Seek:bridge,Ticket:50));seekMixer.Render(output);
Check(Math.Abs(output[598]-.3f)<.0001 && Math.Abs(output[600]-.02f)<.0001 && seekMixer.View(0).Frame==3024,"seek preview joins normal source in one callback");
seekMixer.Post(new(AudioOp.Seek,Frame:7000,Ticket:51));seekMixer.Panic();seekMixer.Render(output);
Check(seekMixer.AppliedTicket==51 && seekMixer.View(0).Frame==0 && seekMixer.View(0).State==VoiceState.Stopped,"PANIC cancels stale seek while acknowledging buffer retirement");
seekMixer.Post(new(AudioOp.Play));seekMixer.Render(output);
before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)seekMixer.Render(output);
Check(GC.GetAllocatedBytesForCurrentThread()==before,"seek-capable render remains allocation-free");
var gridProject=new ProjectDocument { Grid=new(2,8),SetupGrid=new(8,2) };Projects.Validate(gridProject);
var gridCopy=System.Text.Json.JsonSerializer.Deserialize<ProjectDocument>(System.Text.Json.JsonSerializer.Serialize(gridProject,Projects.Json),Projects.Json)!;
Check(gridCopy.SetupGrid==new GridLayout(8,2),"setup layout persists independently of live layout");
Check(gridCopy.Grid==new GridLayout(2,8) && gridCopy.Slots.Length==16,"custom grid persists with all 16 slots");
bool invalidGrid=false;try{Projects.Validate(gridProject with { Grid=new(3,5) });}catch(InvalidDataException){invalidGrid=true;}
Check(invalidGrid,"grid cannot hide slots by providing fewer than 16 cells");
invalidGrid=false;try{Projects.Validate(gridProject with { SetupGrid=new(3,5) });}catch(InvalidDataException){invalidGrid=true;}
Check(invalidGrid,"setup grid cannot hide slots");
Check(System.Text.Json.JsonSerializer.Deserialize<ProjectDocument>("{}",Projects.Json)!.SetupGrid==new GridLayout(),"legacy setup defaults to 4x4");
Check(System.Text.Json.JsonSerializer.Deserialize<ProjectDocument>("{}",Projects.Json)!.Grid==new GridLayout(),"legacy project without grid defaults to 4x4");
Check(StagePad.App.SlotPalette.Colors.Count==20 && StagePad.App.SlotPalette.Colors.Select(c=>c.Hex).Distinct().Count()==20,"palette has exactly 20 distinct colors");
foreach(var color in StagePad.App.SlotPalette.Colors)Projects.ValidateSlot(new SlotSettings { Color=color.Hex });
Check(true,"all palette colors pass project validation");
bool colorRoundtrip=true;
for(int r=0;r<=255;r+=17)for(int g=0;g<=255;g+=17)for(int b=0;b<=255;b+=17)
{
 var hsv=StagePad.App.ColorMath.FromRgb((byte)r,(byte)g,(byte)b);var rgb=StagePad.App.ColorMath.ToRgb(hsv.Hue,hsv.Saturation,hsv.Value);
 colorRoundtrip &= rgb==((byte)r,(byte)g,(byte)b);
}
Check(colorRoundtrip,"color wheel HSV roundtrip preserves 4096 RGB samples including grays and black");
Check(StagePad.App.ColorMath.ToRgb(360,1,1)==((byte)255,(byte)0,(byte)0) && StagePad.App.ColorMath.ToRgb(180,0,1)==((byte)255,(byte)255,(byte)255),"color wheel hue wraps and center is neutral");
Check(StagePad.App.TitleScrollMotion.Offset(5,100,200)==0,"short title remains stationary");
Check(StagePad.App.TitleScrollMotion.Offset(1,300,160)==0,"long title pauses at start");
Check(Math.Abs(StagePad.App.TitleScrollMotion.Offset(3.2,300,160)+56)<.001,"long title scrolls at 28 units per second");
Check(StagePad.App.TitleScrollMotion.Offset(6.5,300,160)==-140,"long title shows final characters during end pause");
Check(Math.Abs(StagePad.App.TitleScrollMotion.Offset(9.4,300,160)+84)<.001,"long title returns toward start");
Check(Math.Abs(StagePad.App.TitleScrollMotion.Offset(12.4,300,160))<.001,"long title repeats from start");


Check(StagePad.App.HorizontalWheelMotion.Pixels(120)==48,"horizontal wheel scrolls right");
Check(StagePad.App.HorizontalWheelMotion.Pixels(-120)==-48,"horizontal wheel scrolls left");
Check(Math.Abs(StagePad.App.HorizontalWheelMotion.Pixels(1)-.4)<.00001,"trackpad fractional delta is preserved");


var many=Projects.Resize(new ProjectDocument(),100);
Check(many.SlotCount==100 && many.Slots.Length==100 && many.Grid==new GridLayout(10,10) && many.SetupGrid==new GridLayout(10,10),"100 slots fit both 10x10 grids");
Check(many.Slots.Select(s=>s.Midi.Note).Distinct().Count()==100 && many.Slots.All(s=>s.Midi.Note is >=0 and <=127),"100 default MIDI notes remain valid and distinct");
var manyCopy=System.Text.Json.JsonSerializer.Deserialize<ProjectDocument>(System.Text.Json.JsonSerializer.Serialize(many,Projects.Json),Projects.Json)!;
Check(manyCopy.SlotCount==100 && manyCopy.Slots[99].Name=="Slot 100","100-slot JSON roundtrip");
var tagged=many with { Slots=many.Slots.Select((s,i)=>i==99?s with { Name="Finale",File="finale.wav",PadNote=99 }:s).ToArray() };
var smaller=Projects.Resize(tagged,1);var restored=Projects.Resize(smaller,100);
Check(smaller.SlotCount==1 && smaller.Slots.Length==100 && restored.Slots[99]==tagged.Slots[99],"shrinking slots preserves hidden settings and file paths for reactivation");
foreach(int count in new[]{0,101})
{
    bool denied=false;try{Projects.Resize(many,count);}catch(InvalidDataException){denied=true;}
    Check(denied,$"slot count {count} is rejected");
}
foreach(var layout in new[]{new GridLayout(11,10),new GridLayout(10,11)})
{
    bool denied=false;try{Projects.Validate(many with { Grid=layout });}catch(InvalidDataException){denied=true;}
    Check(denied,"grid dimensions cannot exceed 10");
}
Check(Projects.FitGrid(new(16,1),16)==new GridLayout(10,2),"legacy 16x1 layout migrates within 10x10");
var hundredMixer=new Mixer();
for(int index=0;index<Projects.MaxSlots;index++){hundredMixer.Post(new(AudioOp.Install,index,Source:new ConstantSource()));hundredMixer.Post(new(AudioOp.Play,index));}
for(int block=0;block<8;block++)hundredMixer.Render(output);
Check(Enumerable.Range(0,100).All(i=>hundredMixer.View(i).State==VoiceState.Playing) && hundredMixer.CallbackFaults==0,"all 100 voices operate including slot 100");
long hundredBefore=GC.GetAllocatedBytesForCurrentThread();for(int block=0;block<100;block++)hundredMixer.Render(output);
Check(GC.GetAllocatedBytesForCurrentThread()==hundredBefore,"100-voice render has zero managed allocations after warmup");
hundredMixer.Panic();hundredMixer.Render(output);
Check(Enumerable.Range(0,100).All(i=>hundredMixer.View(i).State==VoiceState.Stopped),"PANIC stops all 100 voices");

Check(StagePad.App.LocalizationCatalog.Languages.SequenceEqual(new[]{"it","en","es","fr"}),"four interface languages are available");
foreach(string language in new[]{"en","es","fr"})
{
    Check(StagePad.App.LocalizationCatalog.Entries.All(x=>x.Value.TryGetValue(language,out var value) && !string.IsNullOrWhiteSpace(value)),"localization catalog complete for "+language);
    foreach(var entry in StagePad.App.LocalizationCatalog.Entries)
    {
        var sourceArgs=System.Text.RegularExpressions.Regex.Matches(entry.Key,@"\{(\d+)(?:[^}]*)\}").Select(m=>m.Groups[1].Value).Order().ToArray();
        var translatedArgs=System.Text.RegularExpressions.Regex.Matches(entry.Value[language],@"\{(\d+)(?:[^}]*)\}").Select(m=>m.Groups[1].Value).Order().ToArray();
        if(!sourceArgs.SequenceEqual(translatedArgs))throw new Exception("Localization placeholder mismatch: "+entry.Key);
    }
    Check(true,"format placeholders preserved for "+language);
}
Check(StagePad.App.LocalizationCatalog.Translate("en","Impostazioni")=="Settings" && StagePad.App.LocalizationCatalog.Translate("es","Impostazioni")=="Ajustes" && StagePad.App.LocalizationCatalog.Translate("fr","Impostazioni")=="Paramètres","settings label translates in all target languages");
Check(StagePad.App.LocalizationCatalog.Translate("fr","My custom audio.wav")=="My custom audio.wav","unknown user text is preserved");
Check(StagePad.App.LocalizationCatalog.Message("en","Salvataggio non riuscito: C:\\Audio\\My Track.wav")=="Save failed: C:\\Audio\\My Track.wav","translated error preserves user path");


Check(StagePad.App.FollowScroll.Offset(100,300,1200,140,100)==100,"visible triggered pad leaves viewport unchanged");
Check(StagePad.App.FollowScroll.Offset(100,300,1200,500,100)==300,"offscreen pad to right scrolls only enough to reveal it");
Check(StagePad.App.FollowScroll.Offset(500,300,1200,100,100)==100,"offscreen pad to left scrolls back into view");
Check(StagePad.App.FollowScroll.Offset(0,200,1600,1400,160)==1360,"last row is revealed vertically without overshooting");
Check(StagePad.App.FollowScroll.Offset(0,300,1200,250,100)==50,"partially clipped pad becomes fully visible");
Check(StagePad.App.FollowScroll.Offset(100,300,1200,50,500)==100,"oversized pad already spanning viewport does not cause jumps");
Check(StagePad.App.FollowScroll.Offset(0,300,1200,500,500)==500,"offscreen oversized pad aligns its leading edge");
Check(StagePad.App.FollowScroll.Offset(0,300,500,450,100)==200,"follow scrolling clamps to content extent");
Check(StagePad.App.FollowScroll.Offset(80,0,1200,500,100)==80,"unarranged viewport does not scroll");
var legacyPreferences=StagePad.App.UiPreferences.Parse("{\"language\":\"fr\"}");
Check(legacyPreferences.Language=="fr" && legacyPreferences.FollowTriggeredSlot,"existing language-only settings keep language and enable follow by default");
var disabledPreferences=legacyPreferences with { FollowTriggeredSlot=false };
var changedLanguage=disabledPreferences with { Language="es" };
var preferenceCopy=StagePad.App.UiPreferences.Parse(changedLanguage.ToJson());
Check(preferenceCopy.Language=="es" && !preferenceCopy.FollowTriggeredSlot,"changing language preserves OFF and preferences roundtrip");
var changedFollow=preferenceCopy with { FollowTriggeredSlot=true };
Check(StagePad.App.UiPreferences.Parse(changedFollow.ToJson()).Language=="es","changing follow preference preserves language");
Check(StagePad.App.UiPreferences.Parse("{\"language\":\"de\",\"followTriggeredSlot\":false}")==new StagePad.App.UiPreferences("it",false),"unknown saved language falls back without resetting follow preference");


Check(StagePad.App.ProjectDirectoryPolicy.FileName("My Show")=="My Show.json","integrated save appends JSON extension");
Check(StagePad.App.ProjectDirectoryPolicy.FileName("My Show.JSON")=="My Show.json","integrated save accepts existing JSON extension");
foreach(string invalidName in new[]{"..","../escape","folder/file","folder\\file","CON","CON.txt","Show.","last-project","","a:b"})
{
    bool projectNameRejected=false;try{StagePad.App.ProjectDirectoryPolicy.FileName(invalidName);}catch(InvalidDataException){projectNameRejected=true;}
    Check(projectNameRejected,"integrated project name rejected: "+invalidName);
}
string customProjectDirectory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"StagePadCustomProjects");
var customPreferences=new StagePad.App.UiPreferences("fr",false,customProjectDirectory);
Check(StagePad.App.UiPreferences.Parse(customPreferences.ToJson())==customPreferences,"project directory preference roundtrips with language and follow setting");
var changedPreferences=customPreferences with { Language="es",FollowTriggeredSlot=true };
Check(StagePad.App.UiPreferences.Parse(changedPreferences.ToJson()).ProjectDirectory==customProjectDirectory,"changing other preferences preserves custom project directory");
Check(StagePad.App.UiPreferences.Parse((customPreferences with { ProjectDirectory=null }).ToJson()).ProjectDirectory==null,"reset directory restores dynamic default instead of hardcoded username");
Check(StagePad.App.ProjectDirectoryPolicy.DirectoryPath(customProjectDirectory)==System.IO.Path.GetFullPath(customProjectDirectory),"absolute custom project directory accepted");
bool relativeRejected=false;try{StagePad.App.ProjectDirectoryPolicy.DirectoryPath("relative-folder");}catch(InvalidDataException){relativeRejected=true;}
Check(relativeRejected,"relative project directory is rejected");


var placementWork=new StagePad.App.ScreenBox(0,0,1920,1040);
var rightEditor=StagePad.App.SlotWindowPlacement.Beside(placementWork,new(100,50,200,160));
Check(rightEditor==new StagePad.App.ScreenBox(312,50,650,950),"slot editor opens 650x950 to the right when space permits");
var leftEditor=StagePad.App.SlotWindowPlacement.Beside(placementWork,new(1350,50,200,160));
Check(leftEditor.Left==688 && leftEditor.Right==1338,"slot editor opens to the left when right side is too narrow");
var lowEditor=StagePad.App.SlotWindowPlacement.Beside(placementWork,new(100,900,200,140));
Check(lowEditor.Top==90 && lowEditor.Bottom==1040,"slot editor stays above the taskbar work-area boundary");
var negativeEditor=StagePad.App.SlotWindowPlacement.Beside(new(-1920,0,1920,1040),new(-1800,100,200,160));
Check(negativeEditor.Left==-1588 && negativeEditor.Right<=0,"slot editor supports monitors with negative screen coordinates");
var smallEditor=StagePad.App.SlotWindowPlacement.Beside(new(0,0,500,700),new(100,100,200,160));
Check(smallEditor==new StagePad.App.ScreenBox(0,0,500,700),"slot editor shrinks only when screen work area cannot contain requested size");
var middleEditor=StagePad.App.SlotWindowPlacement.Beside(new(0,0,1280,1020),new(500,100,250,160));
Check(middleEditor.Left>=0 && middleEditor.Right<=1280 && middleEditor.Top>=0 && middleEditor.Bottom<=1020,"slot editor clamps inside screen when neither side fully fits");
var offsetWorkEditor=StagePad.App.SlotWindowPlacement.Beside(new(0,40,1920,1000),new(100,0,200,100));
Check(offsetWorkEditor.Top==40,"slot editor respects a work area with a top toolbar");


Check(StagePad.App.StartupWindowPlacement.Center(new(0,0,1920,1040))==new StagePad.App.ScreenBox(335,40,1250,960),"main window opens centered at 1250x960 pixels");
Check(StagePad.App.StartupWindowPlacement.Center(new(-1920,0,1920,1040))==new StagePad.App.ScreenBox(-1585,40,1250,960),"main window centers on a monitor left of the primary screen");
Check(StagePad.App.StartupWindowPlacement.Center(new(0,40,1920,1040))==new StagePad.App.ScreenBox(335,80,1250,960),"main window centering respects taskbar work area offset");
Check(StagePad.App.StartupWindowPlacement.Center(new(0,0,1280,720))==new StagePad.App.ScreenBox(15,0,1250,720),"main window height fits a shorter screen");
Check(StagePad.App.StartupWindowPlacement.Center(new(100,100,1000,700))==new StagePad.App.ScreenBox(100,100,1000,700),"main window fits smaller work areas without going offscreen");

if (args.Contains("--stress"))
{
    var stress = new Mixer();
    for (int i=0; i<16; i++) { stress.Post(new(AudioOp.Install,i,Source:new ConstantSource())); stress.Post(new(AudioOp.Play,i)); }
    stress.Render(output);
    long allocated = GC.GetAllocatedBytesForCurrentThread(), blocks=0;
    var clock = System.Diagnostics.Stopwatch.StartNew();
    // Exclude Stopwatch setup from allocation measurement.
    allocated = GC.GetAllocatedBytesForCurrentThread();
    while (clock.Elapsed.TotalSeconds < 15) { stress.Render(output); blocks++; }
    long bytes = GC.GetAllocatedBytesForCurrentThread()-allocated;

Console.WriteLine($"Stress diagnostic: managed bytes={bytes}, faults={stress.CallbackFaults}, playing={Enumerable.Range(0,16).Count(i=>stress.View(i).State == VoiceState.Playing)}");
    // The dedicated 1000-block test above strictly checks Render allocations.
    // This full-loop sample also includes harness/runtime work; keep it diagnostic.
    Check(stress.CallbackFaults == 0 && output.All(float.IsFinite) && Enumerable.Range(0,16).All(i=>stress.View(i).State == VoiceState.Playing), "15-second CPU stress with 16 voices, finite output and no source faults");
    Console.WriteLine($"Stress: {blocks} blocks, {blocks*output.Length/2L} stereo frames in {clock.Elapsed.TotalSeconds:0.0}s. Not a real-device endurance test.");
}

foreach(var action in Enum.GetValues<SlotAction>())
{
    Check(SlotActions.From(SlotActions.Audio(action),SlotActions.Midi(action),SlotActions.Osc(action))==action,"independent action combination "+action);
}
var oldSlot=System.Text.Json.JsonSerializer.Deserialize<SlotSettings>("{\"Action\":\"Both\",\"Midi\":{\"Note\":72}}",Projects.Json)!;
Check(SlotActions.MidiList(oldSlot).Length==1 && SlotActions.MidiList(oldSlot)[0].Note==72 && oldSlot.OscCommands.Length==0,"legacy single MIDI slot retains behavior without OSC");
var noArguments=new OscCommand {Address="/go"};
Check(Convert.ToHexString(OscCodec.Encode(noArguments))=="2F676F002C000000","OSC empty message exact wire vector");
Check(System.Text.Encoding.ASCII.GetString(OscCodec.Encode(new(){Address="/cue/*/go"})).StartsWith("/cue/*/go"),"OSC address patterns are transmitted unchanged");
var mixedOsc=new OscCommand {Address="/test",Arguments=[new(OscArgumentType.Int,"42"),new(OscArgumentType.Float,"1.5"),new(OscArgumentType.String,"go"),new(OscArgumentType.True),new(OscArgumentType.False)]};
Check(Convert.ToHexString(OscCodec.Encode(mixedOsc))=="2F746573740000002C696673544600000000002A3FC00000676F0000","OSC integer float string and boolean exact big-endian wire vector");
Check(Convert.ToHexString(OscCodec.Encode(new(){Address="/x",Arguments=[new(OscArgumentType.Int,"-2147483648")]})).EndsWith("80000000"),"OSC preserves signed 32-bit minimum");
Check(MidiCodec.ControlChange(16,127,0)==0x007FBF && MidiCodec.ProgramChange(3,127)==0x7FC2,"MIDI CC and Program Change exact status and payload");
foreach(var invalid in new OscCommand[]{new(){Host="http://localhost"},new(){Port=0},new(){Port=65536},new(){Address="show/go"},new(){Address="/has space"},new(){Address="/bad\0"},new(){Arguments=[new(OscArgumentType.Float,"NaN")]},new(){Arguments=[new(OscArgumentType.Float,"Infinity")]},new(){Arguments=[new(OscArgumentType.Int,"2147483648")]},new(){Arguments=[new(OscArgumentType.String,"bad\0value")]},new(){Arguments=Enumerable.Repeat(new OscArgument(),17).ToArray()}})
{
    bool oscRejected=false;try{OscCodec.Encode(invalid);}catch(InvalidDataException){oscRejected=true;}Check(oscRejected,"invalid OSC input rejected before network dispatch");
}
var combinedSlot=new SlotSettings {Action=SlotAction.All,MidiCommands=[new(){Kind=MidiKind.ControlChange,Controller=12,ControlValue=42},new(){Note=75}],OscCommands=[mixedOsc]};
Projects.ValidateSlot(combinedSlot);
var combinedCopy=System.Text.Json.JsonSerializer.Deserialize<SlotSettings>(System.Text.Json.JsonSerializer.Serialize(combinedSlot,Projects.Json),Projects.Json)!;
Check(combinedCopy.Action==SlotAction.All && combinedCopy.MidiCommands!.Length==2 && combinedCopy.OscCommands[0].Arguments[2].Value=="go","multi-output slot survives JSON roundtrip");
foreach(var invalid in new SlotSettings[]{new(){Action=SlotAction.Osc},new(){Action=SlotAction.Midi,MidiCommands=[]},new(){MidiCommands=Enumerable.Repeat(new MidiSettings(),17).ToArray()},new(){OscCommands=Enumerable.Repeat(new OscCommand(),17).ToArray()},new(){OscCommands=[new(){Arguments=[new(OscArgumentType.String,new string('<',1024)),new(OscArgumentType.String,new string('<',1024))]}]}})
{
    bool batchRejected=false;try{Projects.ValidateSlot(invalid);}catch(InvalidDataException){batchRejected=true;}Check(batchRejected,"empty enabled output or oversized command list rejected");
}
var maximumMidi=new SlotSettings {Action=SlotAction.Midi,MidiCommands=Enumerable.Range(0,16).Select(n=>new MidiSettings{Note=n}).ToArray()};
Projects.ValidateSlot(maximumMidi);Check(true,"16 MIDI commands fit per-slot snapshot budget");
Projects.ValidateSlot(maximumMidi with{Action=SlotAction.All,OscCommands=Enumerable.Repeat(new OscCommand(),16).ToArray()});Check(true,"16 MIDI and 16 OSC commands can be combined in one slot");
bool noAction=false;try{SlotActions.From(false,false,false);}catch(InvalidDataException){noAction=true;}Check(noAction,"slot requires at least one selected action");

var automaticTitle=new SlotSettings{Name="Slot 1",AutoName=true,File="C:\\Music\\Prima.wav"};
Check(SlotTitles.Display(automaticTitle,0)=="Prima" && SlotTitles.Display(automaticTitle with{File="C:\\Music\\Seconda.wav"},0)=="Seconda","automatic title follows Windows audio filename dynamically");
Check(SlotTitles.Display(automaticTitle with{AutoName=false,Name="Slot Drums"},0)=="Slot Drums","custom Slot prefix is not treated as an automatic name");
Check(SlotTitles.Display(new(){Name="Prima",File="/audio/Prima.wav"},0)=="Prima" && SlotTitles.Normalize(new(){Name="Prima",File="/audio/Prima.wav"},0).AutoName==true,"legacy track-derived names migrate to automatic mode");
Check(SlotTitles.Normalize(new(){Name=""},3).AutoName==true && SlotTitles.Display(new(){Name="",AutoName=true},3)=="Slot 4","empty title resets to automatic mode and empty-slot fallback");
Check(MidiCodec.TryControlChange(MidiCodec.ControlChange(16,127,0),out var ccCh,out var ccNumber,out var ccValue) && ccCh==16 && ccNumber==127 && ccValue==0,"CC input decodes channel controller and zero value");
foreach(var invalidPair in new[]{(new MidiCcBinding(0,7),new MidiCcBinding()),(new MidiCcBinding(1,128),new MidiCcBinding()),(new MidiCcBinding(1,7),new MidiCcBinding(1,7))})
{
    bool invalidBinding=false;try{MidiCcBinding.Validate(invalidPair.Item1,invalidPair.Item2);}catch(InvalidDataException){invalidBinding=true;}Check(invalidBinding,"invalid or conflicting fader binding rejected");
}
MidiCcBinding.Validate(new(2,7),new(3,7));Check(true,"same CC number on distinct channels can control separate volumes");

var emergencyMixer=new Mixer();
emergencyMixer.Post(new(AudioOp.Install,0,Source:new ConstantSource()));
emergencyMixer.Post(new(AudioOp.Configure,0,Mode:PlayMode.Hold));
emergencyMixer.Post(new(AudioOp.Play,0));emergencyMixer.Render(output);
long emergencyFrame=emergencyMixer.View(0).Frame;
emergencyMixer.Post(new(AudioOp.Trigger,0));emergencyMixer.EmergencyPause();emergencyMixer.Render(output);
Check(emergencyMixer.View(0).State==VoiceState.Paused && emergencyMixer.View(0).Frame==emergencyFrame && output.All(v=>v==0),"EMERGENCY cancels queued trigger and pauses immediately without losing cursor");
emergencyMixer.Post(new(AudioOp.Release,0));emergencyMixer.Render(output);
Check(emergencyMixer.View(0).State==VoiceState.Paused && emergencyMixer.View(0).Frame==emergencyFrame,"Hold release after EMERGENCY preserves paused cursor");
emergencyMixer.Post(new(AudioOp.Play,0));emergencyMixer.Render(output);
Check(emergencyMixer.View(0).State==VoiceState.Playing && emergencyMixer.View(0).Frame>emergencyFrame,"manual Play resumes after EMERGENCY");
while(emergencyMixer.Post(new(AudioOp.Volume,0,.7f))){}
emergencyFrame=emergencyMixer.View(0).Frame;emergencyMixer.EmergencyPause();emergencyMixer.Render(output);
Check(emergencyMixer.View(0).State==VoiceState.Paused && emergencyMixer.View(0).Frame==emergencyFrame,"EMERGENCY bypasses saturated audio command queue");
for(int n=0;n<10;n++)emergencyMixer.Render(output);
emergencyMixer.Panic();emergencyMixer.Post(new(AudioOp.Trigger,0));emergencyMixer.Render(output);
Check(emergencyMixer.View(0).State==VoiceState.Playing,"fresh trigger immediately following PANIC survives same callback");
var emergencySetting=new SlotSettings{EmergencyPauseAll=true};
Check(System.Text.Json.JsonSerializer.Deserialize<SlotSettings>(System.Text.Json.JsonSerializer.Serialize(emergencySetting,Projects.Json),Projects.Json)!.EmergencyPauseAll,"EMERGENCY flag survives project serialization");
var pickupControl=new VolumeControl();var pickupMap=new MidiCcBinding(1,7,Pickup:true);
Check(pickupControl.Apply(pickupMap,0,.8f,0)==null && pickupControl.Apply(pickupMap,50,.8f,0)==null,"soft takeover suppresses mismatched fader jumps");
Check(pickupControl.Apply(pickupMap,110,.8f,0)==110/127f,"soft takeover activates when fader crosses current volume");
Check(pickupControl.Apply(pickupMap,0,.8f,1)==null,"soft takeover resets on track change");
pickupControl.Reset();pickupControl.Apply(pickupMap,64,64/127f,0);
Check(pickupControl.Apply(pickupMap,65,.9f,0)==null,"GUI volume adjustment rearms soft takeover");
foreach(var modeValue in new[]{(MidiControlMode.RelativeTwosComplement,1,127),(MidiControlMode.RelativeBinaryOffset,65,63),(MidiControlMode.RelativeSignMagnitude,1,65)})
{
    var mapValue=new MidiCcBinding(1,7,modeValue.Item1,Step:.01f);
    Check(Math.Abs(pickupControl.Apply(mapValue,modeValue.Item2,.5f,0)!.Value-.51f)<.0001 && Math.Abs(pickupControl.Apply(mapValue,modeValue.Item3,.5f,0)!.Value-.49f)<.0001,"relative encoder increments and decrements "+modeValue.Item1);
    Check(pickupControl.Apply(mapValue,modeValue.Item2,1,0)==1 && pickupControl.Apply(mapValue,modeValue.Item3,0,0)==0,"relative encoder clamps volume "+modeValue.Item1);
}
foreach(int badDelay in new[]{-1,30001})
{
    bool delayRejected=false;try{Projects.ValidateMidi(new(){DelayMs=badDelay});}catch(InvalidDataException){delayRejected=true;}Check(delayRejected,"invalid MIDI delay rejected");
    delayRejected=false;try{OscCodec.Validate(new(){DelayMs=badDelay});}catch(InvalidDataException){delayRejected=true;}Check(delayRejected,"invalid OSC delay rejected");
}
Console.WriteLine($"{passed} checks passed. Windows/WASAPI/hardware tests are NOT covered by this suite.");

sealed class ThrowingSource : IAudioSource { public long Frames => 48000; public long ResidentBytes => 0; public string? Error => null; public int ReadAt(long p, Span<float> s) => throw new IOException("Simulated"); public void Dispose() { } }
sealed class EmptySource : IAudioSource { public long Frames => 48000; public long ResidentBytes => 0; public string? Error => null; public int ReadAt(long p, Span<float> s) => 0; public void Dispose() { } }

sealed class ConstantSource : IAudioSource { public long Frames => long.MaxValue/4; public long ResidentBytes=>0; public string? Error=>null; public int ReadAt(long p,Span<float> s) { s.Fill(.02f); return s.Length/2; } public void Dispose() { } }

