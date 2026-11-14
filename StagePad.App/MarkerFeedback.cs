using StagePad.Core;
namespace StagePad.App;
// UI-only cue crossing detector. Never called by the audio render callback.
internal sealed class MarkerFeedback
{
    private string? context;
    private AudioMarker[]? previousMarkers;
    private double previous;
    public const int PulseCount=7, PulsePeriodMs=260, PulseOnMs=150;
    public const int DurationMs=PulseCount*PulsePeriodMs;
    private long tick,flashStarted,previousPass;
    private bool wasPlaying,initialized,suppressInitial;
    public AudioMarker? Active {get;private set;}
    public void Reset(){context=null;previousMarkers=null;Active=null;initialized=false;wasPlaying=false;tick=0;suppressInitial=true;}
    public void Observe(string key,AudioMarker[] markers,double position,bool playing,bool seeking,long engineTick,long now,bool ended=false,long loopPass=0,AudioMarker? loopEnd=null,AudioMarker? loopStart=null)
    {
        if(Active!=null && now-flashStarted>=DurationMs)Active=null;
        if(!double.IsFinite(position) || engineTick<=0 || now-engineTick>500 || seeking)
        {Active=null;initialized=false;wasPlaying=false;return;}
        if(loopStart!=null || loopEnd!=null)
        {
            // An active loop owns the visual feedback for its entire lifetime.
            // Keep a fresh baseline so removing it cannot replay earlier markers.
            Active=null;context=key;previousMarkers=markers;previous=position;tick=engineTick;
            previousPass=loopPass;wasPlaying=playing;initialized=true;suppressInitial=false;return;
        }
        if(context!=key || (previousMarkers==null || !AudioMarkers.Same(markers,previousMarkers,true)))
        {
            bool changed=context!=key;context=key;previousMarkers=markers;previous=position;tick=engineTick;previousPass=loopPass;wasPlaying=playing;initialized=true;Active=null;
            double initial=loopStart?.Seconds??0;
            if(changed && !suppressInitial && playing && position>=initial && position<=initial+.25)
            {foreach(var marker in markers)if(marker.Seconds>=initial && marker.Seconds<=position && (Active==null || marker.Seconds>=Active.Seconds))Active=marker;if(Active!=null)flashStarted=now;}
            suppressInitial=false;return;
        }
        if(!initialized){previous=position;tick=engineTick;previousPass=loopPass;wasPlaying=playing;initialized=true;return;}
        if(engineTick==tick)return;
        if(playing && loopPass>previousPass)
        {
            // Wrapping is not a new marker crossing at either loop boundary.
            // Interior markers already reached after the wrap still get their cue.
            Active=null;
            foreach(var marker in markers)
                if(marker.Seconds>(loopStart?.Seconds??position) && marker.Seconds<(loopEnd?.Seconds??double.PositiveInfinity) && marker.Seconds<=position && (Active==null || marker.Seconds>=Active.Seconds))Active=marker;
            if(Active!=null)flashStarted=now;
            previous=position;tick=engineTick;previousPass=loopPass;wasPlaying=playing;return;
        }
        double elapsed=Math.Max(0,engineTick-tick)/1000.0,delta=position-previous;
        if(playing || (ended && wasPlaying))
        {
            double start=previous;
            bool discontinuity=delta<-.00001 || delta>elapsed+.15;
            if(!wasPlaying && previous<=.000001 && loopStart!=null && position>=loopStart.Seconds && position<=loopStart.Seconds+elapsed+.15)start=loopStart.Seconds-.000001;
            else if(discontinuity){Active=null;start=position<=.05 && delta<0 ? -.000001 : position;}
            else if(!wasPlaying && previous<=.000001)start=-.000001;
            AudioMarker? reached=null;
            foreach(var marker in markers)
                if(marker.Seconds>start && marker.Seconds<=position+.000001 && (reached==null || marker.Seconds>=reached.Seconds))reached=marker;
            if(reached!=null){Active=reached;flashStarted=now;}
        }
        else if(!ended)Active=null;
        previous=position;tick=engineTick;previousPass=loopPass;wasPlaying=playing;
    }
    public bool InProgress(long now)=>Active!=null && now>=flashStarted && now-flashStarted<DurationMs;
    public bool Illuminated(long now,bool animate)=>InProgress(now) && (!animate || (now-flashStarted)%PulsePeriodMs<PulseOnMs);
}
