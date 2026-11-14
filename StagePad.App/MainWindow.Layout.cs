using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StagePad.App;
internal sealed partial class MainWindow
{
    private readonly TextBlock inputBadge=Text("● MIDI IN · AVVIO",11), outputBadge=Text("● MIDI OUT · AVVIO",11);
    private Panel? sidebarSettings;
    private readonly ComboBox gridRows=new(){ItemsSource=Enumerable.Range(1,10).ToArray(),SelectedItem=4},gridColumns=new(){ItemsSource=Enumerable.Range(1,10).ToArray(),SelectedItem=4};
    private StagePad.Core.GridLayout? appliedGrid, appliedSetupGrid;
    private bool liveView;
    private StackPanel? gridToolbar;
    private readonly ComboBox slotCount=new(){ItemsSource=Enumerable.Range(1,StagePad.Core.Projects.MaxSlots).ToArray(),SelectedItem=16};
    private int appliedSlotCount=16,appliedGridSlotCount=-1;
    private static FrameworkElement HealthChip(TextBlock label)
    {
        label.Margin=new Thickness(10,4,10,4);label.Foreground=Brush("#91A6BD");
        return new Border { Background=Brush("#121D2B"),CornerRadius=new CornerRadius(6),Margin=new Thickness(0,0,8,0),Child=label };
    }
    private static Grid ButtonRow(params Button[] buttons)
    {
        var row=new Grid { Margin=new Thickness(-2,4,-2,0) };
        for(int i=0;i<buttons.Length;i++)
        {
            row.ColumnDefinitions.Add(new());buttons[i].FontSize=11;buttons[i].Padding=new Thickness(7,5,7,5);
            Grid.SetColumn(buttons[i],i);row.Children.Add(buttons[i]);
        }
        return row;
    }
    private static FrameworkElement Field(string caption,FrameworkElement control)
    {
        var panel=new StackPanel { Margin=new Thickness(0,6,0,4) };
        var label=Text(caption,11);label.Foreground=Brush("#91A6BD");panel.Children.Add(label);
        if(control is ComboBox combo){combo.MinWidth=0;combo.Width=double.NaN;combo.HorizontalAlignment=HorizontalAlignment.Stretch;}
        panel.Children.Add(control);return panel;
    }
    private static void Section(Panel panel,string title)
    {
        panel.Children.Add(new Border { Height=1,Background=Brush("#293C51"),Margin=new Thickness(0,16,0,12) });
        var label=Text(title,10);label.FontWeight=FontWeights.SemiBold;label.Foreground=Brush("#75E2C1");panel.Children.Add(label);
    }
    private FrameworkElement BuildSidebar()
    {
        var body=new StackPanel { Margin=new Thickness(14,12,14,14) };
        var headline=Text("Il tuo show",19);headline.FontWeight=FontWeights.SemiBold;body.Children.Add(headline);body.Children.Add(setupHint);
        var settings=new StackPanel();sidebarSettings=settings;body.Children.Add(settings);
        Section(settings,"PROGETTO");settings.Children.Add(ButtonRow(Button("Nuovo show",NewShow),Button("Apri",OpenProject)));settings.Children.Add(ButtonRow(Button("Salva",SaveProject),Button("Salva con nome",SaveProjectAs)));
        Section(settings,"SLOT");settings.Children.Add(Field("Numero di slot · 1-100",slotCount));
        settings.Children.Add(Button("Applica numero slot",async()=>await Send(new("slot-count",Value:(int)(slotCount.SelectedItem??16)))));
        Section(settings,"USCITA AUDIO");settings.Children.Add(Field("Dispositivo",outputs));
        var bufferRow=new Grid();bufferRow.ColumnDefinitions.Add(new(){Width=new GridLength(86)});bufferRow.ColumnDefinitions.Add(new());
        latency.Width=80;bufferRow.Children.Add(latency);
        var applyAudio=Button("Applica",async()=>await SendRouting());Grid.SetColumn(applyAudio,1);bufferRow.Children.Add(applyAudio);
        settings.Children.Add(Field("Buffer / latenza · ms",bufferRow));
        settings.Children.Add(Button("Test audio · −24 dB",async()=>await Send(new("test"))));
        Section(settings,"MIDI ROUTING");settings.Children.Add(Field("Controller · MIDI IN",midiInput));settings.Children.Add(Field("Destinazione · MIDI OUT",midiOutput));
        settings.Children.Add(Button("Applica MIDI",async()=>await SendRouting(false)));
        var testInput=Button("Test IN",()=>{Warn("Premi un pad e osserva MIDI IN. Gli slot già associati restano operativi durante il test.");return Task.CompletedTask;});
        var testOutput=Button("Test OUT",async()=>await Send(new("test-midi",current)));testOutput.ToolTip="Invia il messaggio MIDI dello slot selezionato al dispositivo di destinazione, senza audio.";
        settings.Children.Add(ButtonRow(testInput,testOutput));
        return new Border { Width=264,Margin=new Thickness(0,5,14,5),Background=Brush("#101B28"),BorderBrush=Brush("#26374A"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(13),
            Child=new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly } };
    }
    private Task OpenLogs()
    {
        try
        {
            string folder=System.IO.Path.Combine(Log.Root,"logs");System.IO.Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe",folder){UseShellExecute=true});
        }
        catch(Exception ex){Warn(ex.Message);}
        return Task.CompletedTask;
    }
    private FrameworkElement BuildPlayer()
    {
        waveform.SeekRequested+=async fraction=>
        {
            if(last==null)return;int slot=current;
            markerFeedback.Reset();await Send(new("seek",slot,Value:(float)fraction,Origin:"mouse"));
        };
        selected.TextTrimming=TextTrimming.CharacterEllipsis;selected.FontSize=17;selected.FontWeight=FontWeights.SemiBold;
        waveform.AddMarkerRequested+=fraction=>OpenMarkers(fraction*(last?.Slots[current].Duration??0));
        waveform.EditMarkerRequested+=id=>OpenMarkers(SelectedPlayerPosition(),id);
        waveform.Height=82;waveform.Margin=new Thickness(0,6,0,0);position.Height=3;position.Margin=new Thickness(0);
        var layout=new Grid();layout.ColumnDefinitions.Add(new());layout.ColumnDefinitions.Add(new(){Width=new GridLength(185)});
        var audioArea=new Grid{Margin=new Thickness(0,0,16,0)};audioArea.RowDefinitions.Add(new(){Height=GridLength.Auto});audioArea.RowDefinitions.Add(new(){Height=GridLength.Auto});audioArea.RowDefinitions.Add(new(){Height=GridLength.Auto});layout.Children.Add(audioArea);
        var heading=new Grid();heading.ColumnDefinitions.Add(new(){Width=new GridLength(2,GridUnitType.Star)});heading.ColumnDefinitions.Add(new(){Width=new GridLength(3,GridUnitType.Star)});audioArea.Children.Add(heading);
        var title=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)};
        var label=Text("TRACCIA SELEZIONATA",10);label.Foreground=Brush("#91A6BD");label.FontWeight=FontWeights.SemiBold;
        title.Children.Add(label);title.Children.Add(selected);heading.Children.Add(title);
        var readouts=new Grid();for(int i=0;i<3;i++)readouts.ColumnDefinitions.Add(new());
        var elapsed=Readout("TRASCORSO",selectedTime);var duration=Readout("DURATA TOTALE",durationTime);var remaining=Readout("RIMANENTE",remainingTime);
        durationTime.Foreground=Brush("#9BAFC5");remainingTime.Foreground=Brush("#FFCC80");
        readouts.Children.Add(elapsed);Grid.SetColumn(duration,1);Grid.SetColumn(remaining,2);readouts.Children.Add(duration);readouts.Children.Add(remaining);Grid.SetColumn(readouts,1);heading.Children.Add(readouts);
        waveform.Height=72;waveform.HorizontalAlignment=HorizontalAlignment.Stretch;Grid.SetRow(waveform,1);audioArea.Children.Add(waveform);Grid.SetRow(position,2);audioArea.Children.Add(position);
        var tools=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(tools,1);layout.Children.Add(tools);
        var masterHeading=new DockPanel();var masterLabel=Text("MASTER",10);masterLabel.Foreground=Brush("#91A6BD");masterHeading.Children.Add(masterLabel);
        masterPercent.FontSize=12;masterPercent.HorizontalAlignment=HorizontalAlignment.Right;masterHeading.Children.Add(masterPercent);tools.Children.Add(masterHeading);
        master.Width=double.NaN;master.HorizontalAlignment=HorizontalAlignment.Stretch;master.Margin=new Thickness(0,2,0,0);tools.Children.Add(master);tools.Children.Add(MarkerControls());
        var chrome=new Grid{Margin=new Thickness(0,12,0,0)};
        playerHaloWide=new Border{BorderBrush=playerRed,BorderThickness=new Thickness(10),CornerRadius=new CornerRadius(21),Margin=new Thickness(-7),Opacity=0,IsHitTestVisible=false};
        playerHaloInner=new Border{BorderBrush=playerRed,BorderThickness=new Thickness(6),CornerRadius=new CornerRadius(17),Margin=new Thickness(-3),Opacity=0,IsHitTestVisible=false};
        playerChrome=new Border{Padding=new Thickness(16,12,16,12),Background=Brush("#121D2B"),BorderBrush=playerNeutral,BorderThickness=new Thickness(5),CornerRadius=new CornerRadius(14),Child=layout};
        chrome.Children.Add(playerHaloWide);chrome.Children.Add(playerHaloInner);chrome.Children.Add(playerChrome);return chrome;
    }
    private FrameworkElement BuildGridToolbar()
    {
        var bar=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,8) };gridToolbar=bar;

        foreach(var pair in new[]{("Righe",gridRows),("Colonne",gridColumns)})
        {
            var label=Text(pair.Item1,11);label.VerticalAlignment=VerticalAlignment.Center;label.Margin=new Thickness(8,0,5,0);bar.Children.Add(label);
            pair.Item2.Width=58;pair.Item2.MinWidth=0;bar.Children.Add(pair.Item2);
        }
        bar.Children.Add(Button("Applica",async()=>await Send(new("grid",Rows:(int)(gridRows.SelectedItem??4),Columns:(int)(gridColumns.SelectedItem??4),Origin:liveView?"serata":"setup"))));
        bar.ToolTip="Massimo 10 righe e 10 colonne, con una cella per ogni slot attivo. Scorri per raggiungere quelle fuori schermo.";
        return bar;
    }
    private void RefreshGridControls()
    {
        var value=(liveView?appliedGrid:appliedSetupGrid)??new();

        gridRows.SelectedItem=value.Rows;gridColumns.SelectedItem=value.Columns;
    }
    private void ApplyLayouts(StagePad.Core.ProjectDocument project)
    {
        bool refresh=false;
        if(appliedSlotCount!=project.SlotCount)
        {
            appliedSlotCount=project.SlotCount;slotCount.SelectedItem=project.SlotCount;
            for(int i=0;i<cards.Length;i++)
            {
                cards[i].Visibility=liveCards[i].Visibility=i<project.SlotCount?Visibility.Visible:Visibility.Collapsed;
                displayedPadElapsed[i]=displayedPadDuration[i]=-1;
            }
        }
        if(appliedGrid!=project.Grid || appliedGridSlotCount!=project.SlotCount){appliedGrid=project.Grid;ApplyGrid(liveGrid,project.Grid,200,160);refresh=liveView;}
        if(appliedSetupGrid!=project.SetupGrid || appliedGridSlotCount!=project.SlotCount){appliedSetupGrid=project.SetupGrid;ApplyGrid(setupGrid,project.SetupGrid,250,270);refresh|=!liveView;}
        appliedGridSlotCount=project.SlotCount;
        if(refresh)RefreshGridControls();
    }
    private void ApplyGrid(Grid grid,StagePad.Core.GridLayout value,double cellWidth,double cellHeight)
    {
        grid.RowDefinitions.Clear();grid.ColumnDefinitions.Clear();
        // Minimum extent plus scrolling prevents controls and labels being crushed.
        grid.MinWidth=value.Columns*cellWidth;grid.MinHeight=value.Rows*cellHeight;
        for(int i=0;i<value.Rows;i++)grid.RowDefinitions.Add(new(){MinHeight=cellHeight});
        for(int i=0;i<value.Columns;i++)grid.ColumnDefinitions.Add(new(){MinWidth=cellWidth});
        for(int i=0;i<grid.Children.Count;i++){Grid.SetRow(grid.Children[i],i<appliedSlotCount?i/value.Columns:0);Grid.SetColumn(grid.Children[i],i<appliedSlotCount?i%value.Columns:0);}
        FitGridViewport(grid==liveGrid?liveScroll:setupScroll,grid);
    }
    private static void BindGridViewport(ScrollViewer scroll,Grid grid)
    {
        scroll.SizeChanged+=(_,_)=>FitGridViewport(scroll,grid);
        scroll.ScrollChanged+=(_,e)=>{if(e.ViewportWidthChange!=0 || e.ViewportHeightChange!=0)FitGridViewport(scroll,grid);};
        scroll.Loaded+=(_,_)=>FitGridViewport(scroll,grid);
    }
    private static void FitGridViewport(ScrollViewer scroll,Grid grid)
    {
        // ScrollViewer measures content with infinity. Give the grid a finite
        // extent so star cells cannot grow to fit long names or their controls.
        double width=Math.Max(grid.MinWidth,scroll.ViewportWidth);
        double height=Math.Max(grid.MinHeight,scroll.ViewportHeight);
        if(!double.IsFinite(width) || !double.IsFinite(height))return;
        if(grid.Width!=width)grid.Width=width;
        if(grid.Height!=height)grid.Height=height;
        if(grid.ColumnDefinitions.Count>0)
            foreach(var column in grid.ColumnDefinitions)column.Width=new GridLength(width/grid.ColumnDefinitions.Count);
        if(grid.RowDefinitions.Count>0)
            foreach(var row in grid.RowDefinitions)row.Height=new GridLength(height/grid.RowDefinitions.Count);
    }
    private static void PressFeedback(Button button,bool pressed)
    {
        double opacity=pressed ? .72 : 1;
        if(SystemParameters.ClientAreaAnimation)button.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(opacity,TimeSpan.FromMilliseconds(70)));
        else button.Opacity=opacity;
    }
    private long frameStamp;
    private readonly long[] displayedPadElapsed=Enumerable.Repeat(-1L,StagePad.Core.Projects.MaxSlots).ToArray(), displayedPadDuration=Enumerable.Repeat(-1L,StagePad.Core.Projects.MaxSlots).ToArray();
    private void OnUiFrame(object? sender,EventArgs e)
    {
        long now=Stopwatch.GetTimestamp();
        if(Stopwatch.GetElapsedTime(frameStamp,now).TotalMilliseconds<22 || !IsVisible)return;
        frameStamp=now;
        if(liveView)foreach(var title in scrollingTitles)title?.Tick();
        RenderPlayer();
    }
    private void RenderPlayer()
    {
        if(last==null || (uint)current>=last.Slots.Length)return;
        long now=Environment.TickCount64;
        long anchor=last.RenderHeartbeat>0?last.RenderHeartbeat:last.Heartbeat;
        PlayerReading selectedReading=default;
        foreach(var slot in last.Slots)
        {
            bool playing=slot.State=="Playing" && last.AudioError==null && slot.Underruns==0;
            var loop=LoopBounds(last.Project?.Slots[slot.Index]);
            var reading=UiPlaybackClock.Read(slot.Position,slot.Duration,playing,slot.State=="Ended",anchor,now,loop.Start,loop.End);
            int index=slot.Index;
            if(displayedPadElapsed[index]!=reading.Elapsed || displayedPadDuration[index]!=reading.Duration)
            {
                string text=$"{UiPlaybackClock.Format(reading.Elapsed)} / {UiPlaybackClock.Format(reading.Duration)}";
                times[index].Text=liveTimes[index].Text=text;
                displayedPadElapsed[index]=reading.Elapsed;displayedPadDuration[index]=reading.Duration;
            }
            if(index==current)selectedReading=reading;
        }
        double duration=last.Slots[current].Duration;
        double normalized=duration<=0?0:selectedReading.Position/duration;
        waveform.SetPosition(normalized);position.Value=normalized;
        AssignText(selectedTime,UiPlaybackClock.Format(selectedReading.Elapsed));
        AssignText(durationTime,UiPlaybackClock.Format(selectedReading.Duration));
        AssignText(remainingTime,UiPlaybackClock.Format(selectedReading.Remaining));
        RenderMarkerFeedback(now,anchor);
    }
    private static void AssignText(TextBlock target,string text){if(target.Text!=text)target.Text=text;}
}
