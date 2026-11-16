using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace StagePad.App;
internal static class ScrollSpeed
{
    private sealed class Remainder {public int Delta;}
    private static readonly ConditionalWeakTable<ScrollViewer,Remainder> remainders=new();
    private static bool installed;
    public static void Install()
    {
        if(installed)return;installed=true;
        EventManager.RegisterClassHandler(typeof(ScrollViewer),UIElement.PreviewMouseWheelEvent,new MouseWheelEventHandler(Wheel));
    }
    private static DependencyObject? Parent(DependencyObject node)=>node is Visual or System.Windows.Media.Media3D.Visual3D?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);
    private static void Wheel(object sender,MouseWheelEventArgs e)
    {
        if(e.Handled)return;
        bool horizontal=(Keyboard.Modifiers&ModifierKeys.Shift)!=0;
        for(var node=e.OriginalSource as DependencyObject;node!=null;node=Parent(node))
        {
            if(node is not ScrollViewer scroll || (horizontal?scroll.ScrollableWidth:scroll.ScrollableHeight)<=0)continue;
            double offset=horizontal?scroll.HorizontalOffset:scroll.VerticalOffset,max=horizontal?scroll.ScrollableWidth:scroll.ScrollableHeight;
            if(e.Delta>0 && offset<=0 || e.Delta<0 && offset>=max)continue;
            if(scroll.CanContentScroll)
            {
                var state=remainders.GetOrCreateValue(scroll);state.Delta+=e.Delta;
                while(Math.Abs(state.Delta)>=120)
                {
                    bool up=state.Delta>0;state.Delta+=up?-120:120;
                    if(horizontal){if(up)scroll.LineLeft();else scroll.LineRight();}
                    else{if(up)scroll.LineUp();else scroll.LineDown();}
                }
            }
            else
            {
                double next=offset-HorizontalWheelMotion.Pixels(e.Delta);
                if(horizontal)scroll.ScrollToHorizontalOffset(next);else scroll.ScrollToVerticalOffset(next);
            }
            e.Handled=true;return;
        }
    }
}
