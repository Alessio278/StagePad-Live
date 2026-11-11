using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace StagePad.App;
internal sealed partial class MainWindow
{
    private void QueueFollowTriggeredSlot(int slot,long serial,string engine)
    {
        if(!liveView || !L.FollowTriggeredSlot)return;
        // Wait for current grid/visibility changes to be arranged; a newer trigger wins.
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>
        {
            if(!liveView || !L.FollowTriggeredSlot || last?.Instance!=engine || last.TriggerSerial!=serial ||
                (uint)slot>=last.Slots.Length)return;
            var card=liveCards[slot];
            if(!card.IsVisible || card.ActualWidth<=0 || card.ActualHeight<=0)return;
            var origin=card.TranslatePoint(new Point(),liveGrid);
            double horizontal=FollowScroll.Offset(liveScroll.HorizontalOffset,liveScroll.ViewportWidth,liveScroll.ExtentWidth,origin.X,card.ActualWidth);
            double vertical=FollowScroll.Offset(liveScroll.VerticalOffset,liveScroll.ViewportHeight,liveScroll.ExtentHeight,origin.Y,card.ActualHeight);
            if(Math.Abs(horizontal-liveScroll.HorizontalOffset)>.5)liveScroll.ScrollToHorizontalOffset(horizontal);
            if(Math.Abs(vertical-liveScroll.VerticalOffset)>.5)liveScroll.ScrollToVerticalOffset(vertical);
        }));
    }
    private void EnableHorizontalTrackpad()
    {
        HwndSource? source=null;
        SourceInitialized+=(_,_)=>
        {
            source=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(HorizontalWheelMessage);
        };
        Closed+=(_,_)=>{source?.RemoveHook(HorizontalWheelMessage);source=null;};
        foreach(var scroll in new[]{setupScroll,liveScroll})
        {
            scroll.PanningMode=PanningMode.Both;
            scroll.PreviewMouseWheel+=(_,e)=>
            {
                if((Keyboard.Modifiers&ModifierKeys.Shift)==0 || scroll.ScrollableWidth<=0)return;
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset-HorizontalWheelMotion.Pixels(e.Delta));
                e.Handled=true;
            };
        }
    }
    private IntPtr HorizontalWheelMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        const int WmMouseHWheel=0x020E;
        if(message!=WmMouseHWheel)return IntPtr.Zero;
        // Signed screen coordinates support monitors to the left/above the primary.
        long packed=lParam.ToInt64();
        var point=PointFromScreen(new Point(unchecked((short)packed),unchecked((short)(packed>>16))));
        var scroll=liveView?liveScroll:setupScroll;
        if(!scroll.IsVisible || scroll.ScrollableWidth<=0)return IntPtr.Zero;
        var local=TranslatePoint(point,scroll);
        if(!new Rect(scroll.RenderSize).Contains(local))return IntPtr.Zero;
        int delta=unchecked((short)(wParam.ToInt64()>>16));
        scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset+HorizontalWheelMotion.Pixels(delta));
        handled=true;
        return IntPtr.Zero;
    }
}
