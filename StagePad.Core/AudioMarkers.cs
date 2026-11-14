using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace StagePad.Core;
public sealed record AudioMarker
{
    public string Id {get;init;}=Guid.NewGuid().ToString("N");
    public double Seconds {get;init;}
    public string Label {get;init;}="Marker";
    public string Color {get;init;}="#75E2C1";
    public MidiSettings? MidiTrigger {get;init;}
    public OscCommand? OscTrigger {get;init;}
}
public static class AudioMarkers
{
    public const int Maximum=32;
    public static bool Same(AudioMarker[]? left,AudioMarker[]? right,bool displayOnly=false)
    {
        if(ReferenceEquals(left,right))return true;
        if(left==null || right==null || left.Length!=right.Length)return false;
        for(int i=0;i<left.Length;i++)
        {
            var a=left[i];var b=right[i];
            if(a.Id!=b.Id || a.Seconds!=b.Seconds || a.Label!=b.Label || a.Color!=b.Color)return false;
            if(!displayOnly)
            {
                if(a.MidiTrigger!=b.MidiTrigger)return false;
                var x=a.OscTrigger;var y=b.OscTrigger;
                if(x!=null && y!=null){if(x.Host!=y.Host || x.Port!=y.Port || x.Address!=y.Address || x.DelayMs!=y.DelayMs || !x.Arguments.SequenceEqual(y.Arguments))return false;}
                else if(x!=y)return false;
            }
        }
        return true;
    }
    public static void Validate(AudioMarker[]? markers,double? duration=null)
    {
        if(markers==null || markers.Length>Maximum)throw new InvalidDataException("Massimo 32 marker per traccia.");
        if(markers.Length==0)return;
        var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach(var m in markers)
        {
            if(m==null || !Guid.TryParseExact(m.Id,"N",out _) || !ids.Add(m.Id) || !double.IsFinite(m.Seconds) || m.Seconds<0 || m.Seconds>31536000 ||
                string.IsNullOrWhiteSpace(m.Label) || m.Label.Length>64 || m.Label.Any(char.IsControl) || !Regex.IsMatch(m.Color??"","^#[0-9A-Fa-f]{6}$"))
                throw new InvalidDataException("Marker non valido: posizione, etichetta o colore.");
            if(m.MidiTrigger!=null)
            {
                Projects.ValidateMidi(m.MidiTrigger);
                if(m.MidiTrigger.DelayMs!=0 || m.MidiTrigger.Behavior!=NoteBehavior.Pulse)throw new InvalidDataException("I trigger marker MIDI sono immediati e le note usano Note Off automatico.");
            }
            if(m.OscTrigger!=null){OscCodec.Validate(m.OscTrigger);if(m.OscTrigger.DelayMs!=0)throw new InvalidDataException("I trigger marker OSC sono immediati.");}
            if(duration.HasValue && m.Seconds>duration.Value)throw new InvalidDataException("Il marker deve essere dentro la durata della traccia.");
        }
        if(JsonSerializer.SerializeToUtf8Bytes(markers).Length>8192)throw new InvalidDataException("Marker troppo grandi: riduci etichette o trigger.");
    }
    public static AudioMarker[] Change(AudioMarker[] current,string operation,AudioMarker? marker,string? id,double duration)
    {
        Validate(current);
        AudioMarker[] next;
        if(operation=="marker-add")
        {
            if(marker==null || current.Any(m=>m.Id==marker.Id))throw new InvalidDataException("Marker già presente o non valido.");
            next=[..current,marker];
        }
        else
        {
            if(id==null || !current.Any(m=>m.Id==id))throw new InvalidDataException("Il marker non esiste più. Aggiorna l’elenco.");
            next=operation switch
            {
                "marker-edit" when marker!=null && marker.Id==id=>current.Select(m=>m.Id==id?marker:m).ToArray(),
                "marker-delete"=>current.Where(m=>m.Id!=id).ToArray(),
                _=>throw new InvalidDataException("Operazione marker non valida.")
            };
        }
        if(marker!=null && operation is "marker-add" or "marker-edit")Validate([marker],duration);
        Validate(next);return next.OrderBy(m=>m.Seconds).ThenBy(m=>m.Id,StringComparer.Ordinal).ToArray();
    }
    public static string Format(double seconds)
    {
        long milliseconds=(long)Math.Round(Math.Max(0,seconds)*1000,MidpointRounding.AwayFromZero);
        return string.Create(CultureInfo.InvariantCulture,$"{milliseconds/3600000:00}:{milliseconds/60000%60:00}:{milliseconds/1000%60:00}.{milliseconds%1000:000}");
    }
    public static bool TryParse(string text,out double seconds)
    {
        seconds=0;var fields=text.Trim().Replace(',','.').Split(':');
        if(fields.Length is <1 or >3)return false;
        for(int i=0;i<fields.Length;i++)
        {
            if(!double.TryParse(fields[i],NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out double value) || !double.IsFinite(value) || value<0 || (i>0 && value>=60) || (i<fields.Length-1 && value!=Math.Truncate(value)))return false;
            seconds=seconds*60+value;
        }
        return seconds<=31536000;
    }
}

public static class MarkerTriggers
{
    public static int Scope(int slot,int index)=>10000+slot*AudioMarkers.Maximum+index;
}
