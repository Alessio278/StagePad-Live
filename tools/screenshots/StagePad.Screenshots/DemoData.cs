using System.IO;
using System.Text;
using StagePad.Core;
namespace StagePad.App;
internal sealed class DemoData
{
    public ProjectDocument Project {get;}
    public float[] Peaks {get;}
    public string ShowPath {get;}
    public const double Duration=30;
    private readonly string language;
    private DemoData(ProjectDocument project,float[] peaks,string showPath,string language){Project=project;Peaks=peaks;ShowPath=showPath;this.language=language;}
    public static DemoData Create(string folder,string language="it")
    {
        string T(string key)=>DemoText.Get(language,key);
        Directory.CreateDirectory(folder);string audio=Path.Combine(folder,"audio"),shows=Path.Combine(folder,"project");Directory.CreateDirectory(audio);Directory.CreateDirectory(shows);
        string[] titles=["Intro - Aurora",T("ambient"),T("guest"),T("percussion"),T("transition"),T("ending")];
        string[] colors=["#75E2C1","#629FF4","#B596FF","#FFBD70","#F084A0","#6ED6DA"];
        var files=new List<string>();float[] peaks=[];
        for(int i=0;i<titles.Length;i++)
        {string path=Path.Combine(audio,$"{i+1:00} - {titles[i]}.wav");var wave=Wave(path,i);if(i==0)peaks=wave;files.Add(path);}
        var marks=new AudioMarker[]{
            new(){Id="11111111111111111111111111111111",Seconds=0,Label="Intro",Color="#75E2C1"},
            new(){Id="22222222222222222222222222222222",Seconds=8,Label=T("entrance"),Color="#B596FF",MidiTrigger=new(){Kind=MidiKind.ControlChange,Channel=1,Controller=20,ControlValue=90}},
            new(){Id="33333333333333333333333333333333",Seconds=16,Label=T("opening"),Color="#FFBD70",OscTrigger=new(){Host="127.0.0.1",Port=9000,Address="/scene/recall",Arguments=[new(OscArgumentType.Int,"2")]}},
            new(){Id="44444444444444444444444444444444",Seconds=24,Label=T("ending"),Color="#F084A0"}};
        var slots=Enumerable.Range(0,16).Select(i=>Projects.DefaultSlot(i) with{PadChannel=10,PadNote=36+i}).ToArray();
        for(int i=0;i<6;i++)slots[i]=slots[i] with{Name=titles[i],AutoName=false,File=files[i],Action=SlotAction.Audio,Color=colors[i],Volume=.65f+i*.05f,Mode=i==1?PlayMode.TogglePause:PlayMode.OneShot,Markers=i==0?marks:[]};
        MidiSettings cue=new(){Kind=MidiKind.MSC,Cue="12.000",DeviceId=127};
        slots[1]=slots[1] with{Action=SlotAction.Both,MidiCommands=[cue]};
        slots[2]=slots[2] with{Action=SlotAction.AudioOsc,OscCommands=[new(){Host="127.0.0.1",Port=9000,Address="/ch/01/mix/on",Arguments=[new(OscArgumentType.Int,"1")]}]};
        slots[6]=slots[6] with{Name=T("scene")+" - Cue 12",AutoName=false,Action=SlotAction.Midi,Color="#B596FF",MidiCommands=[cue,cue with{Cue="13.000",DelayMs=1200}]};
        slots[7]=slots[7] with{Name=T("voice")+" ON",AutoName=false,Action=SlotAction.Osc,Color="#6ED6DA",OscCommands=[new(){Host="127.0.0.1",Port=9000,Address="/ch/01/mix/on",Arguments=[new(OscArgumentType.Int,"1")]}]};
        slots[8]=slots[8] with{Name=T("change"),AutoName=false,Action=SlotAction.MidiOsc,Color="#FFBD70",MidiCommands=[new(){Kind=MidiKind.ProgramChange,Channel=1,Program=8}],OscCommands=[new(){Host="127.0.0.1",Port=9000,Address="/scene/recall",Arguments=[new(OscArgumentType.Int,"3")]}]};
        slots[9]=slots[9] with{Name=T("reverb")+" - ON",AutoName=false,Action=SlotAction.Osc,Color="#629FF4",OscCommands=[new(){Host="127.0.0.1",Port=9000,Address="/fx/01/on",Arguments=[new(OscArgumentType.Int,"1")]}]};
        slots[10]=slots[10] with{Name=T("sequence"),AutoName=false,Action=SlotAction.Midi,Color="#B596FF",MidiCommands=[new(){Kind=MidiKind.ControlChange,Channel=1,Controller=21,ControlValue=90},new(){Kind=MidiKind.ControlChange,Channel=1,Controller=22,ControlValue=64,DelayMs=750},new(){Kind=MidiKind.ProgramChange,Channel=1,Program=4,DelayMs=500}]};
        slots[11]=slots[11] with{Name=T("voice")+" OFF",AutoName=false,Action=SlotAction.Osc,Color="#F084A0",OscCommands=[new(){Host="127.0.0.1",Port=9000,Address="/ch/01/mix/on",Arguments=[new(OscArgumentType.Int,"0")]}]};
        slots[13]=slots[13] with{Name=T("ready"),AutoName=false,Action=SlotAction.None};
        slots[14]=slots[14] with{Name="+ MARKER",AutoName=false,Action=SlotAction.Marker,Color="#75E2C1"};
        slots[15]=slots[15] with{Name=T("emergency"),AutoName=false,Action=SlotAction.MidiOsc,EmergencyPauseAll=true,EmergencyHold=true,Color="#EF5B68",MidiCommands=[new(){Kind=MidiKind.ControlChange,Channel=1,Controller=123,ControlValue=0}],OscCommands=[new(){Host="127.0.0.1",Port=9000,Address="/main/st/mix/on",Arguments=[new(OscArgumentType.Int,"0")]}]};
        slots[0]=slots[0] with{MarkerTrim=new(marks[1].Id,marks[3].Id)};
        var project=new ProjectDocument{Name=T("show"),Slots=slots,SlotCount=16,Master=.82f,DefaultSlotVolume=.8f,MasterControl=new(1,7),CurrentTrackControl=new(1,11),MarkerCapture=new(1,100),CommandDefaults=new(){MidiChannel=1,OscHost="127.0.0.1",OscPort=9000,OscAddress="/scene/recall"},Routing=new(){AudioDevice="demo-audio",MidiInput="demo-pad",MidiOutput="demo-output"}};
        Projects.Validate(project);string showPath=Path.Combine(shows,T("show")+".json");
        ProjectFiles.Write(showPath,project,false);
        foreach(string name in new[]{T("rehearsal"),T("electronic"),"Soundcheck"})ProjectFiles.Write(Path.Combine(shows,name+".json"),project with{Name=name},false);
        foreach(string path in Directory.GetFiles(shows,"*.json"))File.SetLastWriteTimeUtc(path,new DateTime(2026,1,15,15,30,0,DateTimeKind.Utc));
        return new(project,peaks,showPath,language);
    }
    public StateInfo State(bool loop=false,bool live=false)
    {
        string T(string key)=>DemoText.Get(language,key);
        long now=Environment.TickCount64;var project=Project;
        if(loop){var slots=(SlotSettings[])project.Slots.Clone();slots[0]=slots[0] with{MarkerLoop=new(true,"22222222222222222222222222222222","44444444444444444444444444444444")};project=project with{Slots=slots};}
        var views=project.Slots.Select((s,i)=>new SlotInfo(i,SlotTitles.Display(s,i),i==0?"Playing":SlotActions.Audio(s.Action)&&s.File!=null?"Ready":"Empty",i==0?(loop?12.4:10.6):0,s.File!=null?Duration:0,s.Volume,0,null,false,Revision:1,LoopPass:loop&&i==0?2:0,TriggerCount:i==8?1:0,TriggeredAt:i==8?now:0)).ToArray();
        var input=new MidiPortState("demo-input","Connesso",null,"demo-pad",[new("demo-pad","Controller DEMO ("+T("simulated")+")")],now,"CH 10 · Note 44",27);
        var output=new MidiPortState("demo-output","Connesso",null,"demo-output",[new("demo-output","MIDI OUT DEMO ("+T("simulated")+")")],now,"Program 8 · CH 1",18,FeedbackStatus:"OFF");
        return new("0123456789abcdef0123456789abcdef",T("audioReady")+" · DEMO",null,"demo-audio",40,project.Master,48*1048576,0,false,[new("demo-audio","Audio DEMO - Stereo")],views,project,ShowLocked:live,MidiIn:input,MidiOut:output,Heartbeat:now,RenderHeartbeat:now,DeviceHeartbeat:now,ProjectPath:ShowPath,OscSent:12,CurrentPlaying:0,MarkerSetup:!live,LastSavedAt:new DateTimeOffset(2026,1,15,15,30,0,TimeSpan.Zero).ToUnixTimeSeconds());
    }
    private static float[] Wave(string path,int voice)
    {
        const int rate=24000;int count=(int)(rate*Duration);float[] peaks=new float[1024];
        using var writer=new BinaryWriter(File.Create(path),Encoding.ASCII);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
        for(int i=0;i<count;i++)
        {
            double t=i/(double)rate;
            double pulse=.35+.65*Math.Pow(Math.Sin(t*(voice+2)*1.3),4);double fade=Math.Min(1,t/.7)*Math.Min(1,(Duration-t)/.8);
            double sample=(Math.Sin(2*Math.PI*(110+voice*27.5)*t)*.25+Math.Sin(2*Math.PI*(165+voice*11)*t)*.14+Math.Sin(2*Math.PI*55*t)*.1)*pulse*fade;
            sample*=.7+.3*Math.Sin(t*.6+voice);short value=(short)(Math.Clamp(sample,-.95,.95)*32767);writer.Write(value);
            int bin=Math.Min(peaks.Length-1,(int)((long)i*peaks.Length/count));peaks[bin]=Math.Max(peaks[bin],Math.Abs(value)/32768f);
        }
        return peaks;
    }
}
