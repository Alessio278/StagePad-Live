using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StagePad.Core;
namespace StagePad.App;
internal sealed class MarkerWindow : Window
{
    public int Slot {get;}
    private readonly string instance;
    private readonly int revision;
    private readonly double duration;
    private readonly Func<Request,Task<Reply>> send;
    private readonly Func<AudioMarker,Task> seek;
    private readonly ListBox list=new(){MinHeight=80,Background=new SolidColorBrush(Color.FromRgb(11,17,26)),Foreground=Brushes.White,BorderThickness=new Thickness(0)};
    private readonly StackPanel form=new();
    private readonly TextBox time=new(),label=new(){MaxLength=64};
    private readonly TextBlock notice=new(){Foreground=Brushes.Orange,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)};
    private readonly TextBlock count=new(){Foreground=Brushes.LightSlateGray};
    private readonly Button save=new(){Content="Salva marker"},remove=new(){Content="Elimina marker"};
    private readonly StackPanel colorHost=new();
    private readonly ComboBox loopStart=new(),loopEnd=new();
    private readonly TextBlock loopStatus=new(){Foreground=Brushes.MediumAquamarine,TextWrapping=TextWrapping.Wrap};
    private MarkerLoop? shownLoop;
    private readonly StackPanel triggerHost=new();
    private MidiCommandEditor? midiTrigger;
    private OscCommandEditor? oscTrigger;
    private bool loopInitialized;
    private AudioMarker[] shown=[];
    private string? editId,deleteId;
    private string color="#75E2C1";
    private bool busy,stale,showLocked;
    private StateInfo? latestState;
    private double currentPosition;
    public MarkerWindow(int slot,StateInfo state,double position,string? selected,Func<Request,Task<Reply>> send,Func<AudioMarker,Task> seek)
    {
        Slot=slot;this.send=send;this.seek=seek;instance=state.Instance;revision=state.Slots[slot].Revision;duration=state.Slots[slot].Duration;
        WindowAppearance.Apply(this);Title=L.T("Gestisci marker");Width=600;Height=760;MinWidth=440;MinHeight=480;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Background=new SolidColorBrush(Color.FromRgb(11,17,26));Foreground=Brushes.White;
        var root=new DockPanel{Margin=new Thickness(22)};Content=root;
        var header=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        header.Children.Add(new TextBlock{Text=$"{slot+1:00} · {state.Slots[slot].Name}",FontSize=20,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=Brushes.White});header.Children.Add(count);header.Children.Add(loopStatus);L.Preserve(loopStatus);
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
        bottom.Children.Add(notice);L.Preserve(notice);
        var done=CommandForm.Button("Fatto",Close);bottom.Children.Add(done);
        var body=new Grid();body.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});body.RowDefinitions.Add(new(){Height=new GridLength(1.6,GridUnitType.Star)});root.Children.Add(body);body.Children.Add(list);
        Grid.SetRow(form,1);body.Children.Add(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=350});Grid.SetRow((UIElement)body.Children[^1],1);
        var next=CommandForm.Button("+ Marker",async()=>await AddAt(CursorPosition()));form.Children.Add(next);
        form.Children.Add(CommandForm.Row("Posizione · hh:mm:ss.fff",time));form.Children.Add(CommandForm.Row("Etichetta marker",label));form.Children.Add(CommandForm.Row("Colore marker",colorHost));
        var triggerForm=new StackPanel();var triggerButtons=new WrapPanel();
        triggerButtons.Children.Add(CommandForm.Button("Aggiungi trigger MIDI",()=>{if(midiTrigger==null){SetMidi(new());L.Apply(triggerHost);}}));
        triggerButtons.Children.Add(CommandForm.Button("Aggiungi trigger OSC",()=>{if(oscTrigger==null){SetOsc(new());L.Apply(triggerHost);}}));
        triggerForm.Children.Add(triggerButtons);triggerForm.Children.Add(triggerHost);
        triggerForm.Children.Add(new TextBlock{Text="Invio al passaggio del marker, anche a ogni giro del loop. Il salto manuale non invia i trigger saltati. Le note MIDI hanno Note Off automatico. Le destinazioni MIDI sono quelle dello show.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12});
        form.Children.Add(new Expander{Header="Trigger MIDI / OSC",Content=triggerForm,Foreground=Brushes.White,Margin=new Thickness(0,8,0,0)});
        var actions=new Grid();actions.ColumnDefinitions.Add(new());actions.ColumnDefinitions.Add(new());save.Margin=new Thickness(0,10,5,0);remove.Margin=new Thickness(5,10,0,0);actions.Children.Add(save);Grid.SetColumn(remove,1);actions.Children.Add(remove);form.Children.Add(actions);
        form.Children.Add(new TextBlock{Text="Doppio clic su un marker per saltare al punto. Le modifiche sono salvate nello show.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12,Margin=new Thickness(0,10,0,0)});
        var loopForm=new StackPanel{Margin=new Thickness(0,10,0,0)};
        loopForm.Children.Add(CommandForm.Row("Inizio loop",loopStart));loopForm.Children.Add(CommandForm.Row("Fine loop",loopEnd));
        loopForm.Children.Add(CommandForm.Button("Applica loop",async()=>await SaveLoop(false)));
        loopForm.Children.Add(CommandForm.Button("Rimuovi loop",async()=>await SaveLoop(true)));
        loopForm.Children.Add(new TextBlock{Text="Play avvia la traccia dal marker iniziale del loop. La pausa conserva la posizione. Ferma lo slot per applicare o rimuovere il loop. Rimuovilo prima di spostare o eliminare i due marker. Il tratto viene preparato in RAM entro il budget impostato.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,FontSize=12,Margin=new Thickness(0,8,0,0)});
        form.Children.Add(new Expander{Header="Loop tra marker",Content=loopForm,Foreground=Brushes.White,Margin=new Thickness(0,14,0,0)});
        list.SelectionChanged+=(_,_)=>{if(!busy && list.SelectedItem is ListBoxItem item && item.Tag is AudioMarker marker)Edit(marker);};
        list.MouseDoubleClick+=async(_,_)=>{if(list.SelectedItem is ListBoxItem item && item.Tag is AudioMarker marker)try{await seek(marker);}catch(Exception ex){notice.Text=L.Message(ex.Message);}};
        save.Click+=async(_,_)=>await Save();remove.Click+=async(_,_)=>await Delete();
        Update(state);New(position);if(selected!=null)Choose(selected);L.Apply(this);L.Preserve(count);
        PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape)Close();};
    }
    private double CursorPosition()
    {
        if(latestState==null)return currentPosition;
        var slot=latestState.Slots[Slot];var settings=latestState.Project?.Slots[Slot];
        var loop=settings?.MarkerLoop;var markers=settings?.Markers??[];
        double start=loop is {Enabled:true}?markers.FirstOrDefault(m=>m.Id==loop.StartId)?.Seconds??0:0;
        double end=loop is {Enabled:true}?markers.FirstOrDefault(m=>m.Id==loop.EndId)?.Seconds??0:0;
        return UiPlaybackClock.Read(slot.Position,slot.Duration,slot.State=="Playing" && latestState.AudioError==null,slot.State=="Ended",latestState.RenderHeartbeat>0?latestState.RenderHeartbeat:latestState.Heartbeat,Environment.TickCount64,start,end).Position;
    }
    internal async Task AddAt(double position)
    {
        if(busy || stale || showLocked)return;
        New(position);await Save();
    }
    private void Choose(string id){foreach(ListBoxItem item in list.Items)if(item.Tag is AudioMarker m && m.Id==id){list.SelectedItem=item;Edit(m);break;}}
    private void New(double position){list.SelectedItem=null;editId=deleteId=null;time.Text=AudioMarkers.Format(Math.Clamp(position,0,duration));label.Text="Marker "+(shown.Length+1);SetColor("#75E2C1");SetTriggers(null,null);notice.Text="";Buttons();}
    private void Edit(AudioMarker marker){editId=marker.Id;deleteId=null;time.Text=AudioMarkers.Format(marker.Seconds);label.Text=marker.Label;SetColor(marker.Color);SetTriggers(marker.MidiTrigger,marker.OscTrigger);notice.Text="";Buttons();}
    private void SetTriggers(MidiSettings? midi,OscCommand? osc)
    {
        triggerHost.Children.Clear();midiTrigger=null;oscTrigger=null;
        if(midi!=null)SetMidi(midi);if(osc!=null)SetOsc(osc);
    }
    private void SetMidi(MidiSettings value)
    {
        midiTrigger=new MidiCommandEditor(value,()=>{if(midiTrigger!=null)triggerHost.Children.Remove(midiTrigger);midiTrigger=null;},markerTrigger:true);triggerHost.Children.Add(midiTrigger);
    }
    private void SetOsc(OscCommand value)
    {
        oscTrigger=new OscCommandEditor(value,()=>{if(oscTrigger!=null)triggerHost.Children.Remove(oscTrigger);oscTrigger=null;},markerTrigger:true);triggerHost.Children.Add(oscTrigger);
    }
    private void SetColor(string value){color=value;colorHost.Children.Clear();var picker=new ColorPicker(value);picker.Changed+=v=>color=v;colorHost.Children.Add(picker);}
    private void Buttons(){form.IsEnabled=!busy && !stale && !showLocked;save.IsEnabled=editId!=null || shown.Length<AudioMarkers.Maximum;remove.IsEnabled=editId!=null;L.Bind(remove,ContentControl.ContentProperty,deleteId!=null?"Conferma eliminazione":"Elimina marker");}
    private async Task Save()
    {
        if(busy || stale || showLocked)return;
        try
        {
            if(!AudioMarkers.TryParse(time.Text,out double seconds))throw new InvalidDataException("Posizione marker non valida. Usa hh:mm:ss.fff.");
            if(seconds<=duration+.0005)seconds=Math.Min(seconds,duration);
            var marker=new AudioMarker{Id=editId??Guid.NewGuid().ToString("N"),Seconds=seconds,Label=label.Text.Trim(),Color=color,MidiTrigger=midiTrigger?.Read(),OscTrigger=oscTrigger?.Read()};AudioMarkers.Validate([marker],duration);
            busy=true;Buttons();var reply=await send(new(editId==null?"marker-add":"marker-edit",Slot,Origin:instance,Marker:marker,MarkerId:editId,SourceRevision:revision));
            if(reply.Ok)
            {
                editId=marker.Id;
                var snapshot=await send(new("state"));
                if(snapshot.State is {} state){Update(state);Choose(marker.Id);}
            }
            notice.Text=reply.Ok?L.T("Marker salvato."):L.Message(reply.Error??"Comando non riuscito.");
        }
        catch(Exception ex){notice.Text=L.Message(ex.Message);}
        finally{busy=false;Buttons();}
    }
    private async Task Delete()
    {
        if(busy || stale || showLocked || editId==null)return;
        if(deleteId!=editId){deleteId=editId;notice.Text=L.T("Premi di nuovo per eliminare il marker selezionato.");Buttons();return;}
        busy=true;Buttons();
        try{var reply=await send(new("marker-delete",Slot,Origin:instance,MarkerId:editId,SourceRevision:revision));if(reply.Ok){New(currentPosition);notice.Text=L.T("Marker eliminato.");}else notice.Text=L.Message(reply.Error??"Comando non riuscito.");}
        catch(Exception ex){notice.Text=L.Message(ex.Message);}
        finally{busy=false;Buttons();}
    }
    private static string? SelectedId(ComboBox choice)=>(choice.SelectedItem as ComboBoxItem)?.Tag as string;
    private void FillLoopChoices(string? start,string? end)
    {
        foreach(var choice in new[]{loopStart,loopEnd})
        {
            string? selected=ReferenceEquals(choice,loopStart)?start:end;choice.Items.Clear();
            foreach(var marker in shown)
            {
                var item=new ComboBoxItem{Tag=marker.Id,Content=AudioMarkers.Format(marker.Seconds)+"   "+marker.Label};L.Preserve(item);choice.Items.Add(item);if(marker.Id==selected)choice.SelectedItem=item;
            }
        }
    }
    private async Task SaveLoop(bool removeLoop)
    {
        if(busy || stale || showLocked)return;
        try
        {
            MarkerLoop? value=removeLoop?null:new(true,SelectedId(loopStart),SelectedId(loopEnd));
            MarkerLoops.Validate(value,shown);
            busy=true;Buttons();notice.Text=L.T("Preparazione loop in corso...");
            var reply=await send(new("marker-loop",Slot,Origin:instance,SourceRevision:revision,MarkerLoop:value));
            notice.Text=reply.Ok?L.T("Loop aggiornato."):L.Message(reply.Error??"Comando non riuscito.");
        }
        catch(Exception ex){notice.Text=L.Message(ex.Message);}
        finally{busy=false;Buttons();}
    }
    public void Update(StateInfo state)
    {
        stale=state.Instance!=instance || (uint)Slot>=state.Slots.Length || state.Slots[Slot].Revision!=revision;
        showLocked=state.ShowLocked;
        if(stale){notice.Text=L.T("La traccia è cambiata. Riapri la gestione marker.");Buttons();return;}
        double previousPosition=currentPosition;
        latestState=state;currentPosition=CursorPosition();
        if(Math.Abs(currentPosition-previousPosition)>.000001 && !busy && !time.IsKeyboardFocusWithin)time.Text=AudioMarkers.Format(currentPosition);
        var markers=state.Project?.Slots[Slot].Markers??[];
        if(!AudioMarkers.Same(shown,markers))
        {
            string? start=SelectedId(loopStart),end=SelectedId(loopEnd);
            shown=markers;FillLoopChoices(start,end);string? selected=(list.SelectedItem as ListBoxItem)?.Tag is AudioMarker m?m.Id:null;
            bool oldBusy=busy;busy=true;list.Items.Clear();
            foreach(var marker in markers)
            {
                var row=new DockPanel{Margin=new Thickness(4,5,4,5)};
                var swatch=new Border{Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(marker.Color)),Width=10,Height=18,CornerRadius=new CornerRadius(3),Margin=new Thickness(0,0,10,0)};DockPanel.SetDock(swatch,Dock.Left);row.Children.Add(swatch);
                var text=new TextBlock{Text=AudioMarkers.Format(marker.Seconds)+"   "+marker.Label+(marker.MidiTrigger!=null?" · MIDI":"")+(marker.OscTrigger!=null?" · OSC":""),Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis};row.Children.Add(text);
                var item=new ListBoxItem{Content=row,Tag=marker,ToolTip=marker.Label+" · "+AudioMarkers.Format(marker.Seconds),HorizontalContentAlignment=HorizontalAlignment.Stretch};System.Windows.Automation.AutomationProperties.SetName(item,text.Text);list.Items.Add(item);if(marker.Id==selected)list.SelectedItem=item;
            }
            busy=oldBusy;
        }
        var activeLoop=state.Project?.Slots[Slot].MarkerLoop;
        if(!loopInitialized || shownLoop!=activeLoop)
        {
            loopInitialized=true;shownLoop=activeLoop;
            FillLoopChoices(activeLoop?.StartId,activeLoop?.EndId);
        }
        loopStatus.Text=state.Slots[Slot].LoopPreparing?L.T("Preparazione loop in corso..."):activeLoop is {Enabled:true}?L.T("Loop attivo"):L.T("Loop disattivato");
        count.Text=L.F("{0} / 32 marker · durata {1}",shown.Length,AudioMarkers.Format(duration));Buttons();
        if(showLocked)notice.Text=L.T("SHOW LOCK: modifiche ai marker bloccate.");
    }
}
