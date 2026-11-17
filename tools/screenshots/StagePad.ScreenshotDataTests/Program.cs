using StagePad.App;
using StagePad.Core;
string root=Path.Combine(Path.GetTempPath(),"StagePad-demo-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);Environment.SetEnvironmentVariable("STAGEPAD_DATA_DIR",root);
try
{
    foreach(string language in DemoText.Languages)
    {
    string folder=Path.Combine(root,language);
    var demo=DemoData.Create(folder,language);Projects.Validate(demo.Project);
    if(demo.Project.SlotCount!=16 || demo.Project.Slots.Count(s=>s.File!=null)!=6 || demo.Project.Slots[0].Markers.Length!=4)throw new Exception("Incomplete demo.");
    if(demo.Peaks.Length!=1024 || demo.Peaks.Any(p=>!float.IsFinite(p)||p<0||p>1) || demo.Peaks.Max()<.1)throw new Exception("Invalid waveform.");
    foreach(var slot in demo.Project.Slots.Where(s=>s.File!=null))
    {
        using var file=File.OpenRead(slot.File!);using var reader=new BinaryReader(file);
        if(new string(reader.ReadChars(4))!="RIFF" || file.Length!=44+(int)(DemoData.Duration*24000*2))throw new Exception("Invalid generated WAV.");
    }
    foreach(bool looping in new[]{false,true})
    {
        var state=demo.State(looping);Projects.Validate(state.Project!);
        if(state.Slots.Length!=16 || state.Slots[0].Duration!=30 || (state.Project!.Slots[0].MarkerLoop!=null)!=looping)throw new Exception("Invalid demo snapshot.");
    }
    if(Directory.GetFiles(Path.GetDirectoryName(demo.ShowPath)!,"*.json").Length!=4)throw new Exception("Missing show list.");
    if(demo.Project.Slots.SelectMany(s=>s.OscCommands).Any(o=>o.Host!="127.0.0.1"))throw new Exception("Nonlocal demo destination.");
    string[] captions=["01-setup.png","02-live.png","03-player-marker.png","04-player-loop.png","05-markers.png","06-slot-audio.png","07-slot-midi.png","08-slot-osc.png","09-emergency.png","10-slot-marker.png","11-settings.png","12-midi-volumes.png","13-shows.png"];
    foreach(string caption in captions)if(string.IsNullOrWhiteSpace(DemoText.Get(language,caption)))throw new Exception("Missing caption.");
    foreach(string key in new[]{"title","notice","readme"})if(string.IsNullOrWhiteSpace(DemoText.Get(language,key)))throw new Exception("Missing gallery text.");
    if(!Path.GetFileName(demo.ShowPath).Contains(DemoText.Get(language,"show")) || !Path.GetFileName(demo.Project.Slots[1].File!).Contains(DemoText.Get(language,"ambient")))throw new Exception("Untranslated demo filenames.");
    if(demo.Project.Slots[6].Name!=DemoText.Get(language,"scene")+" - Cue 12" || demo.Project.Slots[15].Name!=DemoText.Get(language,"emergency"))throw new Exception("Untranslated slot names.");
    if(demo.Project.Name!=DemoText.Get(language,"show") || demo.Project.Slots[0].Markers[1].Label!=DemoText.Get(language,"entrance") || demo.Project.Slots[1].Name!=DemoText.Get(language,"ambient"))throw new Exception("Demo text language mismatch.");
    Console.WriteLine("PASS: "+language+" - localized demo, six WAVs, waveform, 16 slots, markers, loops and shows.");
    }
    Console.WriteLine("Native WPF capture requires Windows and is not covered by this check.");
}
finally{Directory.Delete(root,true);}
