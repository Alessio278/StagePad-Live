namespace StagePad.Core;

public sealed record MarkerTrim(string? StartId=null,string? EndId=null);

public static class MarkerTrims
{
    public static void Validate(MarkerTrim? trim,AudioMarker[] markers)
    {
        if(trim==null)return;
        var start=trim.StartId==null?null:markers.FirstOrDefault(m=>m.Id==trim.StartId);
        var end=trim.EndId==null?null:markers.FirstOrDefault(m=>m.Id==trim.EndId);
        if(trim.StartId!=null && start==null || trim.EndId!=null && end==null)
            throw new InvalidDataException("Marker di taglio non trovato.");
        if(end!=null && end.Seconds-(start?.Seconds??0)<MarkerLoops.MinimumSeconds)
            throw new InvalidDataException("Fine traccia dopo inizio: distanza minima 50 ms.");
    }
    public static (long Start,long End) Frames(MarkerTrim? trim,AudioMarker[] markers,double duration)
    {
        Validate(trim,markers);
        if(!double.IsFinite(duration) || duration<0)throw new InvalidDataException("Durata traccia non valida.");
        long total=(long)Math.Round(duration*Mixer.Rate);
        long start=trim?.StartId is {} a?(long)Math.Round(markers.First(m=>m.Id==a).Seconds*Mixer.Rate):0;
        long end=trim?.EndId is {} b?(long)Math.Round(markers.First(m=>m.Id==b).Seconds*Mixer.Rate):total;
        if(start<0 || end>total || end<start || trim!=null && end-start<(long)(MarkerLoops.MinimumSeconds*Mixer.Rate))
            throw new InvalidDataException("Il taglio deve restare dentro la traccia, con almeno 50 ms di audio.");
        return(start,end);
    }
    public static void ValidateLoop(MarkerTrim? trim,MarkerLoop? loop,AudioMarker[] markers)
    {
        Validate(trim,markers);
        if(trim==null || loop is not {Enabled:true})return;
        var range=MarkerLoops.Frames(loop,markers);
        long start=trim.StartId is {} a?(long)Math.Round(markers.First(m=>m.Id==a).Seconds*Mixer.Rate):0;
        long end=trim.EndId is {} b?(long)Math.Round(markers.First(m=>m.Id==b).Seconds*Mixer.Rate):long.MaxValue;
        if(range.Start<start || range.End>end)throw new InvalidDataException("Il loop deve essere compreso tra inizio e fine traccia.");
    }
}

/// <summary>Playback boundaries with an optional preloaded start, owned by the control thread.</summary>
public sealed record TrimRegion(long Start,long End,SeekBuffer? Preview=null);
