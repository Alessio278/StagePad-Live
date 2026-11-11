using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StagePad.App;
internal sealed class ColorWheel : FrameworkElement
{
    private static readonly BitmapSource WheelImage=BuildImage();
    private double hue,saturation,value=1;
    public event Action<double,double>? Changed;
    public ColorWheel()
    { Width=236;Height=236;Focusable=true;Cursor=Cursors.Cross;ToolTip="Trascina per scegliere tonalità e saturazione. Frecce per regolare.";System.Windows.Automation.AutomationProperties.SetName(this,"Ruota colore: tonalità e saturazione"); }
    public void Set(double h,double s,double v){hue=h;saturation=s;value=v;InvalidateVisual();}
    private double Radius=>Math.Max(1,Math.Min(ActualWidth,ActualHeight)/2-8);
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);var center=new Point(ActualWidth/2,ActualHeight/2);double radius=Radius;
        dc.DrawImage(WheelImage,new Rect(center.X-radius,center.Y-radius,radius*2,radius*2));
        if(value<1)dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)Math.Round((1-value)*255),0,0,0)),null,center,radius,radius);
        double angle=hue*Math.PI/180;
        var point=new Point(center.X+Math.Cos(angle)*saturation*radius,center.Y-Math.Sin(angle)*saturation*radius);
        dc.DrawEllipse(null,new Pen(Brushes.Black,5),point,6,6);dc.DrawEllipse(null,new Pen(Brushes.White,2.5),point,6,6);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){base.OnMouseLeftButtonDown(e);Focus();CaptureMouse();Pick(e.GetPosition(this));e.Handled=true;}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(IsMouseCaptured && e.LeftButton==MouseButtonState.Pressed)Pick(e.GetPosition(this));}
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){base.OnMouseLeftButtonUp(e);if(IsMouseCaptured){Pick(e.GetPosition(this));ReleaseMouseCapture();e.Handled=true;}}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down))return;
        hue=(hue+(e.Key==Key.Right?2:e.Key==Key.Left?-2:0)+360)%360;
        saturation=Math.Clamp(saturation+(e.Key==Key.Up?.02:e.Key==Key.Down?-.02:0),0,1);
        Changed?.Invoke(hue,saturation);InvalidateVisual();e.Handled=true;
    }
    private void Pick(Point point)
    {
        double x=point.X-ActualWidth/2,y=ActualHeight/2-point.Y;
        hue=(Math.Atan2(y,x)*180/Math.PI+360)%360;saturation=Math.Clamp(Math.Sqrt(x*x+y*y)/Radius,0,1);
        Changed?.Invoke(hue,saturation);InvalidateVisual();
    }
    private static BitmapSource BuildImage()
    {
        const int size=320;var pixels=new byte[size*size*4];double radius=(size-1)/2d;
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            double dx=(x-radius)/radius,dy=(radius-y)/radius,distance=Math.Sqrt(dx*dx+dy*dy);
            if(distance>1)continue;
            var rgb=ColorMath.ToRgb(Math.Atan2(dy,dx)*180/Math.PI,distance,1);int offset=(y*size+x)*4;
            pixels[offset]=rgb.B;pixels[offset+1]=rgb.G;pixels[offset+2]=rgb.R;pixels[offset+3]=255;
        }
        var bitmap=BitmapSource.Create(size,size,96,96,PixelFormats.Bgra32,null,pixels,size*4);bitmap.Freeze();return bitmap;
    }
}
