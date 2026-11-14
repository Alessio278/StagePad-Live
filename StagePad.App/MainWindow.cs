using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace StagePad.App;
internal sealed partial class MainWindow : Window
{
    private SettingsWindow? settingsWindow;
    private readonly string pipe;
    private readonly int parent;
    private readonly TextBlock showName = Text("Show: …", 11);
    private readonly TextBlock status = Text("● AUDIO · CONNESSIONE", 11);
    private readonly TextBlock warning = Text("", 12);
    private readonly ComboBox outputs = new() { MinWidth = 280, DisplayMemberPath = "Name", SelectedValuePath = "Id" };
    private readonly ComboBox latency = new() { Width = 90, ItemsSource = new[] { 20, 40, 80 }, SelectedItem = 40 };
    private readonly Slider master = new() { Minimum = 0, Maximum = 1, Value = 1, Width = 160 };
    private readonly Slider[] volumes = new Slider[StagePad.Core.Projects.MaxSlots];
    private readonly TextBlock[] titles = new TextBlock[StagePad.Core.Projects.MaxSlots], states = new TextBlock[StagePad.Core.Projects.MaxSlots], times = new TextBlock[StagePad.Core.Projects.MaxSlots];
    private readonly Border[] cards = new Border[StagePad.Core.Projects.MaxSlots];
    private readonly ScrollingTitle[] scrollingTitles = new ScrollingTitle[StagePad.Core.Projects.MaxSlots];
    private readonly string?[] padTintColors = new string?[StagePad.Core.Projects.MaxSlots];
    private readonly Border[] liveCards = new Border[StagePad.Core.Projects.MaxSlots];
    private readonly Border[] accents = new Border[StagePad.Core.Projects.MaxSlots], liveAccents = new Border[StagePad.Core.Projects.MaxSlots];
    private readonly TextBlock[] liveTitles = new TextBlock[StagePad.Core.Projects.MaxSlots], liveStates = new TextBlock[StagePad.Core.Projects.MaxSlots], liveTimes = new TextBlock[StagePad.Core.Projects.MaxSlots];
    private readonly ContentControl viewHost = new()
    { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly Grid liveGrid = new(), setupGrid = new();
    private readonly ScrollViewer liveScroll = new() { VerticalScrollBarVisibility=ScrollBarVisibility.Auto, HorizontalScrollBarVisibility=ScrollBarVisibility.Auto };
    private readonly ScrollViewer setupScroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly Button setupSwitch;
    private readonly Button showSwitch;
    private readonly FrameworkElement setupRouting;
    private readonly TextBlock setupHint;
    private WindowState previousWindowState;
    private WindowStyle previousWindowStyle;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly TextBlock selected = Text("Seleziona uno slot", 18);
    private readonly ProgressBar position = new() { Minimum = 0, Maximum = 1, Height = 10 };
    private readonly TextBlock selectedTime = Text("00:00", 52);
    private readonly TextBlock durationTime = Text("00:00", 52);
    private readonly TextBlock remainingTime = Text("00:00", 52);
    private readonly SemaphoreSlim commands = new(1);
    private bool polling, updating, shuttingDown;
    private readonly Action closeApplication;
    private int ready;
    public bool Ready=>Volatile.Read(ref ready)!=0;
    private StateInfo? last;
    private int current;
    private string? instance;
    private string localWarning = "";
    private DateTime warningUntil;
    public MainWindow(string pipe, int parent,Action? requestClose=null)
    {
        this.pipe = pipe; this.parent = parent;closeApplication=requestClose??(()=>LifecycleSignals.RequestClose("-"+parent));
        Title = "StagePad Live · Setup / Serata"; Width = 1250; Height = 960; WindowStartupLocation=WindowStartupLocation.CenterScreen; MinWidth = 850; MinHeight = 650;
        var workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, workArea.Width); MinHeight = Math.Min(MinHeight, workArea.Height);
        Width = Math.Min(Width, workArea.Width); Height = Math.Min(Height, workArea.Height);
        WindowAppearance.Apply(this);StartupWindowPosition.Attach(this);
        Background = Brush("#0B111A"); Foreground = Brush("#EDF4FC"); FontFamily = new FontFamily("Segoe UI");
        var root = new Grid { Margin = new Thickness(18, 12, 18, 16) }; Content = root;
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 10), MinHeight = 52 };
        header.ColumnDefinitions.Add(new()); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new Image { Width=32,Height=32,Margin=new Thickness(0,0,10,0),
            Source=LogoImage.Create() });
        var brandText=new StackPanel();var brandLine=new StackPanel { Orientation=Orientation.Horizontal };
        var wordmark=Text("STAGEPAD",20);wordmark.FontWeight=FontWeights.SemiBold;wordmark.Margin=new Thickness(0);brandLine.Children.Add(wordmark);
        var liveBadge=Text("LIVE",10);liveBadge.FontWeight=FontWeights.SemiBold;liveBadge.Foreground=Brush("#75E2C1");liveBadge.Margin=new Thickness(3,0,0,3);liveBadge.VerticalAlignment=VerticalAlignment.Bottom;brandLine.Children.Add(liveBadge);
        brandText.Children.Add(brandLine);
        var credit=Text("Created by Alessio Natalucci",9);credit.Foreground=Brush("#6D859E");credit.Margin=new Thickness(0,1,0,0);brandText.Children.Add(credit);brand.Children.Add(brandText);header.Children.Add(brand);
        var navigation = new StackPanel { Orientation = Orientation.Horizontal };
        setupSwitch = Button("Setup", () => { SetView(false); return Task.CompletedTask; });
        showSwitch = Button("Serata", EnterLive);
        setupSwitch.MinWidth = showSwitch.MinWidth = 78;
        navigation.Children.Add(setupSwitch); navigation.Children.Add(showSwitch);
        var segments = new Border { Background = Brush("#121D2B"), Padding = new Thickness(3), CornerRadius = new CornerRadius(11), Margin = new Thickness(10,0,18,0), VerticalAlignment = VerticalAlignment.Center, Child = navigation };
        Grid.SetColumn(segments,1); header.Children.Add(segments);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var settingsButton=Button("⚙",()=>{if(settingsWindow==null){settingsWindow=new SettingsWindow(value=>SendReply(new("budget",Value:value)),OpenLogs,SendReply,OpenUpdates){Owner=this};settingsWindow.Closed+=(_,_)=>settingsWindow=null;settingsWindow.Show();if(last!=null)settingsWindow.Update(last);}else settingsWindow.Activate();return Task.CompletedTask;});
        settingsButton.Width=38;settingsButton.FontSize=22;settingsButton.FontFamily=new FontFamily("Segoe UI Symbol");settingsButton.Padding=new Thickness(0);settingsButton.ToolTip="Impostazioni";
        L.Bind(settingsButton,System.Windows.Automation.AutomationProperties.NameProperty,"Impostazioni");actions.Children.Add(settingsButton);
        lockSwitch = Button("SHOW LOCK", async () => await Send(new("lock", Flag: !(last?.ShowLocked ?? false)))); lockSwitch.MinWidth = 102;
        actions.Children.Add(lockSwitch);
        var fullscreen = Button("⛶", () => { ToggleFullscreen(); return Task.CompletedTask; }); fullscreen.FontSize = 20; fullscreen.Width = 42; fullscreen.Padding = new Thickness(0);
        fullscreen.ToolTip = "Schermo intero · F11 / Esc"; actions.Children.Add(fullscreen);
        var panic = Button("■  STOP ALL", async () => await Send(new("panic")), "#48232E");
        panic.Foreground = Brush("#FFAFB8"); panic.BorderBrush = Brush("#8B4755"); panic.FontSize = 15; panic.MinHeight = 44; panic.MinWidth = 128; panic.Margin = new Thickness(10,0,0,0);
        panic.ToolTip = "PANIC · ferma subito tutte le tracce"; actions.Children.Add(panic);
        Grid.SetColumn(actions,2); header.Children.Add(actions); root.Children.Add(header);
        header.RowDefinitions.Add(new(){Height=GridLength.Auto});header.RowDefinitions.Add(new(){Height=GridLength.Auto});
        header.SizeChanged+=(_,_)=>
        {
            bool compact=header.ActualWidth<810;
            Grid.SetRow(actions,compact?1:0);Grid.SetColumn(actions,compact?0:2);Grid.SetColumnSpan(actions,compact?3:1);
            actions.HorizontalAlignment=compact?HorizontalAlignment.Right:HorizontalAlignment.Stretch;
            actions.Margin=compact?new Thickness(0,6,0,0):new Thickness(0);
        };

        var health = new StackPanel { Margin = new Thickness(0,0,0,7) };
        var chips = new WrapPanel { Orientation = Orientation.Horizontal };
        showName.MaxWidth=200;showName.TextTrimming=TextTrimming.CharacterEllipsis;L.Preserve(showName);
        var showChip=(Border)HealthChip(showName);
        showName.Foreground=Brush("#75E2C1");showName.FontWeight=FontWeights.SemiBold;
        showChip.Background=Brush("#133B32");showChip.BorderBrush=Brush("#326956");showChip.BorderThickness=new Thickness(1);
        chips.Children.Add(showChip);
        chips.Children.Add(HealthChip(status)); chips.Children.Add(HealthChip(inputBadge)); chips.Children.Add(HealthChip(outputBadge));
        health.Children.Add(chips);
        warning.Foreground = Brush("#FFCC80"); warning.Height = 22; warning.TextTrimming = TextTrimming.CharacterEllipsis;
        health.Children.Add(warning); Grid.SetRow(health,1); root.Children.Add(health);

        var workspace = new Grid(); workspace.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); workspace.ColumnDefinitions.Add(new());
        setupHint = Text("Prepara il progetto, poi passa a Serata.", 11); setupHint.Foreground = Brush("#91A6BD"); setupHint.TextWrapping = TextWrapping.Wrap;
        setupRouting = BuildSidebar(); workspace.Children.Add(setupRouting);
        var views=new DockPanel();var gridToolbar=BuildGridToolbar();DockPanel.SetDock(gridToolbar,Dock.Top);views.Children.Add(gridToolbar);views.Children.Add(viewHost);
        Grid.SetColumn(views,1); workspace.Children.Add(views); Grid.SetRow(workspace,2); root.Children.Add(workspace);
        setupScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        setupScroll.PanningMode = PanningMode.Both;
        var grid = setupGrid; setupScroll.Content = grid; liveScroll.Content=liveGrid;
        BindGridViewport(setupScroll,setupGrid);BindGridViewport(liveScroll,liveGrid);
        EnableHorizontalTrackpad();
        for (int i=0; i<4; i++) { grid.RowDefinitions.Add(new()); grid.ColumnDefinitions.Add(new()); }
        for (int i=0; i<StagePad.Core.Projects.MaxSlots; i++)
        {
            int slot=i;
            var panel = new StackPanel { Margin = new Thickness(14,10,14,12) };
            var row = new DockPanel(); var number = Text($"{i+1:00}",11); number.Foreground = Brush("#91A6BD"); number.FontWeight = FontWeights.SemiBold;
            row.Children.Add(number);
            states[i] = Text("VUOTO",10); states[i].HorizontalAlignment=HorizontalAlignment.Right; states[i].Foreground=Brush("#91A6BD"); row.Children.Add(states[i]); panel.Children.Add(row);
            titles[i] = Text($"Slot {i+1}",17); titles[i].FontWeight=FontWeights.SemiBold;titles[i].Foreground=Brushes.White; titles[i].TextTrimming=TextTrimming.CharacterEllipsis; panel.Children.Add(titles[i]);
            times[i] = Text("00:00 / 00:00",17); times[i].FontFamily=new FontFamily("Consolas"); times[i].Foreground=Brush("#B6C9DE"); panel.Children.Add(times[i]);
            var play=PadButton("▶",slot); play.Background=Brush("#23473F"); play.Foreground=Brush("#93F4D3");
            panel.Children.Add(ButtonRow(play,Button("Ⅱ",async()=>{Select(slot);await Send(new("pause",slot));}),Button("■",async()=>{Select(slot);await Send(new("stop",slot));})));
            panel.Children.Add(ButtonRow(Button("Carica…",async()=>
            {
                Select(slot); if(last?.ShowLocked==true){Warn("SHOW LOCK: caricamento bloccato.");return;}
                var dialog=new OpenFileDialog { Filter="Audio|*.wav;*.mp3;*.flac;*.ogg;*.oga;*.m4a;*.aac;*.wma;*.aif;*.aiff|"+L.T("Tutti i file")+"|*.*" };
                if(dialog.ShowDialog(this)==true) await Send(new("load",slot,Path:dialog.FileName));
            }),Button("Configura",()=>{EditSlot(slot);return Task.CompletedTask;})));
            var learnButton=Button("LEARN",async()=>{Select(slot);await Send(new("learn",slot,Flag:last?.LearnSlot!=slot));}); learnButton.FontSize=10;
            var clear=Button("Svuota",async()=>await Send(new("clear",slot))); clear.FontSize=10;
            panel.Children.Add(ButtonRow(learnButton,clear));
            var volume=new Slider { Minimum=0,Maximum=1,Value=1,Margin=new Thickness(0,5,0,0),ToolTip="Volume slot" }; volumes[i]=volume; panel.Children.Add(volume);
            volume.PreviewMouseLeftButtonUp+=async(_,_)=>{if(!updating) await Send(new("volume",slot,Value:(float)volume.Value));};
            volume.PreviewKeyUp+=async(_,_)=>{if(!updating) await Send(new("volume",slot,Value:(float)volume.Value));};
            var body=new DockPanel(); accents[i]=new Border { Height=3,Background=Brush("#4A78BC"),Margin=new Thickness(14,0,14,0),CornerRadius=new CornerRadius(1.5) };
            DockPanel.SetDock(accents[i],Dock.Top);body.Children.Add(accents[i]);body.Children.Add(panel);
            cards[i]=new Border { Child=body,Margin=new Thickness(5),Background=Brush("#121D2B"),BorderThickness=new Thickness(1),BorderBrush=Brush("#26374A"),CornerRadius=new CornerRadius(13),MinHeight=242 };
            cards[i].MouseLeftButtonDown+=(_,_)=>Select(slot);cards[i].Visibility=i<16?Visibility.Visible:Visibility.Collapsed;grid.Children.Add(cards[i]);Grid.SetRow(cards[i],i/4);Grid.SetColumn(cards[i],i%4);
        }
        ApplyGrid(setupGrid,new(),250,270);
        var player=BuildPlayer();Grid.SetRow(player,3);root.Children.Add(player);
        master.ValueChanged+=(_,_)=>{if(!updating)masterPercent.Text=$"{master.Value*100:0}%";};
        master.PreviewMouseLeftButtonUp+=async(_,_)=>{if(!updating)await Send(new("master",Value:(float)master.Value));};
        master.PreviewKeyUp+=async(_,_)=>{if(!updating)await Send(new("master",Value:(float)master.Value));};
        BuildLiveGrid();
        ApplyGrid(liveGrid,new(),200,160);
        SetView(false);
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.F11) { ToggleFullscreen(); e.Handled = true; }
            else if (e.Key == System.Windows.Input.Key.Escape && WindowStyle == WindowStyle.None)
            { ToggleFullscreen(); e.Handled = true; }
        };
        L.Preserve(selected);foreach(var title in titles.Concat(liveTitles))L.Preserve(title);foreach(var card in liveCards)L.Preserve(card);
        L.Changed+=LanguageChanged;L.DirectoryChanged+=ProjectDirectoryChanged;
        ContentRendered+=(_,_)=>Volatile.Write(ref ready,1);
        Loaded += (_,_) => CompositionTarget.Rendering += OnUiFrame;
        Closed += (_,_) => { L.Changed-=LanguageChanged;L.DirectoryChanged-=ProjectDirectoryChanged;CompositionTarget.Rendering -= OnUiFrame; timer.Stop(); };
        timer.Tick += async (_, _) => await Poll(); timer.Start();
        Closing+=(_,_)=>
        {
            if(shuttingDown)return;
            shuttingDown=true;timer.Stop();closeApplication();
        };
    }
    private static FrameworkElement Readout(string label, TextBlock value)
    {
        value.FontWeight=FontWeights.SemiBold;value.FontFamily=new FontFamily("Consolas");value.Margin=new Thickness(0);
        var panel=new StackPanel { Margin=new Thickness(14,0,10,0),VerticalAlignment=VerticalAlignment.Center };
        var caption=Text(label,10);caption.Foreground=Brush("#91A6BD");caption.FontWeight=FontWeights.SemiBold;panel.Children.Add(caption);
        panel.Children.Add(new Viewbox { Height=30,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Left,Child=value });
        return new Border { BorderBrush=Brush("#293C51"),BorderThickness=new Thickness(1,0,0,0),Child=panel };
    }
    private static System.Windows.Shapes.Path TransportIcon(string kind)
    {
        string data=kind switch
        {
            "play"=>"M 4,2 L 17,10 L 4,18 Z",
            "pause"=>"M 3,2 L 8,2 L 8,18 L 3,18 Z M 12,2 L 17,2 L 17,18 L 12,18 Z",
            _=>"M 3,3 L 17,3 L 17,17 L 3,17 Z"
        };
        var geometry=Geometry.Parse(data);geometry.Freeze();
        return new System.Windows.Shapes.Path { Data=geometry,Fill=Brushes.White,Stretch=Stretch.Uniform,Width=19,Height=19,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false };
    }
    private void BuildLiveGrid()
    {
        for(int i=0;i<4;i++){liveGrid.RowDefinitions.Add(new());liveGrid.ColumnDefinitions.Add(new());}
        for(int i=0;i<StagePad.Core.Projects.MaxSlots;i++)
        {
            int slot=i;
            // The controls occupy the actual cell width; no Viewbox shrinks their hit areas.
            var layout=new Grid { Margin=new Thickness(9,5,9,9) };
            layout.RowDefinitions.Add(new(){Height=GridLength.Auto});
            layout.RowDefinitions.Add(new());
            var transportRow=new RowDefinition { Height=new GridLength(36) };layout.RowDefinitions.Add(transportRow);
            liveAccents[i]=new Border { Height=3,Background=Brush("#4A78BC"),CornerRadius=new CornerRadius(1.5),Margin=new Thickness(1,0,1,4) };
            layout.Children.Add(liveAccents[i]);
            var labels=new Grid();labels.RowDefinitions.Add(new(){Height=GridLength.Auto});labels.RowDefinitions.Add(new());labels.RowDefinitions.Add(new(){Height=GridLength.Auto});
            var heading=new Grid();heading.ColumnDefinitions.Add(new(){Width=GridLength.Auto});heading.ColumnDefinitions.Add(new());
            var number=Text($"{i+1:00}",10);number.Foreground=Brush("#91A6BD");number.Margin=new Thickness(0,0,8,0);heading.Children.Add(number);
            liveStates[i]=Text("VUOTO",10);liveStates[i].TextTrimming=TextTrimming.CharacterEllipsis;liveStates[i].HorizontalAlignment=HorizontalAlignment.Right;liveStates[i].Margin=new Thickness(0);Grid.SetColumn(liveStates[i],1);heading.Children.Add(liveStates[i]);labels.Children.Add(heading);
            liveTitles[i]=Text($"Slot {i+1}",22);liveTitles[i].Foreground=Brushes.White;liveTitles[i].FontWeight=FontWeights.SemiBold;liveTitles[i].TextTrimming=TextTrimming.CharacterEllipsis;liveTitles[i].VerticalAlignment=VerticalAlignment.Center;liveTitles[i].Margin=new Thickness(0,2,0,2);var titleViewport=new ScrollingTitle(liveTitles[i]);scrollingTitles[i]=titleViewport;Grid.SetRow(titleViewport,1);labels.Children.Add(titleViewport);
            liveTimes[i]=Text("00:00 / 00:00",15);liveTimes[i].FontFamily=new FontFamily("Consolas");liveTimes[i].Foreground=Brush("#B6C9DE");liveTimes[i].Margin=new Thickness(0,0,0,3);Grid.SetRow(liveTimes[i],2);labels.Children.Add(liveTimes[i]);
            var body=PadButton("",slot);body.Content=labels;body.Background=Brushes.Transparent;body.BorderThickness=new Thickness(0);body.Padding=new Thickness(0);body.Margin=new Thickness(1,0,1,3);body.HorizontalContentAlignment=HorizontalAlignment.Stretch;body.VerticalContentAlignment=VerticalAlignment.Stretch;
            body.ToolTip="Premi il titolo oppure ▶ per eseguire la modalità configurata.";Grid.SetRow(body,1);layout.Children.Add(body);
            var play=PadButton("",slot);play.Content=TransportIcon("play");play.Background=Brush("#23473F");play.ToolTip="Play / riprendi · tieni premuto per Hold";
            var pause=Button("",async()=>{Select(slot);await Send(new("pause",slot));});pause.Content=TransportIcon("pause");pause.ToolTip="Pausa";
            var stopButton=Button("",async()=>{Select(slot);await Send(new("stop",slot));});stopButton.Content=TransportIcon("stop");stopButton.ToolTip="Stop";
            var controls=new Grid { Margin=new Thickness(-2,0,-2,0) };Button[] buttons=[play,pause,stopButton];
            for(int n=0;n<3;n++)
            {
                controls.ColumnDefinitions.Add(new());buttons[n].Padding=new Thickness(0);buttons[n].Margin=new Thickness(2,0,2,0);
                Grid.SetColumn(buttons[n],n);controls.Children.Add(buttons[n]);
                System.Windows.Automation.AutomationProperties.SetName(buttons[n],$"Slot {i+1} · {new[]{"Play","Pausa","Stop"}[n]}");
            }
            Grid.SetRow(controls,2);layout.Children.Add(controls);
            var card=new Border { Margin=new Thickness(5),Background=Brush("#121D2B"),BorderBrush=Brush("#293C51"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Child=layout };
            liveCards[i]=card;card.Visibility=i<16?Visibility.Visible:Visibility.Collapsed;
            card.SizeChanged+=(_,_)=>
            {
                double h=card.ActualHeight;
                heading.Visibility=h<100?Visibility.Collapsed:Visibility.Visible;
                layout.Margin=h<90?new Thickness(6,3,6,5):new Thickness(9,5,9,9);
                liveAccents[slot].Height=h<90?2:3;
                liveAccents[slot].Margin=new Thickness(1,0,1,h<90?2:4);
                transportRow.Height=new GridLength(Math.Clamp(h*.26,24,46));
                liveTitles[slot].FontSize=Math.Clamp(h*.17,12,26);
                liveTimes[slot].FontSize=Math.Clamp(h*.12,11,18);
                liveTimes[slot].Visibility=h<124?Visibility.Collapsed:Visibility.Visible;
                double iconSize=Math.Clamp(h*.14,12,22);
                foreach(var button in buttons)if(button.Content is System.Windows.Shapes.Path icon){icon.Width=icon.Height=iconSize;}
            };
            Grid.SetRow(card,i/4);Grid.SetColumn(card,i%4);liveGrid.Children.Add(card);
        }
    }
    private void SetView(bool live)
    {
        projectBrowser=null;if(gridToolbar!=null)gridToolbar.Visibility=Visibility.Visible;
        liveView=live;
        viewHost.Content = live ? liveScroll : setupScroll;
        RefreshGridControls();
        setupRouting.Visibility = live ? Visibility.Collapsed : Visibility.Visible;
        setupHint.Visibility = live ? Visibility.Collapsed : Visibility.Visible;
        setupSwitch.Background=Brush(live?"#121D2B":"#23473F");showSwitch.Background=Brush(live?"#23473F":"#121D2B");
        setupSwitch.Foreground=Brush(live?"#91A6BD":"#9AF2D7");showSwitch.Foreground=Brush(live?"#9AF2D7":"#91A6BD");
        if(SystemParameters.ClientAreaAnimation) viewHost.BeginAnimation(OpacityProperty,new DoubleAnimation(.78,1,TimeSpan.FromMilliseconds(120)));
        L.Bind(this,TitleProperty,live ? "StagePad Live · SERATA" : "StagePad Live · SETUP");
    }
    private void ToggleFullscreen()
    {
        if (WindowStyle != WindowStyle.None)
        {
            previousWindowState = WindowState; previousWindowStyle = WindowStyle;
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized;
        }
        else { WindowState = WindowState.Normal; WindowStyle = previousWindowStyle; WindowState = previousWindowState; }
    }
    private static LinearGradientBrush PadTint(string hex)
    {
        var color=(Color)ColorConverter.ConvertFromString(hex);
        Color Blend(double strength)=>Color.FromRgb(
            (byte)Math.Round(18+(color.R-18)*strength),
            (byte)Math.Round(29+(color.G-29)*strength),
            (byte)Math.Round(43+(color.B-43)*strength));
        var tint=new LinearGradientBrush(Blend(.18),Blend(.04),new Point(0,0),new Point(1,1));
        tint.Freeze();return tint;
    }
    private static readonly Dictionary<string,SolidColorBrush> brushCache=new(StringComparer.OrdinalIgnoreCase);
    private static SolidColorBrush Brush(string color)
    {
        if(brushCache.TryGetValue(color,out var value))return value;
        value=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));value.Freeze();brushCache[color]=value;return value;
    }
    private static TextBlock Text(string text,double size)
    {
        var label=new TextBlock { Text=text,FontSize=size,Foreground=Brush("#EDF4FC"),Margin=new Thickness(0,3,0,3) };
        L.Bind(label,TextBlock.TextProperty,text);return label;
    }
    private void LanguageChanged()
    {
        L.Apply(setupScroll);L.Apply(liveScroll);L.Apply(setupRouting);L.Apply(this);
        L.Bind(this,TitleProperty,liveView?"StagePad Live · SERATA":"StagePad Live · SETUP");
        if(last!=null)Show(last);
    }
    private static Button Button(string text,Func<Task> action,string background="#1B2B3D")
    {
        var b=new Button { Content=text,Margin=new Thickness(2),Padding=new Thickness(10,7,10,7),Background=Brush(background),Foreground=Brush("#EDF4FC"),HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center };
        L.Bind(b,ContentControl.ContentProperty,text);
        b.Click+=async(_,_)=>{try{await action();}catch(Exception ex){Log.Error("ui-action",ex);}};return b;
    }
    private void Select(int slot) { if(last!=null && (uint)slot>=last.Slots.Length)return;current = slot; if (last != null){Show(last);_ = SyncMarkerContext();} }
    private void Warn(string message) { localWarning = message; warningUntil = DateTime.UtcNow.AddSeconds(12); warning.Text = L.Message(message); }
    private async Task Send(Request request)
    { var reply = await SendReply(request); if (!reply.Ok) Warn(reply.Error ?? "Comando non riuscito."); }
    private long uiPanicEpoch;
    private int waitingCommands;
    private async Task<Reply> SendReply(Request request)
    {
        bool priority = request.Op == "panic" || (request.Op is "play" or "pad-down" && last?.Project is {} emergencyProject && (uint)request.Slot<emergencyProject.Slots.Length && emergencyProject.Slots[request.Slot].EmergencyPauseAll);
        bool independent=priority || request.Op is "marker-context" or "marker-loop" or "export-show" or "import-show" or "test-command" or "save-project" or "load-project" or "rename-project" or "duplicate-project" or "delete-project";
        if (priority) Interlocked.Increment(ref uiPanicEpoch);
        long epoch = Volatile.Read(ref uiPanicEpoch);
        request = request with { Timestamp = request.Timestamp == 0 ? Environment.TickCount64 : request.Timestamp };
        if (Interlocked.Increment(ref waitingCommands) > 128 && !priority)
        { Interlocked.Decrement(ref waitingCommands); return new(false, "Troppi comandi in attesa."); }
        if (!independent) await commands.WaitAsync();
        try
        {
            if (request.Op is "pad-down" or "play" or "seek" && epoch != Volatile.Read(ref uiPanicEpoch)) return new(true);
            return await Wire.Call(pipe, request,request.Op is "export-show" or "import-show"?300000:request.Op=="marker-loop"?30000:request.Op=="test-command"?5000:request.Op is "save-project" or "load-project" or "rename-project" or "duplicate-project" or "delete-project"?20000:1800);
        }
        catch(OperationCanceledException) when(request.Op is "save-project" or "load-project" or "rename-project" or "duplicate-project" or "delete-project")
        {return new(false,"L’operazione non ha risposto in tempo. Controlla l’elenco progetti prima di riprovare.");}
        catch (Exception ex) { Log.Warn("ui-ipc", ex.Message); return new(false, "Motore non raggiungibile: " + ex.Message); }
        finally { if (!independent) commands.Release(); Interlocked.Decrement(ref waitingCommands); }
    }
    private async Task Poll()
    {
        Volatile.Write(ref uiBeat, Environment.TickCount64);
        if (polling) return; polling = true;
        try
        {
            var reply = await Wire.Call(pipe, MarkerContextRequest("state"));
            if (reply.State != null)
            {
                if (instance != null && instance != reply.State.Instance) Warn("Motore audio recuperato: progetto ripristinato, nessuna ripartenza automatica.");
                instance = reply.State.Instance; last = reply.State;
                int? followSlot=null;
                if (seenTrigger != last.TriggerSerial)
                { seenTrigger = last.TriggerSerial; current = last.LastStarted;if(last.TriggerSerial>0)followSlot=current; }
                if((uint)current>=last.Slots.Length){current=0;waveTarget=null;waveLoaded=null;waveform.SetPeaks([]);}
                Show(last);
                if(followSlot is int triggered)QueueFollowTriggeredSlot(triggered,last.TriggerSerial,last.Instance);
                _ = RefreshWave(last);
            }
            try { using var p = Process.GetProcessById(parent); if (p.HasExited) Close(); } catch { Close(); }
        }
        catch { status.Text = L.T("MOTORE NON RAGGIUNGIBILE · nessun riavvio automatico delle tracce"); }
        finally { polling = false; }
    }
    private void Show(StateInfo s)
    {
        updating = true;
        try
        {
            ShowFeatures(s);
            status.Text=s.AudioError!=null?L.T("● AUDIO · ERRORE"):s.AudioStatus.StartsWith("Audio pronto",StringComparison.Ordinal)?L.T("● AUDIO PRONTO"):"● "+L.Message(s.AudioStatus);
            status.Foreground=Brush(s.AudioError==null?"#75E2C1":"#FFCC80");status.ToolTip=L.Message(s.AudioStatus)+" · "+L.F("RAM audio {0:0.0} MiB · errori callback {1}",s.MemoryBytes/1048576.0,s.CallbackErrors);
            var alerts = s.Slots.Where(x => x.Error != null).Select(x => $"Slot {x.Index + 1}: {L.Message(x.Error??"")}").ToList();
            if (s.ProjectLoading) alerts.Insert(0, "Lettura progetto in corso…");
            if (s.MidiIn?.LoggingUnavailable == true || s.MidiOut?.LoggingUnavailable == true) alerts.Add("Log MIDI non scrivibile.");
            if (s.ProjectError != null) alerts.Add(s.ProjectError);
            if(s.OscAlert!=null)alerts.Add(s.OscAlert);
            if (s.MidiAlert != null && s.Project?.Routing.MidiOutput != null) alerts.Add(s.MidiAlert);
            if (s.MidiIn?.Error is { } inError) alerts.Add("MIDI IN: " + inError);
            if (s.MidiOut?.Error is { } outError) alerts.Add("MIDI OUT: " + outError);
            if (s.LearnSlot >= 0) alerts.Insert(0, L.F("LEARN slot {0}: premi il pad da associare (la pressione non avvia lo slot).",s.LearnSlot+1));
            if (s.AudioError != null) alerts.Insert(0, s.AudioError);
            if (Log.Unavailable) alerts.Add("Log UI non scrivibile.");
            if (s.LoggingUnavailable) alerts.Add("Log motore non scrivibile.");
            if (s.Slots.Any(x => x.Underruns > 0)) alerts.Add("Rilevati underrun: verificare il disco e il carico del PC.");
            if (warningUntil > DateTime.UtcNow) alerts.Insert(0, localWarning);
            warning.Text = string.Join("  |  ", alerts.Take(3).Select(L.Message)); warning.ToolTip = warning.Text;
            var old = outputs.SelectedValue as string;
            if (outputs.ItemsSource is not DeviceInfo[] existing || !existing.SequenceEqual(s.Devices))
            { outputs.ItemsSource = s.Devices; outputs.SelectedValue = old ?? s.DeviceId; }
            if (outputs.SelectedIndex < 0 && s.DeviceId != null) outputs.SelectedValue = s.DeviceId;
            if (!master.IsMouseCaptureWithin && !master.IsKeyboardFocusWithin) master.Value = s.Master;
            foreach (var v in s.Slots)
            {
                int i = v.Index; titles[i].Text = v.Name;
                states[i].Text = L.Message(s.Project?.Slots[i].EmergencyPauseAll==true?"EMERGENZA":v.Seeking?"POSIZIONE…":v.Loading ? "CARICAMENTO…" : v.Error != null ? "ERRORE · " + Label(v.State) : s.Project is {} activeProject && !StagePad.Core.SlotActions.Audio(activeProject.Slots[i].Action) ? (StagePad.Core.SlotActions.Osc(activeProject.Slots[i].Action)?(StagePad.Core.SlotActions.Midi(activeProject.Slots[i].Action)?"MIDI + OSC":"COMANDO OSC"):"COMANDO MIDI") : Label(v.State));
                titles[i].Foreground=liveTitles[i].Foreground=Brushes.White;
                var stateBrush = Brush(v.Error!=null?"#EF7E8A":v.State=="Playing"?"#75E2C1":i==current?"#95ABF8":"#293C51");
                cards[i].BorderBrush = stateBrush; liveCards[i].BorderBrush = stateBrush;
                string slotColor=s.Project?.Slots[i].Color??"#4A78BC";
                accents[i].Background=liveAccents[i].Background=Brush(slotColor);
                if(padTintColors[i]!=slotColor)
                {
                    padTintColors[i]=slotColor;
                    var tint=PadTint(slotColor);
                    cards[i].Background=liveCards[i].Background=tint;
                }
                states[i].Foreground=liveStates[i].Foreground=Brush(v.Error!=null?"#EF7E8A":v.State=="Playing"?"#75E2C1":v.State=="Paused"?"#FFCC80":"#91A6BD");
                liveTitles[i].Text = titles[i].Text; liveStates[i].Text = states[i].Text;
                liveCards[i].ToolTip = v.Error==null?v.Name:L.Message(v.Error);
                if (!volumes[i].IsMouseCaptureWithin && !volumes[i].IsKeyboardFocusWithin) volumes[i].Value = v.Volume;
            }
            var currentSlot = s.Slots[current]; selected.Text = $"{current + 1:00} · {currentSlot.Name} · {Label(currentSlot.State)}";
            RenderPlayer();
        }
        finally { updating = false; }
    }
    private static string Label(string state) => L.T(state switch { "Empty" => "VUOTO", "Ready" => "PRONTO", "Playing" => "IN RIPRODUZIONE", "Paused" => "IN PAUSA", "Stopped" => "FERMO", "Ended" => "TERMINATO", "Error" => "ERRORE", "Stopping" => "IN CHIUSURA", _ => state });
}

internal sealed class RecoveryWindow : Window
{
    public RecoveryWindow(string pipe, Func<Process> reopen)
    {
        Title = "StagePad · recupero interfaccia"; Width = 520; Height = 220;
        var panel = new StackPanel { Margin = new Thickness(20) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Interfaccia terminata. Il motore audio resta indipendente.", TextWrapping = TextWrapping.Wrap });
        var panic = new Button { Content = "PANIC / STOP ALL", Margin = new Thickness(5) };
        panic.Click += async (_, _) => { try { await Wire.Call(pipe, new("panic")); } catch { } }; panel.Children.Add(panic);
        var retry = new Button { Content = "Riapri interfaccia", Margin = new Thickness(5) };
        retry.Click += async (_, _) =>
        {
            retry.IsEnabled = false;
            try { using var p = reopen(); await p.WaitForExitAsync(); if (p.ExitCode == 0) Close(); }
            catch (Exception ex) { Log.Error("ui-recovery", ex); }
            finally { retry.IsEnabled = true; }
        }; panel.Children.Add(retry);
    }
}
