using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Text;
namespace StagePad.Core;
public enum OscArgumentType { Int, Float, String, True, False }
public sealed record OscArgument(OscArgumentType Type=OscArgumentType.Int,string Value="1");
public sealed record OscCommand
{
    public int DelayMs {get;init;}
    public string Host {get;init;}="127.0.0.1";
    public int Port {get;init;}=8000;
    public string Address {get;init;}="/go";
    public OscArgument[] Arguments {get;init;}=[];
}
public static class SlotActions
{
    public static bool Audio(SlotAction a)=>a is SlotAction.Audio or SlotAction.Both or SlotAction.AudioOsc or SlotAction.All;
    public static bool Midi(SlotAction a)=>a is SlotAction.Midi or SlotAction.Both or SlotAction.MidiOsc or SlotAction.All;
    public static bool Osc(SlotAction a)=>a is SlotAction.Osc or SlotAction.AudioOsc or SlotAction.MidiOsc or SlotAction.All;
    public static MidiSettings[] MidiList(SlotSettings s)=>s.MidiCommands??[s.Midi];
    public static SlotAction From(bool audio,bool midi,bool osc)=>(audio,midi,osc) switch
    {
        (true,false,false)=>SlotAction.Audio,(false,true,false)=>SlotAction.Midi,(true,true,false)=>SlotAction.Both,
        (false,false,true)=>SlotAction.Osc,(true,false,true)=>SlotAction.AudioOsc,(false,true,true)=>SlotAction.MidiOsc,
        (true,true,true)=>SlotAction.All,_=>throw new InvalidDataException("Seleziona almeno un’azione: Audio, MIDI o OSC.")
    };
}
public static class OscCodec
{
    public static void Validate(OscCommand c)
    {
        if(c is null || c.DelayMs is <0 or >30000 || string.IsNullOrWhiteSpace(c.Host) || c.Host.Length>253 ||
            (!IPAddress.TryParse(c.Host,out _) && Uri.CheckHostName(c.Host)!=UriHostNameType.Dns) || c.Port is <1 or >65535)
            throw new InvalidDataException("Destinazione OSC non valida: host e porta 1-65535.");
        if(string.IsNullOrEmpty(c.Address) || c.Address.Length>512 || c.Address[0]!='/' ||
            c.Address.Any(ch=>ch<=32 || ch>126))throw new InvalidDataException("Indirizzo OSC non valido: usa un percorso come /show/go.");
        if(c.Arguments is null || c.Arguments.Length>16)throw new InvalidDataException("Massimo 16 argomenti per comando OSC.");
        foreach(var a in c.Arguments)
        {
            if(a is null || !Enum.IsDefined(a.Type) || a.Value is null || a.Value.Length>1024 || a.Value.Any(ch=>ch==0 || ch>127))throw new InvalidDataException("Argomento OSC non valido: massimo 1024 caratteri ASCII.");
            if(a.Type==OscArgumentType.Int && !int.TryParse(a.Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out _))throw new InvalidDataException("Argomento OSC intero non valido.");
            if(a.Type==OscArgumentType.Float && (!float.TryParse(a.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var f) || !float.IsFinite(f)))throw new InvalidDataException("Argomento OSC decimale non valido.");
        }
    }
    public static byte[] Encode(OscCommand command)
    {
        Validate(command);
        using var stream=new MemoryStream();
        void String(string value){var bytes=Encoding.ASCII.GetBytes(value);stream.Write(bytes);stream.WriteByte(0);while(stream.Length%4!=0)stream.WriteByte(0);}
        String(command.Address);
        String(","+string.Concat(command.Arguments.Select(a=>a.Type switch{OscArgumentType.Int=>'i',OscArgumentType.Float=>'f',OscArgumentType.String=>'s',OscArgumentType.True=>'T',_=>'F'})));
        Span<byte> bytes=stackalloc byte[4];
        foreach(var a in command.Arguments)
        {
            if(a.Type==OscArgumentType.String){String(a.Value);continue;}
            if(a.Type is OscArgumentType.True or OscArgumentType.False)continue;
            int value=a.Type==OscArgumentType.Int?int.Parse(a.Value,CultureInfo.InvariantCulture):BitConverter.SingleToInt32Bits(float.Parse(a.Value,CultureInfo.InvariantCulture));
            BinaryPrimitives.WriteInt32BigEndian(bytes,value);stream.Write(bytes);
        }
        return stream.ToArray();
    }
}
