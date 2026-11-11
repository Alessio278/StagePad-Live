using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace StagePad.App;
internal sealed class WaveformView : FrameworkElement
{
    private float[] peaks = [];
    private double position;
    private StreamGeometry? geometry;
    private static readonly SolidColorBrush Ink = Frozen("#75B9B1"), BackgroundInk = Frozen("#101B28");
    private static readonly Pen CursorPen = new(Brushes.White, 1.5);
    private static SolidColorBrush Frozen(string hex) { var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); return b; }
    static WaveformView() => CursorPen.Freeze();
    public event Action<double>? SeekRequested;
    private readonly DispatcherTimer hoverTimer=new(){Interval=TimeSpan.FromMilliseconds(650)};
    private readonly Popup seekHint=new(){AllowsTransparency=true,StaysOpen=true,IsHitTestVisible=false,PopupAnimation=PopupAnimation.Fade,Placement=PlacementMode.Relative};
    private Point hoverPoint;
    public WaveformView()
    {
        Cursor=System.Windows.Input.Cursors.Hand;
        seekHint.PlacementTarget=this;
        seekHint.Child=new Border { Background=Frozen("#1B2B3D"),BorderBrush=Frozen("#3C566F"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Padding=new Thickness(12,8,12,8),
            Child=new TextBlock { Text="Fai doppio clic per saltare a questa posizione",Foreground=Brushes.White,FontFamily=new FontFamily("Segoe UI"),FontSize=12 } };
        L.Apply(seekHint);
        hoverTimer.Tick+=(_,_)=>
        {
            hoverTimer.Stop();
            if(!IsMouseOver || peaks.Length==0)return;
            seekHint.HorizontalOffset=hoverPoint.X;seekHint.VerticalOffset=ActualHeight+8;seekHint.IsOpen=true;
        };
        MouseMove+=(_,e)=>{var next=e.GetPosition(this);if(next==hoverPoint)return;hoverPoint=next;HideHint();if(peaks.Length>0)hoverTimer.Start();};
        MouseEnter+=(_,e)=>{hoverPoint=e.GetPosition(this);if(peaks.Length>0)hoverTimer.Start();};
        MouseLeave+=(_,_)=>HideHint();Unloaded+=(_,_)=>HideHint();IsVisibleChanged+=(_,_)=>{if(!IsVisible)HideHint();};
    }
    private void HideHint(){hoverTimer.Stop();seekHint.IsOpen=false;}
    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        HideHint();
        base.OnMouseLeftButtonDown(e);
        if(e.ClickCount==2 && ActualWidth>0 && peaks.Length>0)
        { e.Handled=true;SeekRequested?.Invoke(Math.Clamp(e.GetPosition(this).X/ActualWidth,0,1)); }
    }
    public void SetPeaks(float[] value) { HideHint(); peaks = value; geometry = null; InvalidateVisual(); }
    public void SetPosition(double value) { value = Math.Clamp(value,0,1); if(Math.Abs(position-value)<.000001)return; position=value; InvalidateVisual(); }
    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); geometry = null; }
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(BackgroundInk, null, new Rect(RenderSize));
        if (peaks.Length > 0 && geometry == null)
        {
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                double center = ActualHeight / 2;
                c.BeginFigure(new Point(0, center), true, true);
                for (int i = 0; i < peaks.Length; i++) c.LineTo(new Point(i * ActualWidth / Math.Max(1, peaks.Length - 1), center - peaks[i] * center), true, false);
                for (int i = peaks.Length - 1; i >= 0; i--) c.LineTo(new Point(i * ActualWidth / Math.Max(1, peaks.Length - 1), center + peaks[i] * center), true, false);
            }
            g.Freeze(); geometry = g;
        }
        if (geometry != null) dc.DrawGeometry(Ink, null, geometry);
        double x = position * ActualWidth;
        dc.DrawLine(CursorPen, new Point(x, 0), new Point(x, ActualHeight));
    }
}
