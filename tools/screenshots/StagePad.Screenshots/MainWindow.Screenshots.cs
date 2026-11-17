using System.Windows;
using System.Windows.Media;
namespace StagePad.App;
internal sealed partial class MainWindow
{
    internal void PrepareScreenshotWindow()
    {
        timer.Stop();IsHitTestVisible=false;Focusable=false;
        Loaded+=(_,_)=>CompositionTarget.Rendering-=OnUiFrame;
    }
    internal void PresentScreenshot(StateInfo state,bool live,int selectedSlot,float[] peaks)
    {
        last=state;current=selectedSlot;markerFeedback.Reset();markerEffectKey=null;
        SetView(live);viewHost.BeginAnimation(OpacityProperty,null);viewHost.Opacity=1;
        Show(state);waveform.SetPeaks(peaks);UpdateMarkerState(state);RenderPlayer();
    }
    internal FrameworkElement ScreenshotPlayer=>playerChrome??throw new InvalidOperationException("Player unavailable.");
    internal async Task ScreenshotProjects()
    {
        ShowProjectBrowser(false);
        if(projectBrowser is {} browser){await browser.Refresh();if(last!=null)browser.Update(last);}
    }
}
