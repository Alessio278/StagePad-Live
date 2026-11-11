using System.Globalization;
using StagePad.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace StagePad.App;
internal sealed class SettingsWindow : Window
{
    private sealed record LanguageOption(string Code,string Name);
    private readonly TextBox memory=new(){Text="512",Width=90,VerticalContentAlignment=VerticalAlignment.Center};
    private readonly TextBlock diagnostics=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Orange};
    private readonly StackPanel memoryControls=new();
    private StateInfo? state;
    private bool saving,mappingSaving;
    private readonly StackPanel volumeControls=new();
    private readonly TextBlock controlledTrack=new(){Foreground=Brushes.LightSlateGray,TextWrapping=TextWrapping.Wrap};
    private readonly TextBox masterChannel=new(){Text="1"},masterCc=new(){Text="-1"},trackChannel=new(){Text="1"},trackCc=new(){Text="-1"};
    private readonly Button learnMaster=new(){Content="LEARN master"},learnTrack=new(){Content="LEARN traccia"};
    private (MidiCcBinding Master,MidiCcBinding Track)? shownBindings;
    private readonly ComboBox masterMode=CommandForm.Choice(MidiControlMode.Absolute,ModeCaption),trackMode=CommandForm.Choice(MidiControlMode.Absolute,ModeCaption);
    private readonly CheckBox masterPickup=PickupOption("Evita salti di volume del master"),trackPickup=PickupOption("Evita salti di volume della traccia");
    private static CheckBox PickupOption(string label)=>new()
    {
        Foreground=Brushes.White,Margin=new Thickness(0,8,0,4),VerticalContentAlignment=VerticalAlignment.Center,
        Content=new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.White,Margin=new Thickness(5,0,0,0)}
    };
    private static TextBlock PickupHelp()=>new()
    {
        Text="Con fader assoluti, il volume cambia solo quando il controllo raggiunge il valore attuale. Utile per evitare sbalzi quando fader e volume non coincidono.",
        TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12,Margin=new Thickness(24,0,0,10)
    };
    private readonly TextBox masterStep=new(){Text="1"},trackStep=new(){Text="1"};
    private static string ModeCaption(MidiControlMode mode)=>mode switch{MidiControlMode.Absolute=>"Assoluto 0-127",MidiControlMode.RelativeTwosComplement=>"Relativo: complemento a 2",MidiControlMode.RelativeBinaryOffset=>"Relativo: offset 64",_=>"Relativo: segno e modulo"};
    private string notice="";
    public SettingsWindow(Func<int,Task<Reply>> applyBudget,Func<Task> openLogs,Func<Request,Task<Reply>> send,Func<Task>? openUpdates=null)
    {
        WindowAppearance.Apply(this);Title="Impostazioni";Width=540;Height=700;MinWidth=440;MinHeight=390;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Background=new SolidColorBrush(Color.FromRgb(11,17,26));Foreground=Brushes.White;
        var root=new DockPanel { Margin=new Thickness(26) };Content=root;
        var footer=new DockPanel { Margin=new Thickness(0,14,0,0) };DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var done=new Button { Content="Fatto",MinWidth=90,HorizontalAlignment=HorizontalAlignment.Right };DockPanel.SetDock(done,Dock.Right);footer.Children.Add(done);done.Click+=(_,_)=>Close();
        footer.Children.Add(new TextBlock { Text="Created by Alessio Natalucci",FontSize=10,Foreground=Brushes.SlateGray,VerticalAlignment=VerticalAlignment.Center });
        var body=new StackPanel();root.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled });
        body.Children.Add(new TextBlock { Text="Impostazioni",FontSize=25,FontWeight=FontWeights.SemiBold });
        body.Children.Add(new TextBlock { Text="Lingua dell’interfaccia",FontSize=13,Margin=new Thickness(0,22,0,7) });
        var languages=new ComboBox { ItemsSource=new[]{new LanguageOption("it","Italiano"),new LanguageOption("en","English"),new LanguageOption("es","Español"),new LanguageOption("fr","Français")},DisplayMemberPath="Name",SelectedValuePath="Code",SelectedValue=L.Language };
        body.Children.Add(languages);
        body.Children.Add(new TextBlock { Text="La lingua si aggiorna subito ed è salvata su questo PC. L’audio continua senza interruzioni.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,12,0,0) });
        Section(body,"SERATA");
        var follow=new CheckBox { IsChecked=L.FollowTriggeredSlot,Foreground=Brushes.White,
            Content=new TextBlock { Text="Scorrimento automatico al pad attivato",TextWrapping=TextWrapping.Wrap },Margin=new Thickness(0,0,0,8) };
        body.Children.Add(follow);
        body.Children.Add(new TextBlock { Text="Solo in Serata. I pad già completamente visibili non spostano la griglia.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12 });
        void SaveFollow()
        {
            notice=L.SetFollowTriggeredSlot(follow.IsChecked==true)?"":"L’opzione è applicata, ma non è stato possibile salvarla.";
            UpdateNotice();
        }
        follow.Checked+=(_,_)=>SaveFollow();follow.Unchecked+=(_,_)=>SaveFollow();
        Section(body,"VOLUMI MIDI");body.Children.Add(volumeControls);
        volumeControls.Children.Add(CommandForm.Row("Master: canale MIDI",masterChannel));volumeControls.Children.Add(CommandForm.Row("Master: CC (-1 = disabilitato)",masterCc));
        volumeControls.Children.Add(CommandForm.Row("Modalità master",masterMode));volumeControls.Children.Add(masterPickup);volumeControls.Children.Add(PickupHelp());volumeControls.Children.Add(CommandForm.Row("Passo relativo master (%)",masterStep));volumeControls.Children.Add(learnMaster);
        volumeControls.Children.Add(CommandForm.Row("Traccia: canale MIDI",trackChannel));volumeControls.Children.Add(CommandForm.Row("Traccia: CC (-1 = disabilitato)",trackCc));
        volumeControls.Children.Add(CommandForm.Row("Modalità traccia",trackMode));volumeControls.Children.Add(trackPickup);volumeControls.Children.Add(PickupHelp());volumeControls.Children.Add(CommandForm.Row("Passo relativo traccia (%)",trackStep));volumeControls.Children.Add(learnTrack);
        volumeControls.Children.Add(new TextBlock{Text="Le opzioni anti-salto agiscono sui fader assoluti. Il passo relativo serve solo agli encoder relativi. La traccia segue l’ultimo audio ancora in riproduzione.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12,Margin=new Thickness(0,8,0,8)});
        volumeControls.Children.Add(controlledTrack);L.Preserve(controlledTrack);
        var applyControls=CommandForm.Button("Applica assegnazioni MIDI",async()=>
        {
            if(mappingSaving)return;mappingSaving=true;volumeControls.IsEnabled=false;
            try
            {
                var (master,track)=ReadBindings();
                var reply=await send(new("midi-volume-controls",MasterControl:master,TrackControl:track));
                notice=reply.Ok?"Assegnazioni MIDI aggiornate.":reply.Error??"Comando non riuscito.";
            }
            catch(Exception ex){notice=ex.Message;}
            finally{mappingSaving=false;volumeControls.IsEnabled=state?.ShowLocked!=true;UpdateNotice();}
        });volumeControls.Children.Add(applyControls);
        async Task Learn(string target)
        {
            try{bool enabled=state?.VolumeLearn!=target;var maps=ReadBindings();var reply=await send(new("learn-volume",Origin:target,Flag:enabled,MasterControl:maps.Master,TrackControl:maps.Track));notice=reply.Ok?(enabled?"Muovi il fader o knob da assegnare.":""):reply.Error??"Comando non riuscito.";}
            catch(Exception ex){notice=ex.Message;}UpdateNotice();
        }
        learnMaster.Click+=async(_,_)=>await Learn("master");learnTrack.Click+=async(_,_)=>await Learn("track");
        Section(body,"PROGETTI");
        body.Children.Add(new TextBlock { Text="Cartella dei progetti",FontSize=12,Margin=new Thickness(0,0,0,7) });
        var directory=new TextBox { Text=L.ProjectDirectory,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8) };body.Children.Add(directory);
        var directoryActions=new StackPanel { Orientation=Orientation.Horizontal };
        var applyDirectory=new Button { Content="Applica cartella",Margin=new Thickness(0,0,8,0) };
        var resetDirectory=new Button { Content="Default",ToolTip="Ripristina la cartella project predefinita. I file esistenti non vengono spostati." };
        directoryActions.Children.Add(applyDirectory);directoryActions.Children.Add(resetDirectory);body.Children.Add(directoryActions);
        applyDirectory.Click+=async(_,_)=>
        {
            applyDirectory.IsEnabled=resetDirectory.IsEnabled=false;
            try
            {
                string path=ProjectDirectoryPolicy.DirectoryPath(directory.Text);
                await Task.Run(()=>System.IO.Directory.CreateDirectory(path));
                notice=L.SetProjectDirectory(path)?"Cartella progetti aggiornata.":"L’opzione è applicata, ma non è stato possibile salvarla.";
                directory.Text=L.ProjectDirectory;
            }
            catch(Exception ex){notice=ex.Message;}
            finally{applyDirectory.IsEnabled=resetDirectory.IsEnabled=true;UpdateNotice();}
        };
        resetDirectory.Click+=(_,_)=>
        {
            notice=L.SetProjectDirectory(null)?"Cartella predefinita ripristinata.":"L’opzione è applicata, ma non è stato possibile salvarla.";
            directory.Text=L.ProjectDirectory;UpdateNotice();
        };
        Section(body,"MEMORIA AUDIO");
        body.Children.Add(memoryControls);
        memoryControls.Children.Add(new TextBlock { Text="Budget RAM · MiB",FontSize=12,Margin=new Thickness(0,0,0,7) });
        var memoryRow=new Grid();memoryRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});memoryRow.ColumnDefinitions.Add(new());memoryRow.Children.Add(memory);
        var apply=new Button { Content="Applica MiB",Margin=new Thickness(12,0,0,0) };Grid.SetColumn(apply,1);memoryRow.Children.Add(apply);memoryControls.Children.Add(memoryRow);
        body.Children.Add(status);status.Margin=new Thickness(0,10,0,0);L.Preserve(status);L.Preserve(diagnostics);
        Section(body,"DIAGNOSTICA");body.Children.Add(diagnostics);
        var actions=new Grid { Margin=new Thickness(0,14,0,0) };actions.ColumnDefinitions.Add(new());actions.ColumnDefinitions.Add(new());
        var logs=new Button { Content="Apri log",Margin=new Thickness(0,0,6,0) };var licenses=new Button { Content="Licenze",Margin=new Thickness(6,0,0,0) };Grid.SetColumn(licenses,1);actions.Children.Add(logs);actions.Children.Add(licenses);body.Children.Add(actions);
        body.Children.Add(new TextBlock { Text="STAGEPAD LIVE · 0.13.2",FontSize=10,Foreground=Brushes.SlateGray,Margin=new Thickness(0,18,0,0) });
        body.Children.Add(new TextBlock{Text="App portatile: aggiorna sostituendo StagePad.exe. Progetti e impostazioni restano salvati.",FontSize=11,Foreground=Brushes.LightSlateGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)});
        body.Children.Add(CommandForm.Button("Aggiornamenti",()=>{if(openUpdates!=null)_=openUpdates();}));
        logs.Click+=async(_,_)=>{try{await openLogs();}catch(Exception ex){notice=ex.Message;UpdateNotice();}};
        licenses.Click+=async(_,_)=>{try{await Notices.Open();}catch(Exception ex){notice=ex.Message;UpdateNotice();}};
        apply.Click+=async(_,_)=>
        {
            if(saving)return;
            if(!int.TryParse(memory.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out int budget) || budget is <128 or >4096)
            {notice="Budget RAM non valido.";UpdateNotice();return;}
            saving=true;memoryControls.IsEnabled=false;notice="";UpdateNotice();
            try
            {
                var reply=await applyBudget(budget);notice=reply.Ok?"Budget memoria aggiornato.":reply.Error??"Comando non riuscito.";
            }
            catch(Exception ex){notice=ex.Message;}
            finally{saving=false;memoryControls.IsEnabled=state?.ShowLocked!=true;UpdateNotice();}
        };
        languages.SelectionChanged+=(_,_)=>
        {
            if(languages.SelectedValue is string code && code!=L.Language)
            {
                notice=L.Change(code)?"":"La lingua è applicata, ma non è stato possibile salvare la preferenza.";
                UpdateNotice();if(state!=null)Update(state);
            }
        };
        L.Apply(this);
    }
    internal (MidiCcBinding Master,MidiCcBinding Track) ReadBindings()
    {
        var master=new MidiCcBinding(int.Parse(masterChannel.Text,CultureInfo.InvariantCulture),int.Parse(masterCc.Text,CultureInfo.InvariantCulture),(MidiControlMode)masterMode.SelectedValue,masterPickup.IsChecked==true,float.Parse(masterStep.Text.Replace(',','.'),CultureInfo.InvariantCulture)/100);
        var track=new MidiCcBinding(int.Parse(trackChannel.Text,CultureInfo.InvariantCulture),int.Parse(trackCc.Text,CultureInfo.InvariantCulture),(MidiControlMode)trackMode.SelectedValue,trackPickup.IsChecked==true,float.Parse(trackStep.Text.Replace(',','.'),CultureInfo.InvariantCulture)/100);
        MidiCcBinding.Validate(master,track);return(master,track);
    }
    private static void Section(Panel panel,string caption)
    {
        panel.Children.Add(new Border { Height=1,Background=new SolidColorBrush(Color.FromRgb(41,60,81)),Margin=new Thickness(0,22,0,14) });
        panel.Children.Add(new TextBlock { Text=caption,Foreground=new SolidColorBrush(Color.FromRgb(117,226,193)),FontSize=11,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,10) });
    }
    private void UpdateNotice()=>status.Text=L.Message(notice);
    public void Update(StateInfo current)
    {
        string? previousLearn=state?.VolumeLearn;state=current;
        if(previousLearn!=null && current.VolumeLearn==null)notice="Assegnazioni MIDI aggiornate.";
        volumeControls.IsEnabled=!current.ShowLocked && !mappingSaving;
        memoryControls.IsEnabled=!current.ShowLocked && !saving;
        if(!memory.IsKeyboardFocusWithin && !saving)memory.Text=(current.Project?.MemoryMiB??512).ToString(CultureInfo.InvariantCulture);
        if(current.Project is {} project && !mappingSaving && !(masterChannel.IsKeyboardFocusWithin || masterCc.IsKeyboardFocusWithin || trackChannel.IsKeyboardFocusWithin || trackCc.IsKeyboardFocusWithin || masterStep.IsKeyboardFocusWithin || trackStep.IsKeyboardFocusWithin) && shownBindings!=(project.MasterControl,project.CurrentTrackControl))
        {
            shownBindings=(project.MasterControl,project.CurrentTrackControl);
            masterMode.SelectedValue=project.MasterControl.Mode;trackMode.SelectedValue=project.CurrentTrackControl.Mode;
            masterPickup.IsChecked=project.MasterControl.Pickup;trackPickup.IsChecked=project.CurrentTrackControl.Pickup;
            masterStep.Text=(project.MasterControl.Step*100).ToString(CultureInfo.InvariantCulture);trackStep.Text=(project.CurrentTrackControl.Step*100).ToString(CultureInfo.InvariantCulture);
            masterChannel.Text=project.MasterControl.Channel.ToString(CultureInfo.InvariantCulture);masterCc.Text=project.MasterControl.Controller.ToString(CultureInfo.InvariantCulture);
            trackChannel.Text=project.CurrentTrackControl.Channel.ToString(CultureInfo.InvariantCulture);trackCc.Text=project.CurrentTrackControl.Controller.ToString(CultureInfo.InvariantCulture);
        }
        L.Bind(learnMaster,ContentControl.ContentProperty,current.VolumeLearn=="master"?"Annulla LEARN":"LEARN master");
        L.Bind(learnTrack,ContentControl.ContentProperty,current.VolumeLearn=="track"?"Annulla LEARN":"LEARN traccia");
        controlledTrack.Text=L.F("Traccia controllata: {0}",current.Slots.FirstOrDefault(v=>v.Index==current.CurrentPlaying)?.Name??L.T("nessun audio in riproduzione"));
        string Port(MidiPortState? port)=>port==null?L.T("processo non disponibile"):L.Message(port.Status)+(string.IsNullOrEmpty(port.LastMessage)?"":" · "+port.LastMessage);
        diagnostics.Text=L.Message(current.AudioStatus)+"\n"+L.F("RAM audio {0:0.0} MiB · errori callback {1}",current.MemoryBytes/1048576.0,current.CallbackErrors)+
            "\nMIDI IN: "+Port(current.MidiIn)+"\nMIDI OUT: "+Port(current.MidiOut);
        diagnostics.Text+="\nOSC OUT · "+current.OscSent+" · "+(current.OscAlert??"OK");diagnostics.ToolTip=diagnostics.Text;UpdateNotice();
    }
}
