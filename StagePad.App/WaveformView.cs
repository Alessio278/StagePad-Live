using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using StagePad.Core;
namespace StagePad.App;
internal sealed class WaveformView : FrameworkElement
{
    private float[] peaks=[];
    private AudioMarker[] markers=[];
    private sealed record MarkerInk(AudioMarker Marker,double X,SolidColorBrush Brush,Pen Pen,FormattedText? Text,double LabelX);
    private MarkerInk[] markerInk=[];
    private double duration,position;
    private StreamGeometry? geometry;
    private static readonly SolidColorBrush Ink=Frozen("#75B9B1"),BackgroundInk=Frozen("#101B28");
    private static readonly Pen CursorPen=new(Brushes.White,1.5);
    private static SolidColorBrush Frozen(string hex){var b=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));b.Freeze();return b;}
    static WaveformView()=>CursorPen.Freeze();
    public event Action<double>? SeekRequested,AddMarkerRequested;
    public event Action<string>? EditMarkerRequested;
    private readonly DispatcherTimer hoverTimer=new(){Interval=TimeSpan.FromMilliseconds(650)};
    private readonly Popup seekHint=new(){AllowsTransparency=true,StaysOpen=true,IsHitTestVisible=false,PopupAnimation=PopupAnimation.Fade,Placement=PlacementMode.Relative};
    private readonly TextBlock hintText=new(){Foreground=Brushes.White,FontFamily=new FontFamily("Segoe UI"),FontSize=12,MaxWidth=360,TextWrapping=TextWrapping.Wrap};
    private Point hoverPoint;
    private const double MarkerBand=23;
    public WaveformView()
    {
        Cursor=System.Windows.Input.Cursors.Hand;ClipToBounds=true;seekHint.PlacementTarget=this;L.Preserve(hintText);
        seekHint.Child=new Border{Background=Frozen("#1B2B3D"),BorderBrush=Frozen("#3C566F"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Padding=new Thickness(12,8,12,8),Child=hintText};
        hoverTimer.Tick+=(_,_)=>
        {
            hoverTimer.Stop();if(!IsMouseOver || peaks.Length==0)return;
            var marker=HitMarker(hoverPoint.X);
            hintText.Text=marker==null?L.T("Fai doppio clic per saltare a questa posizione"):marker.Label+" · "+AudioMarkers.Format(marker.Seconds)+"\n"+L.T("Doppio clic: vai al marker · clic destro: modifica");
            seekHint.HorizontalOffset=Math.Min(hoverPoint.X,Math.Max(0,ActualWidth-180));seekHint.VerticalOffset=ActualHeight+8;seekHint.IsOpen=true;
        };
        MouseMove+=(_,e)=>{var next=e.GetPosition(this);if(next==hoverPoint)return;hoverPoint=next;HideHint();if(peaks.Length>0)hoverTimer.Start();};
        MouseEnter+=(_,e)=>{hoverPoint=e.GetPosition(this);if(peaks.Length>0)hoverTimer.Start();};
        MouseLeave+=(_,_)=>HideHint();Unloaded+=(_,_)=>HideHint();IsVisibleChanged+=(_,_)=>{if(!IsVisible)HideHint();};
        MouseRightButtonUp+=(_,e)=>
        {
            HideHint();if(duration<=0 || peaks.Length==0)return;double x=e.GetPosition(this).X;var marker=HitMarker(x);
            var menu=new ContextMenu();var item=new MenuItem{Header=L.T(marker==null?"Aggiungi marker qui":"Modifica marker")};
            item.Click+=(_,_)=>{if(marker==null)AddMarkerRequested?.Invoke(Math.Clamp(x/Math.Max(1,ActualWidth),0,1));else EditMarkerRequested?.Invoke(marker.Id);};menu.Items.Add(item);menu.IsOpen=true;e.Handled=true;
        };
    }
    private AudioMarker? HitMarker(double x)=>markerInk.Where(m=>Math.Abs(m.X-x)<=9).OrderBy(m=>Math.Abs(m.X-x)).Select(m=>m.Marker).FirstOrDefault();
    private void HideHint(){hoverTimer.Stop();seekHint.IsOpen=false;}
    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        HideHint();base.OnMouseLeftButtonDown(e);
        if(e.ClickCount==2 && ActualWidth>0 && peaks.Length>0)
        {
            var marker=HitMarker(e.GetPosition(this).X);
            e.Handled=true;SeekRequested?.Invoke(marker!=null && duration>0?marker.Seconds/duration:Math.Clamp(e.GetPosition(this).X/ActualWidth,0,1));
        }
    }
    public void SetPeaks(float[] value){HideHint();peaks=value;geometry=null;InvalidateVisual();}
    public void SetMarkers(AudioMarker[] value,double trackDuration)
    {
        if(duration==trackDuration && AudioMarkers.Same(markers,value,true))return;
        duration=trackDuration;markers=value;BuildMarkerInk();InvalidateVisual();
    }
    private void BuildMarkerInk()
    {
        var drawings=new List<MarkerInk>();double nextLabel=0;
        foreach(var marker in markers)
        {
            if(duration<=0 || marker.Seconds>duration)continue;
            double x=Math.Clamp(marker.Seconds/duration,0,1)*ActualWidth;
            var brush=Frozen(marker.Color);var pen=new Pen(brush,1);pen.Freeze();
            var text=new FormattedText(marker.Label,System.Globalization.CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            double labelX=Math.Clamp(x+5,0,Math.Max(0,ActualWidth-Math.Min(text.Width,120)));FormattedText? caption=null;
            if(labelX>=nextLabel && ActualWidth-labelX>12)
            {text.MaxTextWidth=Math.Max(1,Math.Min(120,ActualWidth-labelX));text.Trimming=TextTrimming.CharacterEllipsis;caption=text;nextLabel=labelX+Math.Min(text.Width,120)+10;}
            drawings.Add(new(marker,x,brush,pen,caption,labelX));
        }
        markerInk=drawings.ToArray();
    }
    protected override void OnDpiChanged(DpiScale oldDpi,DpiScale newDpi){base.OnDpiChanged(oldDpi,newDpi);BuildMarkerInk();InvalidateVisual();}
    public void SetPosition(double value){value=Math.Clamp(value,0,1);if(Math.Abs(position-value)<.000001)return;position=value;InvalidateVisual();}
    protected override void OnRenderSizeChanged(SizeChangedInfo info){base.OnRenderSizeChanged(info);geometry=null;BuildMarkerInk();InvalidateVisual();}
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(BackgroundInk,null,new Rect(RenderSize));
        if(ActualWidth<=0 || ActualHeight<=MarkerBand || peaks.Length==0)return;
        double top=MarkerBand,center=top+Math.Max(0,ActualHeight-top)/2,amplitude=Math.Max(0,ActualHeight-top)/2;
        if(peaks.Length>0 && geometry==null)
        {
            var g=new StreamGeometry();using(var c=g.Open())
            {
                c.BeginFigure(new Point(0,center),true,true);
                if(peaks.Length==1)c.LineTo(new Point(0,center-peaks[0]*amplitude),true,false);
                if(peaks.Length==1)c.LineTo(new Point(ActualWidth,center-peaks[0]*amplitude),true,false);
                for(int i=0;i<peaks.Length && peaks.Length>1;i++)c.LineTo(new Point(i*ActualWidth/Math.Max(1,peaks.Length-1),center-peaks[i]*amplitude),true,false);
                if(peaks.Length==1)c.LineTo(new Point(ActualWidth,center+peaks[0]*amplitude),true,false);
                if(peaks.Length==1)c.LineTo(new Point(0,center+peaks[0]*amplitude),true,false);
                for(int i=peaks.Length-1;i>=0 && peaks.Length>1;i--)c.LineTo(new Point(i*ActualWidth/Math.Max(1,peaks.Length-1),center+peaks[i]*amplitude),true,false);
            }
            g.Freeze();geometry=g;
        }
        if(geometry!=null)dc.DrawGeometry(Ink,null,geometry);
        foreach(var item in markerInk)
        {
            dc.DrawLine(item.Pen,new Point(item.X,MarkerBand),new Point(item.X,ActualHeight));
            dc.DrawRoundedRectangle(item.Brush,null,new Rect(Math.Clamp(item.X-4,0,Math.Max(0,ActualWidth-8)),15,8,7),2,2);
            if(item.Text!=null)dc.DrawText(item.Text,new Point(item.LabelX,0));
        }
        double cursor=position*ActualWidth;dc.DrawLine(CursorPen,new Point(cursor,MarkerBand),new Point(cursor,ActualHeight));
    }
}
