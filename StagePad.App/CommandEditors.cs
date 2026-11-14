using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StagePad.Core;
namespace StagePad.App;
internal static class CommandForm
{
    public static readonly Brush Accent=new SolidColorBrush(Color.FromRgb(117,226,193));
    public static FrameworkElement Row(string label,FrameworkElement editor)
    {
        var grid=new Grid {Margin=new Thickness(0,4,0,4)};grid.ColumnDefinitions.Add(new(){Width=new GridLength(.4,GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(.6,GridUnitType.Star)});
        grid.Children.Add(new TextBlock {Text=label,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)});
        Grid.SetColumn(editor,1);grid.Children.Add(editor);return grid;
    }
    public static TextBox Text(object value,int length=150)=>new(){Text=Convert.ToString(value,CultureInfo.InvariantCulture),MaxLength=length,Padding=new Thickness(7,5,7,5)};
    public static Button Button(string label,Action action)
    {var button=new Button{Content=label,Margin=new Thickness(0,6,8,6),Padding=new Thickness(9,5,9,5)};L.Bind(button,ContentControl.ContentProperty,label);button.Click+=(_,_)=>action();return button;}
    private sealed record Option<T>(T Key,string Label){public string Value=>L.T(Label);}
    public static ComboBox Choice<T>(T selected,Func<T,string>? caption=null)where T:struct,Enum=>new(){ItemsSource=Enum.GetValues<T>().Select(v=>new Option<T>(v,caption?.Invoke(v)??v.ToString())).ToArray(),DisplayMemberPath="Value",SelectedValuePath="Key",SelectedValue=selected};
    public static void Tools(Panel body,Action<int>? move,Func<Task<Reply>>? test)
    {
        var row=new WrapPanel();body.Children.Add(row);
        if(move!=null){row.Children.Add(Button("↑",()=>move(-1)));row.Children.Add(Button("↓",()=>move(1)));}
        if(test==null)return;
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray};L.Preserve(status);
        var button=new Button{Content="Test ora",Margin=new Thickness(0,6,0,6)};L.Bind(button,ContentControl.ContentProperty,"Test ora");row.Children.Add(button);body.Children.Add(status);
        button.Click+=async(_,_)=>{button.IsEnabled=false;try{var reply=await test();status.Text=reply.Ok?L.T("Comando trasmesso."):L.Message(reply.Error??"Comando non riuscito.");}catch(Exception ex){status.Text=L.Message(ex.Message);}finally{button.IsEnabled=true;}};
    }
    public static Border Card(UIElement child)=>new(){Child=child,Background=new SolidColorBrush(Color.FromRgb(18,29,43)),BorderBrush=new SolidColorBrush(Color.FromRgb(41,60,81)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Padding=new Thickness(12),Margin=new Thickness(0,6,0,6)};
}
internal sealed class MidiCommandEditor : ContentControl
{
    private readonly MidiSettings original;
    private readonly bool markerTrigger;
    private readonly StackPanel body=new();
    private readonly Dictionary<string,TextBox> texts=[];
    private readonly Dictionary<string,ComboBox> choices=[];
    private readonly List<(FrameworkElement Row,MidiKind[] Kinds)> rows=[];
    public MidiCommandEditor(MidiSettings value,Action remove,Action<int>? move=null,Func<MidiSettings,Task<Reply>>? test=null,bool markerTrigger=false)
    {
        this.markerTrigger=markerTrigger;original=value;Content=CommandForm.Card(body);
        body.Children.Add(CommandForm.Button("Rimuovi comando MIDI",remove));
        CommandForm.Tools(body,move,test==null?null:()=>test(Read()));
        if(!markerTrigger)Text("Attesa prima (ms)",value.DelayMs,Enum.GetValues<MidiKind>());
        Choice("Protocollo",value.Kind,null,v=>v switch{MidiKind.ControlChange=>"Control Change",MidiKind.ProgramChange=>"Program Change",_=>v.ToString()});
        Text("Canale OUT 1-16",value.Channel,[MidiKind.Note,MidiKind.ControlChange,MidiKind.ProgramChange]);
        Text("Nota OUT 0-127",value.Note,[MidiKind.Note]);Text("Velocity 1-127",value.Velocity,[MidiKind.Note]);
        if(!markerTrigger)Choice("Nota",value.Behavior,[MidiKind.Note],v=>v==NoteBehavior.Pulse?"Impulso con Note Off automatico":"Note On premendo / Off al rilascio");Text("Durata impulso ms",value.PulseMs,[MidiKind.Note]);
        Text("Controller CC 0-127",value.Controller,[MidiKind.ControlChange]);Text("Valore CC 0-127",value.ControlValue,[MidiKind.ControlChange]);Text("Programma 0-127",value.Program,[MidiKind.ProgramChange]);
        Choice("Comando MSC",value.Command,[MidiKind.MSC]);Text("Device ID 0-127",value.DeviceId,[MidiKind.MSC]);Text("Command format 1 / 2 / 127",value.CommandFormat,[MidiKind.MSC]);Text("Cue per GO",value.Cue,[MidiKind.MSC]);
        Choice("Indirizzamento",value.Address,[MidiKind.MSC],v=>v switch{MscAddress.Default=>"Predefinito / Default",MscAddress.ExecDotPage=>"Exec.Page",_=>"Exec Page (spazio)"});Text("Executor",value.Executor,[MidiKind.MSC]);
        choices["Protocollo"].SelectionChanged+=(_,_)=>Update();Update();L.Apply(this);
    }
    private void Text(string label,object value,MidiKind[] kinds)
    {var field=CommandForm.Text(value);texts[label]=field;var row=CommandForm.Row(label,field);body.Children.Add(row);rows.Add((row,kinds));}
    private void Choice<T>(string label,T value,MidiKind[]? kinds,Func<T,string>? captions=null)where T:struct,Enum
    {var combo=CommandForm.Choice(value,captions);choices[label]=combo;var row=CommandForm.Row(label,combo);body.Children.Add(row);if(kinds!=null)rows.Add((row,kinds));}
    private void Update(){var kind=(MidiKind)choices["Protocollo"].SelectedValue;foreach(var row in rows)row.Row.Visibility=row.Kinds.Contains(kind)?Visibility.Visible:Visibility.Collapsed;}
    private int N(string label)=>int.Parse(texts[label].Text,CultureInfo.InvariantCulture);
    public MidiSettings Read()
    {
        var kind=(MidiKind)choices["Protocollo"].SelectedValue;var value=original with{Kind=kind,DelayMs=markerTrigger?0:N("Attesa prima (ms)")};
        value=kind switch
        {
            MidiKind.Note=>value with{Channel=N("Canale OUT 1-16"),Note=N("Nota OUT 0-127"),Velocity=N("Velocity 1-127"),PulseMs=N("Durata impulso ms"),Behavior=markerTrigger?NoteBehavior.Pulse:(NoteBehavior)choices["Nota"].SelectedValue},
            MidiKind.ControlChange=>value with{Channel=N("Canale OUT 1-16"),Controller=N("Controller CC 0-127"),ControlValue=N("Valore CC 0-127")},
            MidiKind.ProgramChange=>value with{Channel=N("Canale OUT 1-16"),Program=N("Programma 0-127")},
            _=>value with{Command=(MscCommand)choices["Comando MSC"].SelectedValue,DeviceId=N("Device ID 0-127"),CommandFormat=N("Command format 1 / 2 / 127"),Cue=texts["Cue per GO"].Text.Trim().Replace(',','.'),Address=(MscAddress)choices["Indirizzamento"].SelectedValue,Executor=N("Executor"),Page=1}
        };
        Projects.ValidateMidi(value);return value;
    }
}
internal sealed class OscCommandEditor : ContentControl
{
    private readonly TextBox host,port,address,delay;
    private readonly StackPanel arguments=new();
    private readonly List<(FrameworkElement Row,ComboBox Type,TextBox Value)> values=[];
    private readonly Button add;
    public OscCommandEditor(OscCommand value,Action remove,Action<int>? move=null,Func<OscCommand,Task<Reply>>? test=null,bool markerTrigger=false)
    {
        var body=new StackPanel();Content=CommandForm.Card(body);
        body.Children.Add(CommandForm.Button("Rimuovi comando OSC",remove));
        CommandForm.Tools(body,move,test==null?null:()=>test(Read()));
        delay=CommandForm.Text(markerTrigger?0:value.DelayMs);if(!markerTrigger)body.Children.Add(CommandForm.Row("Attesa prima (ms)",delay));
        host=CommandForm.Text(value.Host,253);port=CommandForm.Text(value.Port,5);address=CommandForm.Text(value.Address,512);
        body.Children.Add(CommandForm.Row("Host / IP OSC",host));body.Children.Add(CommandForm.Row("Porta UDP",port));body.Children.Add(CommandForm.Row("Percorso OSC",address));
        body.Children.Add(arguments);add=CommandForm.Button("Aggiungi argomento",()=>AddArgument(new()));body.Children.Add(add);
        foreach(var arg in value.Arguments)AddArgument(arg);L.Apply(this);
    }
    private void AddArgument(OscArgument arg)
    {
        if(values.Count>=16)return;
        var row=new Grid{Margin=new Thickness(0,4,0,4)};row.ColumnDefinitions.Add(new(){Width=new GridLength(105)});row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var type=CommandForm.Choice(arg.Type,v=>v switch{OscArgumentType.Int=>"Intero (i)",OscArgumentType.Float=>"Decimale (f)",OscArgumentType.String=>"Testo (s)",OscArgumentType.True=>"Vero (T)",_=>"Falso (F)"});
        var text=CommandForm.Text(arg.Value,1024);text.Margin=new Thickness(6,0,6,0);
        var remove=CommandForm.Button("Rimuovi",()=>{values.RemoveAll(v=>ReferenceEquals(v.Row,row));arguments.Children.Remove(row);add.IsEnabled=true;});
        row.Children.Add(type);Grid.SetColumn(text,1);row.Children.Add(text);Grid.SetColumn(remove,2);row.Children.Add(remove);
        void Update()=>text.Visibility=(OscArgumentType)type.SelectedValue is OscArgumentType.True or OscArgumentType.False?Visibility.Collapsed:Visibility.Visible;
        type.SelectionChanged+=(_,_)=>Update();Update();values.Add((row,type,text));arguments.Children.Add(row);add.IsEnabled=values.Count<16;L.Apply(row);
    }
    public OscCommand Read()
    {
        var result=new OscCommand {DelayMs=int.Parse(delay.Text,CultureInfo.InvariantCulture),Host=host.Text.Trim(),Port=int.Parse(port.Text,CultureInfo.InvariantCulture),Address=address.Text.Trim(),Arguments=values.Select(v=>new OscArgument((OscArgumentType)v.Type.SelectedValue,(OscArgumentType)v.Type.SelectedValue is OscArgumentType.True or OscArgumentType.False?"":(OscArgumentType)v.Type.SelectedValue==OscArgumentType.Float?v.Value.Text.Trim().Replace(',','.'):v.Value.Text)).ToArray()};
        OscCodec.Validate(result);return result;
    }
}
