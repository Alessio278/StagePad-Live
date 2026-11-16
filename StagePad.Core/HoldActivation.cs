namespace StagePad.Core;
// Control-thread state, independent of the audio callback. Each input must
// remain continuously held; repeated presses cannot accelerate activation.
public sealed class HoldActivation
{
    public const int RequiredMs=3000;
    private readonly Dictionary<string,long> held=new(StringComparer.Ordinal);
    private bool consumed;
    public void Press(string input,long now){if(!held.ContainsKey(input))held[input]=now;}
    public void Release(string input){held.Remove(input);if(held.Count==0)consumed=false;}
    public double Progress(long now)=>consumed || held.Count==0?0:Math.Clamp((now-held.Values.Min())/(double)RequiredMs,0,1);
    public bool Consume(long now)
    {if(consumed || held.Count==0 || now-held.Values.Min()<RequiredMs)return false;consumed=true;return true;}
    public void Clear(){held.Clear();consumed=false;}
}
public static class SlotContent
{
    public static bool Configured(SlotSettings s)=>s.Action==SlotAction.Marker || s.EmergencyPauseAll || SlotActions.Audio(s.Action) && !string.IsNullOrWhiteSpace(s.File) || SlotActions.Midi(s.Action) && SlotActions.MidiList(s).Length>0 || SlotActions.Osc(s.Action) && s.OscCommands.Length>0;
}
public sealed record CommandDefaults
{
    public int MidiChannel {get;init;}=1;
    public int MscDeviceId {get;init;}=127;
    public string OscHost {get;init;}="127.0.0.1";
    public int OscPort {get;init;}=8000;
    public string OscAddress {get;init;}="/";
    public void Validate(){Projects.ValidateMidi(new(){Channel=MidiChannel,DeviceId=MscDeviceId});OscCodec.Validate(new(){Host=OscHost,Port=OscPort,Address=OscAddress});}
    public MidiSettings Midi()=>new(){Channel=MidiChannel,DeviceId=MscDeviceId};
    public MidiSettings Resolve(MidiSettings m)=>m.UseShowDestination?m with{Channel=MidiChannel,DeviceId=MscDeviceId,UseShowDestination=false}:m;
    public OscCommand Resolve(OscCommand o)=>o.UseShowDestination?o with{Host=OscHost,Port=OscPort,UseShowDestination=false}:o;
    public SlotSettings Resolve(SlotSettings s)=>s with{MidiCommands=SlotActions.MidiList(s).Select(m=>Resolve(m)).ToArray(),OscCommands=s.OscCommands.Select(o=>Resolve(o)).ToArray()};
    public OscCommand Osc()=>new(){Host=OscHost,Port=OscPort,Address=OscAddress};
}
