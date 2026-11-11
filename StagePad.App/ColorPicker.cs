using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace StagePad.App;
internal sealed class ColorPicker : StackPanel
{
    public event Action<string>? Changed;
    private readonly ColorWheel wheel=new();
    private readonly Slider brightness=new(){Minimum=0,Maximum=1,Value=1,SmallChange=.01,LargeChange=.1,Width=292};
    private readonly Border preview=new(){Width=30,Height=24,CornerRadius=new CornerRadius(6),Margin=new Thickness(0,0,12,0)};
    private readonly TextBlock label=new(){Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center};
    private readonly TextBlock percentage=new(){Foreground=Brushes.White,FontSize=12};
    private readonly Popup popup;
    private readonly List<(Button Button,PaletteColor Color)> swatches=[];
    private double hue,saturation,value;
    private bool syncing;
    public ColorPicker(string initial)
    {
        Orientation=Orientation.Vertical;
        var content=new DockPanel();DockPanel.SetDock(preview,Dock.Left);content.Children.Add(preview);content.Children.Add(label);label.TextTrimming=TextTrimming.CharacterEllipsis;
        var open=new Button { Content=content,Padding=new Thickness(12,8,12,8),HorizontalContentAlignment=HorizontalAlignment.Stretch,ToolTip="Apri la ruota colore" };Children.Add(open);
        var body=new StackPanel();
        body.Children.Add(new TextBlock { Text="Scegli il colore",Foreground=Brushes.White,FontSize=18,FontWeight=FontWeights.SemiBold,Margin=new Thickness(3,0,0,4) });
        body.Children.Add(wheel);
        var row=new DockPanel { Margin=new Thickness(4,6,4,2) };DockPanel.SetDock(percentage,Dock.Right);row.Children.Add(percentage);row.Children.Add(new TextBlock { Text="Luminosità",Foreground=Brushes.White,FontSize=12 });body.Children.Add(row);body.Children.Add(brightness);
        body.Children.Add(new TextBlock { Text="COLORI RAPIDI",Foreground=new SolidColorBrush(Color.FromRgb(145,166,189)),FontSize=10,Margin=new Thickness(4,16,0,5) });
        var palette=new UniformGrid { Columns=10,Rows=2,Width=300,Height=62 };
        foreach(var color in SlotPalette.Colors)
        {
            var button=new Button { Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.Hex)),Margin=new Thickness(2),Padding=new Thickness(0),ToolTip=color.Name,Foreground=Brushes.White };
            System.Windows.Automation.AutomationProperties.SetName(button,color.Name);swatches.Add((button,color));palette.Children.Add(button);
            button.Click+=(_,_)=>SetColor(color.Hex,true);
        }
        body.Children.Add(palette);
        var done=new Button { Content="Fatto",Margin=new Thickness(0,14,0,0),Padding=new Thickness(10,7,10,7) };body.Children.Add(done);
        popup=new Popup { PlacementTarget=open,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,Child=new Border { Background=new SolidColorBrush(Color.FromRgb(18,29,43)),BorderBrush=new SolidColorBrush(Color.FromRgb(53,76,99)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Padding=new Thickness(18),Child=body } };
        Children.Add(popup);
        open.Click+=(_,_)=>popup.IsOpen=!popup.IsOpen;done.Click+=(_,_)=>popup.IsOpen=false;
        Unloaded+=(_,_)=>popup.IsOpen=false;
        wheel.Changed+=(h,s)=>{hue=h;saturation=s;Publish();};
        brightness.ValueChanged+=(_,_)=>{if(syncing)return;value=brightness.Value;Publish();};
        SetColor(initial,false);
        L.Apply(this);L.Apply(popup);
        void TranslateColor()=>Refresh($"#{ColorMath.ToRgb(hue,saturation,value).R:X2}{ColorMath.ToRgb(hue,saturation,value).G:X2}{ColorMath.ToRgb(hue,saturation,value).B:X2}");
        Loaded+=(_,_)=>L.Changed+=TranslateColor;
        Unloaded+=(_,_)=>L.Changed-=TranslateColor;
    }
    public void ResetToDefault()=>SetColor(new StagePad.Core.SlotSettings().Color,true);
    private void SetColor(string hex,bool notify)
    {
        var color=(Color)ColorConverter.ConvertFromString(hex);(hue,saturation,value)=ColorMath.FromRgb(color.R,color.G,color.B);
        syncing=true;brightness.Value=value;syncing=false;Refresh(hex);if(notify)Changed?.Invoke(hex);
    }
    private void Publish()
    { var c=ColorMath.ToRgb(hue,saturation,value);string hex=$"#{c.R:X2}{c.G:X2}{c.B:X2}";Refresh(hex);Changed?.Invoke(hex); }
    private void Refresh(string hex)
    {
        wheel.Set(hue,saturation,value);preview.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        label.Text=L.T(SlotPalette.Colors.FirstOrDefault(c=>string.Equals(c.Hex,hex,StringComparison.OrdinalIgnoreCase))?.Name??"Personalizzato")+"   ▾";
        percentage.Text=$"{value*100:0}%";
        foreach(var item in swatches){bool active=string.Equals(item.Color.Hex,hex,StringComparison.OrdinalIgnoreCase);item.Button.BorderBrush=active?Brushes.White:Brushes.Transparent;item.Button.BorderThickness=new Thickness(active?2:0);item.Button.Content=active?"✓":"";}
    }
}
