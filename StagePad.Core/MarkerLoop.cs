namespace StagePad.Core;

public sealed record MarkerLoop(bool Enabled=false,string? StartId=null,string? EndId=null);

public static class MarkerLoops
{
    public const double MinimumSeconds=.05;
    public static AudioMarker? End(MarkerLoop? loop,AudioMarker[] markers)=>loop is {Enabled:true}?markers.FirstOrDefault(m=>m.Id==loop.EndId):null;
    public static (long Start,long End) Frames(MarkerLoop loop,AudioMarker[] markers,double? duration=null)
    {
        var start=markers.FirstOrDefault(m=>m.Id==loop.StartId);
        var end=markers.FirstOrDefault(m=>m.Id==loop.EndId);
        if(start==null || end==null || start.Id==end.Id || end.Seconds-start.Seconds<MinimumSeconds)
            throw new InvalidDataException("Scegli due marker distinti: fine dopo inizio, distanza minima 50 ms.");
        if(duration.HasValue && end.Seconds>duration.Value)throw new InvalidDataException("Il marker deve essere dentro la durata della traccia.");
        return ((long)Math.Round(start.Seconds*Mixer.Rate),(long)Math.Round(end.Seconds*Mixer.Rate));
    }
    public static void Validate(MarkerLoop? loop,AudioMarker[] markers)
    {
        if(loop==null)return;
        if(loop.Enabled || loop.StartId!=null || loop.EndId!=null)Frames(loop,markers);
    }
}

/// <summary>Immutable, fully resident region prepared away from the audio callback.</summary>
public sealed class LoopRegion
{
    public SeekBuffer Buffer {get;}
    public long Start=>Buffer.StartFrame;
    public long End=>Start+Buffer.Frames;
    public LoopRegion(SeekBuffer buffer)
    {
        if(buffer.StartFrame<0 || buffer.Frames<2400)throw new ArgumentException("Invalid loop region.");
        Buffer=buffer;
    }
}
