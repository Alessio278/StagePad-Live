using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StagePad.App;

// A finite viewport: the full text never participates in horizontal layout.
internal sealed class ScrollingTitle : FrameworkElement
{
    private readonly TextBlock source;
    private FormattedText? formatted;
    private string? text;
    private double fontSize, pixelsPerDip, started, offset;
    public ScrollingTitle(TextBlock title)
    {
        source=title;ClipToBounds=true;IsHitTestVisible=false;
        Loaded+=(_,_)=>Restart();
        IsVisibleChanged+=(_,_)=>Restart();
    }
    private static double Now=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    private void Restart(){started=Now;offset=0;InvalidateVisual();}
    private void Prepare()
    {
        double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if(formatted!=null && text==source.Text && fontSize==source.FontSize && dpi==pixelsPerDip)return;
        text=source.Text;fontSize=source.FontSize;pixelsPerDip=dpi;
        formatted=new FormattedText(text,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,
            new Typeface(source.FontFamily,source.FontStyle,source.FontWeight,source.FontStretch),fontSize,Brushes.White,dpi);
        Restart();InvalidateMeasure();
    }
    public void Tick()
    {
        if(!IsVisible || !IsLoaded)return;
        Prepare();
        double next=TitleScrollMotion.Offset(Now-started,formatted!.WidthIncludingTrailingWhitespace,ActualWidth);
        if(Math.Abs(next-offset)<.1)return;
        offset=next;InvalidateVisual();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Prepare();
        return new Size(0,formatted!.Height+4);
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);Restart();
    }
    protected override void OnRender(DrawingContext dc)
    {
        Prepare();
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        dc.DrawText(formatted!,new Point(offset,Math.Max(0,(ActualHeight-formatted!.Height)/2)));
        dc.Pop();
    }
}
