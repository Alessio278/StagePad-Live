using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StagePad.Core;

namespace StagePad.App;
internal sealed class SlotEditor : Window
{
    private readonly Dictionary<string, TextBox> texts = [];
    private readonly Dictionary<string, ComboBox> choices = [];
    private readonly TextBlock warning = new() { Foreground = Brushes.Orange, TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel fields = new();
    private readonly SlotSettings original;
    private readonly int slotIndex;
    private readonly Func<Request,Task<Reply>>? test;
    private string selectedColor;
    private readonly CheckBox blockStarts = new() { ToolTip="Gli altri pad audio già in riproduzione continuano. Se ne premi un altro, il suo avvio e i comandi MIDI/OSC associati vengono ignorati." };
    private readonly CheckBox stopOthers = new() { Content = "Ferma gli altri slot audio all’avvio",ToolTip="Avvia subito questo pad e ferma le altre tracce con il fade-out impostato su ciascuna." };
    private readonly CheckBox emergencyHold=new(){Content="Tieni premuto 3 secondi per attivare",ToolTip="Rilascia prima di tre secondi per annullare. Via MIDI usa un pad momentaneo con Note Off."};
    private readonly StackPanel playback = new();
    private TextBlock activationTitle = new(), pauseHint = new(), markerHint = new();
    private readonly CommandDefaults defaults;
    private readonly TextBlock sequencePreview=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.MediumAquamarine};
    private readonly CheckBox emergency=new(){Content="Pausa globale",ToolTip="Mette in pausa tutte le tracce conservando le posizioni."},useMarker=new(){Content="+ Marker",ToolTip="+ Marker è esclusivo: inserisce marker nella traccia selezionata in riproduzione, solo in Setup."};
    private bool populating,changingActions;
    private Button duplicateMidi=new(),duplicateOsc=new(),previewSequence=new();
    private TextBlock? sequenceHint, commandHint;
    private readonly CheckBox useAudio=new(){Content="Audio",ToolTip="Riproduce il file audio assegnato allo slot."},useMidi=new(){Content="MIDI",ToolTip="Invia i comandi MIDI configurati per questo pad."},useOsc=new(){Content="OSC",ToolTip="Invia i messaggi OSC configurati per questo pad."};
    private readonly List<MidiCommandEditor> midiCommands=[];
    private readonly List<OscCommandEditor> oscCommands=[];
    private StackPanel midiList=new(),oscList=new(),midiSection=new(),oscSection=new();
    private SlotSettings populated=new();
    private Button addMidiButton=new(),addOscButton=new();
    public SlotEditor(int index, SlotSettings settings, Func<SlotSettings, Task<Reply>> apply,Func<Request,Task<Reply>>? test=null,CommandDefaults? defaults=null)
    {
        this.test=test;this.defaults=defaults??new();slotIndex=index;original = settings;selectedColor=settings.Color;
        foreach(var box in new[]{useAudio,useMidi,useOsc,emergency,useMarker})
        {box.Checked+=(_,_)=>ActionChanged(box);box.Unchecked+=(_,_)=>Sections();}
        WindowAppearance.Apply(this);
        L.Bind(this,TitleProperty,"Slot {0} · impostazioni",index+1); Width = 650; Height = 950; MinWidth = 460; MinHeight = 450;
        Background = new SolidColorBrush(Color.FromRgb(11, 17, 26)); Foreground = Brushes.White;
        var root=new DockPanel { Margin=new Thickness(22) };Content=root;
        var header=new StackPanel { Margin=new Thickness(0,0,0,16) };DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        header.Children.Add(new TextBlock { Text=$"{index+1:00}  /  {SlotTitles.Display(settings,index)}",FontSize=23,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis });
        header.Children.Add(new TextBlock { Text="Audio, controller, comandi MIDI e OSC",TextWrapping=TextWrapping.Wrap,FontSize=12,Foreground=new SolidColorBrush(Color.FromRgb(145,166,189)),Margin=new Thickness(0,5,0,0) });
        var bottom = new StackPanel(); DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
        bottom.Children.Add(warning);
        var save = new Button { Content = "Applica allo slot", Background = new SolidColorBrush(Color.FromRgb(35,71,63)), Foreground = new SolidColorBrush(Color.FromRgb(154,242,215)), Padding = new Thickness(12), Margin = new Thickness(0,8,0,0) };
        var reset=new Button { Content="Reset",Padding=new Thickness(14,10,14,10),Margin=new Thickness(0,8,10,0),
            ToolTip="Ripristina i valori predefiniti della configurazione. Premi Applica allo slot per salvarli." };
        var actions=new Grid();actions.ColumnDefinitions.Add(new(){Width=GridLength.Auto});actions.ColumnDefinitions.Add(new());
        actions.Children.Add(reset);Grid.SetColumn(save,1);actions.Children.Add(save);bottom.Children.Add(actions);
        reset.Click+=(_,_)=>
        {
            Populate(Projects.DefaultSlot(index));
            L.Bind(warning,TextBlock.TextProperty,"Valori predefiniti caricati. Premi Applica allo slot per salvare.");
        };
        root.Children.Add(new ScrollViewer { Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Populate(settings);
        save.Click += async (_, _) =>
        {
            save.IsEnabled = reset.IsEnabled = false;
            try
            {
                var next = Read(); Projects.ValidateSlot(next);
                var reply = await apply(next);
                if (reply.Ok) Close(); else warning.Text = L.Message(reply.Error??"");
            }
            catch (Exception ex) { warning.Text = L.Message(ex.Message); }
            finally { save.IsEnabled = reset.IsEnabled = true; }
        };
    }
    private void Populate(SlotSettings settings)
    {
        populating=true;fields.Children.Clear();texts.Clear();choices.Clear();midiCommands.Clear();oscCommands.Clear();selectedColor=settings.Color;
        midiList=new();oscList=new();populated=settings;
        Text("Nome",SlotTitles.Automatic(settings,slotIndex)?"":settings.Name);
        fields.Children.Add(new TextBlock{Text="Nome vuoto: usa automaticamente il nome della traccia caricata.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=11});AddPalette();
        playback.Children.Clear();
        useAudio.IsChecked=SlotActions.Audio(settings.Action);useMidi.IsChecked=SlotActions.Midi(settings.Action);useOsc.IsChecked=SlotActions.Osc(settings.Action);
        emergency.IsChecked=settings.EmergencyPauseAll;useMarker.IsChecked=settings.Action==SlotAction.Marker;
        Heading("Uscite dello slot");
        AddActionRow(useAudio, useMidi, useOsc);
        Heading("Comandi di regia");
        AddActionRow(emergency, useMarker);
        pauseHint = Hint("Mette in pausa tutte le tracce conservando le posizioni.");
        markerHint = Hint("+ Marker è esclusivo: inserisce marker nella traccia selezionata in riproduzione, solo in Setup.");
        activationTitle = Heading("Attivazione");
        emergencyHold.IsChecked=settings.RequiresHold;
        foreach (var option in new[]{emergencyHold,blockStarts,stopOthers})
        {
            option.Margin=new Thickness(0,4,0,4);
            option.Padding=new Thickness(5,3,5,3);
        }
        fields.Children.Add(emergencyHold);
        fields.Children.Add(playback);
        Heading("Riproduzione audio", playback);
        Choice("Play", settings.Mode, new Dictionary<PlayMode,string> { [PlayMode.OneShot]="Tocca e parte (ripremi: riparte)", [PlayMode.Hold]="Tieni premuto", [PlayMode.Protected]="Senza interruzione (solo STOP mouse)", [PlayMode.TogglePause]="Play / pausa / riprendi" }, playback);
        Text("Fade in ms", settings.FadeInMs, playback); Text("Fade out ms", settings.FadeOutMs, playback);
        blockStarts.Content=new TextBlock { Text="Impedisci di avviare altri pad audio durante la riproduzione", TextWrapping=TextWrapping.Wrap };
        blockStarts.IsChecked=settings.BlockOtherStarts;
        stopOthers.IsChecked=settings.StopOthersOnStart;
        playback.Children.Add(blockStarts);
        playback.Children.Add(stopOthers);
        Heading("Mappatura controller - puoi anche usare LEARN nella griglia Setup");
        Text("Valore LED a riposo",settings.LedIdleValue);Text("Valore LED premuto",settings.LedActiveValue);
        Text("Canale Pad", settings.PadChannel); Text("Nota pad −1 = non associato", settings.PadNote);
        midiSection=new StackPanel();midiSection.Children.Add(midiList);
        addMidiButton=CommandForm.Button("Aggiungi comando MIDI",()=>{if(midiCommands.Count<16)AddMidi(defaults.Midi());});midiSection.Children.Add(addMidiButton);
        midiSection.Children.Insert(0,new TextBlock{Text="Comandi MIDI · massimo 16",Foreground=CommandForm.Accent,Margin=new Thickness(0,22,0,8)});fields.Children.Add(midiSection);
        foreach(var command in SlotActions.MidiList(settings))AddMidi(!SlotActions.Midi(settings.Action) && settings.MidiCommands==null?defaults.Midi() with{Note=command.Note}:defaults.Resolve(command));
        oscSection=new StackPanel();oscSection.Children.Add(oscList);
        addOscButton=CommandForm.Button("Aggiungi comando OSC",()=>{if(oscCommands.Count<16)AddOsc(defaults.Osc());});oscSection.Children.Add(addOscButton);
        oscSection.Children.Insert(0,new TextBlock{Text="Comandi OSC · UDP · massimo 16",Foreground=CommandForm.Accent,Margin=new Thickness(0,22,0,8)});fields.Children.Add(oscSection);
        foreach(var command in settings.OscCommands)AddOsc(defaults.Resolve(command));
        var sequenceTools=new WrapPanel();
        sequenceTools.Children.Add(duplicateMidi=CommandForm.Button("Duplica ultimo MIDI",()=>{if(midiCommands.Count<16 && midiCommands.Count>0)try{AddMidi(midiCommands[^1].Read());}catch(Exception ex){warning.Text=ex.Message;}}));
        sequenceTools.Children.Add(duplicateOsc=CommandForm.Button("Duplica ultimo OSC",()=>{if(oscCommands.Count<16 && oscCommands.Count>0)try{AddOsc(oscCommands[^1].Read());}catch(Exception ex){warning.Text=ex.Message;}}));
        sequenceTools.Children.Add(previewSequence=CommandForm.Button("Riepilogo sequenze",()=>{try{int midiAt=0,oscAt=0;sequencePreview.Text=string.Join("\n",new[]{useMidi.IsChecked==true?"MIDI: "+string.Join(" → ",midiCommands.Select(c=>(midiAt+=c.Read().DelayMs)+" ms")):null,useOsc.IsChecked==true?"OSC: "+string.Join(" → ",oscCommands.Select(c=>(oscAt+=c.Read().DelayMs)+" ms")):null}.Where(v=>v!=null));}catch(Exception ex){warning.Text=ex.Message;}}));
        fields.Children.Add(sequenceTools);if(sequencePreview.Parent is Panel oldPreview)oldPreview.Children.Remove(sequencePreview);fields.Children.Add(sequencePreview);
        Heading("Le attese sono cumulative nella sequenza. MIDI e OSC partono insieme e avanzano indipendentemente. STOP annulla i comandi ancora in attesa.");
        sequenceHint=fields.Children.OfType<TextBlock>().LastOrDefault();
        commandHint=Hint("Ogni pressione invia i comandi selezionati. OSC usa UDP: verifica host, porta e percorso nel ricevitore. Il rilascio chiude le note MIDI Gate.");
        populating=false;Sections();
        L.Apply(fields);
    }
    private void ActionChanged(CheckBox box)
    {
        if(populating || changingActions)return;
        changingActions=true;
        try
        {
            if(box==useMarker)foreach(var other in new[]{useAudio,useMidi,useOsc,emergency})other.IsChecked=false;
            else useMarker.IsChecked=false;
            if(box==useMidi && midiCommands.Count==0)AddMidi(defaults.Midi());
            if(box==useOsc && oscCommands.Count==0)AddOsc(defaults.Osc());
        }
        finally{changingActions=false;Sections();}
    }
    private void Sections()
    {
        bool midi=useMidi.IsChecked==true,osc=useOsc.IsChecked==true;
        midiSection.Visibility=duplicateMidi.Visibility=midi?Visibility.Visible:Visibility.Collapsed;
        oscSection.Visibility=duplicateOsc.Visibility=osc?Visibility.Visible:Visibility.Collapsed;
        previewSequence.Visibility=midi || osc?Visibility.Visible:Visibility.Collapsed;
        sequencePreview.Text="";sequencePreview.Visibility=previewSequence.Visibility;
        if(commandHint!=null)commandHint.Visibility=previewSequence.Visibility;
        if(sequenceHint!=null)sequenceHint.Visibility=previewSequence.Visibility;
        pauseHint.Visibility = emergency.IsChecked==true ? Visibility.Visible : Visibility.Collapsed;
        markerHint.Visibility = useMarker.IsChecked==true ? Visibility.Visible : Visibility.Collapsed;
        activationTitle.Visibility=emergencyHold.Visibility=useMarker.IsChecked!=true ? Visibility.Visible : Visibility.Collapsed;
        playback.Visibility=blockStarts.Visibility=stopOthers.Visibility=useAudio.IsChecked==true && emergency.IsChecked!=true ? Visibility.Visible : Visibility.Collapsed;
    }
    private void AddPalette()
    {
        var picker=new ColorPicker(selectedColor);
        picker.Changed+=color=>selectedColor=color;
        var row=new Grid();row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        row.Children.Add(picker);
        var resetColor=new Button { Content="Default",Margin=new Thickness(8,0,0,0),Padding=new Thickness(10,7,10,7),
            ToolTip="Ripristina soltanto il colore predefinito dello slot." };
        Grid.SetColumn(resetColor,1);row.Children.Add(resetColor);resetColor.Click+=(_,_)=>picker.ResetToDefault();
        Add("Colore",row);
    }
    private sealed record Option<T>(T Key,string Label) { public string Value=>L.T(Label);public override string ToString()=>Value; }
    private void AddActionRow(params CheckBox[] boxes)
    {
        var row = new System.Windows.Controls.Primitives.UniformGrid { Rows=1, Columns=boxes.Length };
        foreach (var box in boxes)
        {
            if (box.Parent is Panel previous) previous.Children.Remove(box);
            box.MinHeight=38;box.Padding=new Thickness(7,5,7,5);box.Margin=new Thickness(0,2,8,4);
            row.Children.Add(box);
        }
        fields.Children.Add(row);
    }
    private TextBlock Hint(string text)
    {
        var hint=new TextBlock { Text=text, TextWrapping=TextWrapping.Wrap, Foreground=Brushes.LightSlateGray, FontSize=11, Margin=new Thickness(4,2,0,0) };
        fields.Children.Add(hint);return hint;
    }
    private TextBlock Heading(string value, Panel? parent=null)
    {
        var title=new TextBlock { Text=value, FontSize=13, FontWeight=FontWeights.SemiBold, Foreground=CommandForm.Accent, TextWrapping=TextWrapping.Wrap, Margin=new Thickness(0,22,0,8) };
        (parent??fields).Children.Add(title);return title;
    }
    private void Add(string name, FrameworkElement editor, Panel? parent=null)
    {
        var grid = new Grid { Margin = new Thickness(0,4,0,4) };
        grid.ColumnDefinitions.Add(new() { Width = new GridLength(.36,GridUnitType.Star) }); grid.ColumnDefinitions.Add(new() { Width=new GridLength(.64,GridUnitType.Star) });
        grid.Children.Add(new TextBlock { Text=name,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,10,0),FontSize=12,Foreground=new SolidColorBrush(Color.FromRgb(166,186,208)), VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(editor, 1); grid.Children.Add(editor); (parent??fields).Children.Add(grid);
    }
    private void Text(string label, object value, Panel? parent=null)
    { var field = new TextBox { Text = Convert.ToString(value, CultureInfo.InvariantCulture), Padding = new Thickness(9,7,9,7), MaxLength = 150 }; texts[label] = field; Add(label, field,parent); }
    private void Choice<T>(string label, T selected, Dictionary<T, string>? labels = null, Panel? parent=null) where T : struct, Enum
    {
        var options = Enum.GetValues<T>().Select(x => new Option<T>(x, labels?.GetValueOrDefault(x) ?? x.ToString())).ToArray();
        var combo = new ComboBox { ToolTip="Scegli come reagisce la traccia alla pressione e al rilascio del pad.", ItemsSource = options, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = selected, Padding = new Thickness(9,7,9,7) };
        choices[label] = combo; Add(label, combo,parent);
    }
    private int N(string key) => int.Parse(texts[key].Text, CultureInfo.InvariantCulture);
    private T Pick<T>(string key) where T : struct, Enum => (T)choices[key].SelectedValue;
    private void AddMidi(MidiSettings value)
    {
        MidiCommandEditor? editor=null;
        editor=new MidiCommandEditor(value,()=>{midiCommands.Remove(editor!);midiList.Children.Remove(editor);addMidiButton.IsEnabled=true;},direction=>Move(midiCommands,midiList,editor!,direction),test==null?null:command=>test(new("test-command",Origin:"midi",Settings:new(){Action=SlotAction.Midi,MidiCommands=[command]})));
        midiCommands.Add(editor);midiList.Children.Add(editor);addMidiButton.IsEnabled=midiCommands.Count<16;
    }
    private void AddOsc(OscCommand value)
    {
        OscCommandEditor? editor=null;
        editor=new OscCommandEditor(value,()=>{oscCommands.Remove(editor!);oscList.Children.Remove(editor);addOscButton.IsEnabled=true;},direction=>Move(oscCommands,oscList,editor!,direction),test==null?null:command=>test(new("test-command",Origin:"osc",Settings:new(){Action=SlotAction.Osc,OscCommands=[command]})));
        oscCommands.Add(editor);oscList.Children.Add(editor);addOscButton.IsEnabled=oscCommands.Count<16;
    }
    private static void Move<T>(List<T> list,Panel panel,T editor,int direction)where T:UIElement
    {int old=list.IndexOf(editor),next=old+direction;if(old<0 || next<0 || next>=list.Count)return;list.RemoveAt(old);list.Insert(next,editor);panel.Children.Remove(editor);panel.Children.Insert(next,editor);}
    internal SlotSettings Read()
    {
        var midi=useMidi.IsChecked==true?midiCommands.Select(e=>e.Read()).ToArray():populated.MidiCommands;
        var osc=useOsc.IsChecked==true?oscCommands.Select(e=>e.Read()).ToArray():populated.OscCommands;
        return original with
        {
            Name=string.IsNullOrWhiteSpace(texts["Nome"].Text)?$"Slot {slotIndex+1}":texts["Nome"].Text.Trim(),AutoName=string.IsNullOrWhiteSpace(texts["Nome"].Text),Color=selectedColor,EmergencyPauseAll=emergency.IsChecked==true,EmergencyHold=emergency.IsChecked==true && emergencyHold.IsChecked==true,
            HoldToStart=useMarker.IsChecked!=true && emergency.IsChecked!=true && emergencyHold.IsChecked==true,
            BlockOtherStarts=blockStarts.Visibility==Visibility.Visible && blockStarts.IsChecked==true,
            StopOthersOnStart=stopOthers.Visibility==Visibility.Visible && stopOthers.IsChecked==true,Action=useMarker.IsChecked==true?SlotAction.Marker:SlotActions.From(useAudio.IsChecked==true,useMidi.IsChecked==true,useOsc.IsChecked==true),Mode=Pick<PlayMode>("Play"),
            FadeInMs=N("Fade in ms"),FadeOutMs=N("Fade out ms"),PadChannel=N("Canale Pad"),PadNote=N("Nota pad −1 = non associato"),
            LedIdleValue=N("Valore LED a riposo"),LedActiveValue=N("Valore LED premuto"),
            Midi=midi?.FirstOrDefault()??populated.Midi,MidiCommands=midi,OscCommands=osc
        };
    }
}
