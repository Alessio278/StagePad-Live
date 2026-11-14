using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StagePad.Core;
namespace StagePad.App;
internal sealed partial class MainWindow
{
    private readonly string markerContextId=Guid.NewGuid().ToString("N");
    private long markerContextSequence;
    private bool enteringLive;
    private Request MarkerContextRequest(string op,bool? setup=null)=>new(op,current,DeviceId:last?.Instance,Flag:setup??(!liveView && !enteringLive),Origin:markerContextId,Epoch:++markerContextSequence,SourceRevision:last!=null && (uint)current<last.Slots.Length?last.Slots[current].Revision:-1);
    private async Task<Reply> SyncMarkerContext(bool? setup=null)=>last==null?new(false,"Motore non raggiungibile."):await SendReply(MarkerContextRequest("marker-context",setup));
    private async Task EnterLive()
    {
        if(enteringLive)return;enteringLive=true;
        try
        {
            if(last!=null)
            {
                var reply=await SyncMarkerContext(false);if(!reply.Ok){Warn(reply.Error??"Contesto marker non aggiornato.");return;}
            }
            SetView(true);
        }
        finally{enteringLive=false;}
    }
    private MarkerWindow? markerWindow;
    private readonly MarkerFeedback markerFeedback=new();
    private Border? playerChrome,playerHaloWide,playerHaloInner;
    private Button? addMarkerButton,manageMarkersButton;
    private readonly Brush playerRed=Brush("#EF5B68"),playerRedDim=Brush("#803841"),playerNeutral=Brush("#293C51");
    private string? markerEffectKey,cachedMarkerColor;
    private Brush? cachedMarkerBrush;
    private StackPanel MarkerControls()
    {
        var row=new StackPanel{Orientation=Orientation.Vertical,Margin=new Thickness(0,3,0,0)};
        addMarkerButton=Button("+ Marker",QuickAddMarker);
        manageMarkersButton=Button("Gestisci marker",()=>{OpenMarkers(SelectedPlayerPosition());return Task.CompletedTask;});
        row.Children.Add(addMarkerButton);row.Children.Add(manageMarkersButton);return row;
    }
    private async Task QuickAddMarker()
    {
        if(last?.Project==null || (uint)current>=last.Slots.Length || last.ShowLocked)return;
        double position=SelectedPlayerPosition();
        var slot=last.Slots[current];
        if(slot.State!="Playing")
        {var window=OpenMarkers(position);if(window!=null)await window.AddAt(position);return;}
        var markers=last.Project.Slots[current].Markers;
        var marker=new AudioMarker{Seconds=Math.Clamp(position,0,slot.Duration),Label="Marker "+(markers.Length+1)};
        var reply=await SendReply(new("marker-add",current,Origin:last.Instance,SourceRevision:slot.Revision,Marker:marker));
        if(!reply.Ok)Warn(reply.Error??"Comando non riuscito.");
    }
    private double SelectedPlayerPosition()
    {
        if(last==null || (uint)current>=last.Slots.Length)return 0;
        var slot=last.Slots[current];long anchor=last.RenderHeartbeat>0?last.RenderHeartbeat:last.Heartbeat;
        return UiPlaybackClock.Read(slot.Position,slot.Duration,slot.State=="Playing" && last.AudioError==null,slot.State=="Ended",anchor,Environment.TickCount64,LoopBounds(last.Project?.Slots[current]).Start,LoopBounds(last.Project?.Slots[current]).End).Position;
    }
    private static (double Start,double End) LoopBounds(SlotSettings? settings)
    {
        if(settings?.MarkerLoop is not {Enabled:true} loop)return default;
        var start=settings.Markers.FirstOrDefault(m=>m.Id==loop.StartId);var end=settings.Markers.FirstOrDefault(m=>m.Id==loop.EndId);
        return start!=null && end!=null?(start.Seconds,end.Seconds):default;
    }
    private MarkerWindow? OpenMarkers(double position,string? id=null)
    {
        if(last?.Project==null || (uint)current>=last.Slots.Length || last.Slots[current].Loading || last.Slots[current].Duration<=0){Warn("Attendi che la traccia sia pronta.");return null;}
        if(markerWindow!=null){markerWindow.Close();markerWindow=null;}
        int slot=current,revision=last.Slots[current].Revision;string instance=last.Instance;
        var window=new MarkerWindow(slot,last,position,id,SendReply,marker=>SeekMarker(slot,revision,instance,marker)){Owner=this};
        markerWindow=window;window.Closed+=(_,_)=>{if(ReferenceEquals(markerWindow,window))markerWindow=null;};window.Show();return window;
    }
    private async Task SeekMarker(int slot,int revision,string instance,AudioMarker marker)
    {
        if(last?.Instance!=instance || (uint)slot>=last.Slots.Length || last.Slots[slot].Revision!=revision){Warn("La traccia è cambiata. Riapri la gestione marker.");return;}
        var currentMarker=last.Project?.Slots[slot].Markers.FirstOrDefault(m=>m.Id==marker.Id);
        if(currentMarker==null || last.Slots[slot].Duration<=0)return;
        if(currentMarker.Seconds>last.Slots[slot].Duration){Warn("Il marker deve essere dentro la durata della traccia.");return;}
        Select(slot);markerFeedback.Reset();await Send(new("seek",slot,Value:(float)(currentMarker.Seconds/last.Slots[slot].Duration),Origin:"mouse"));
    }
    private void UpdateMarkerState(StateInfo state)
    {
        markerWindow?.Update(state);
        var slot=state.Slots[current];var markers=state.Project?.Slots[current].Markers??[];
        waveform.SetMarkers(markers,slot.Duration);
        if(addMarkerButton!=null)addMarkerButton.IsEnabled=!state.ShowLocked && !slot.Loading && slot.Duration>0 && markers.Length<AudioMarkers.Maximum;
        if(manageMarkersButton!=null)manageMarkersButton.IsEnabled=!slot.Loading && slot.Duration>0;
    }
    private void RenderMarkerFeedback(long now,long engineTick)
    {
        if(last?.Project==null || playerChrome==null || playerHaloWide==null || playerHaloInner==null)return;
        var slot=last.Slots[current];bool playing=slot.State=="Playing" && last.AudioError==null && now-engineTick<=500;
        markerFeedback.Observe(last.Instance+":"+current+":"+slot.Revision,last.Project.Slots[current].Markers,slot.Position,playing,slot.Seeking,engineTick,now,slot.State=="Ended",slot.LoopPass,MarkerLoops.End(last.Project.Slots[current].MarkerLoop,last.Project.Slots[current].Markers),last.Project.Slots[current].MarkerLoop is {Enabled:true} configured?last.Project.Slots[current].Markers.FirstOrDefault(m=>m.Id==configured.StartId):null);
        bool illuminate=markerFeedback.Illuminated(now,SystemParameters.ClientAreaAnimation);
        bool markerCue=markerFeedback.InProgress(now);
        var bounds=LoopBounds(last.Project.Slots[current]);
        bool looping=playing && bounds.End>bounds.Start;
        bool loopHigh=!SystemParameters.ClientAreaAnimation || now%1600<800;
        string effect=(playing?"playing":"idle")+":"+(markerCue?(illuminate?markerFeedback.Active!.Color:"cue-off"):looping?(loopHigh?"loop-high":"loop-low"):"");if(effect==markerEffectKey)return;markerEffectKey=effect;
        Brush accent=markerCue?playerNeutral:looping && !loopHigh?playerRedDim:playing?playerRed:playerNeutral;
        if(illuminate)
        {
            string color=markerFeedback.Active!.Color;if(cachedMarkerColor!=color){cachedMarkerColor=color;cachedMarkerBrush=Brush(color);}accent=cachedMarkerBrush!;
        }
        playerChrome.BorderBrush=playerHaloWide.BorderBrush=playerHaloInner.BorderBrush=accent;
        playerHaloWide.Opacity=markerCue?(illuminate?.7:0):looping && loopHigh?.4:playing?.12:0;playerHaloInner.Opacity=markerCue?(illuminate?1:0):looping && loopHigh?.75:playing?.22:0;
        selectedTime.Foreground=illuminate?accent:Brush("#EDF4FC");durationTime.Foreground=illuminate?accent:Brush("#9BAFC5");remainingTime.Foreground=illuminate?accent:Brush("#FFCC80");
    }
}
