using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StagePad.Core;
namespace StagePad.App;
internal sealed partial class MainWindow
{
    private readonly string?[] feedbackColors=new string?[Projects.MaxSlots];
    private readonly Brush?[] idleTints=new Brush?[Projects.MaxSlots],activeTints=new Brush?[Projects.MaxSlots];
    private readonly TextBlock commandDetails=new(){MaxHeight=76,TextTrimming=TextTrimming.CharacterEllipsis,FontSize=12,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,6,0,0)};
    private FrameworkElement? playerReadouts,playerTitle;
    private SlotSettings? shownCommandSettings;
    private CommandDefaults? shownDestinations;
    private readonly bool[] displayedCommandMode=new bool[Projects.MaxSlots];
    private void RenderSlotFeedback(long now)
    {
        if(last?.Project==null)return;
        foreach(var view in last.Slots)
        {
            int i=view.Index;var settings=last.Project.Slots[i];
            if(feedbackColors[i]!=settings.Color){feedbackColors[i]=settings.Color;idleTints[i]=PadTint(settings.Color);activeTints[i]=PadTint(settings.Color,true);}
            bool commandMode=settings.EmergencyPauseAll || !SlotActions.Audio(settings.Action) || settings.File==null && (SlotActions.Midi(settings.Action)||SlotActions.Osc(settings.Action));
            if(displayedCommandMode[i]!=commandMode){displayedCommandMode[i]=commandMode;displayedPadElapsed[i]=displayedPadDuration[i]=-1;}
            liveVolumePercent[i].Text=$"{view.Volume*100:0}%";liveVolumePercent[i].Visibility=SlotActions.Audio(settings.Action) && !settings.EmergencyPauseAll?Visibility.Visible:Visibility.Collapsed;
            bool disabled=!SlotContent.Configured(settings) || settings.Action==SlotAction.Marker && liveView;
            foreach(var button in triggerButtons[i])button.IsEnabled=!disabled;
            if(liveCards[i].Child is FrameworkElement liveContent)liveContent.Opacity=disabled ? .45 : 1;
            bool recent=view.TriggeredAt>0 && now-view.TriggeredAt<1200;
            bool active=!disabled && (view.State=="Playing" || recent || view.HoldProgress>0 || view.IsPressed);
            Brush tint=SlotContent.Configured(settings)?(active?activeTints[i]!:idleTints[i]!):Brush("#121D2B");
            if(!ReferenceEquals(liveCards[i].Background,tint))cards[i].Background=liveCards[i].Background=tint;
            if(view.HoldProgress>0)
                states[i].Text=liveStates[i].Text=L.F("TIENI PREMUTO · {0:0.0} s",Math.Max(0,3*(1-view.HoldProgress)));
            else if(recent)
                states[i].Text=liveStates[i].Text=L.T(view.TriggerOk?"ATTIVATO":"ERRORE INVIO");
            if(settings.EmergencyPauseAll || !SlotActions.Audio(settings.Action) || settings.File==null && (SlotActions.Midi(settings.Action) || SlotActions.Osc(settings.Action)))
            {
                string summary=$"MIDI { (SlotActions.Midi(settings.Action)?SlotActions.MidiList(settings).Length:0) } · OSC { (SlotActions.Osc(settings.Action)?settings.OscCommands.Length:0) }";
                times[i].Text=liveTimes[i].Text=settings.EmergencyPauseAll && !SlotActions.Midi(settings.Action) && !SlotActions.Osc(settings.Action)?L.T("PAUSA GLOBALE"):summary;
                if(!recent && view.HoldProgress==0)states[i].Text=liveStates[i].Text=L.T(settings.EmergencyPauseAll?"EMERGENZA":SlotContent.Configured(settings)?"PRONTO":"VUOTO");
            }
            if(settings.Action==SlotAction.Marker)
            {times[i].Text=liveTimes[i].Text=L.T("+MARKER");states[i].Text=liveStates[i].Text=L.T(recent?"ATTIVATO":"+MARKER");}
            if(settings.Action==SlotAction.None && !settings.EmergencyPauseAll)times[i].Text=liveTimes[i].Text=L.T("NESSUNA AZIONE");
            if(active){cards[i].BorderBrush=liveCards[i].BorderBrush=Brush(view.TriggerOk?settings.Color:"#EF7E8A");states[i].Foreground=liveStates[i].Foreground=Brushes.White;}
        }
        var selectedSettings=last.Project.Slots[current];
        bool commandsOnly=selectedSettings.EmergencyPauseAll || selectedSettings.File==null && (SlotActions.Midi(selectedSettings.Action)||SlotActions.Osc(selectedSettings.Action)) || !SlotActions.Audio(selectedSettings.Action);
        if(commandsOnly)
        {
            var view=last.Slots[current];bool recent=view.TriggeredAt>0 && now-view.TriggeredAt<1200;
            string state=view.HoldProgress>0?L.F("TIENI PREMUTO · {0:0.0} s",Math.Max(0,3*(1-view.HoldProgress))):L.T(recent?(view.TriggerOk?"ATTIVATO":"ERRORE INVIO"):selectedSettings.EmergencyPauseAll?"EMERGENZA":"PRONTO");
            AssignText(selected,$"{current+1:00} · {view.Name} · {state}");
        }
        waveform.Visibility=commandsOnly?Visibility.Collapsed:Visibility.Visible;commandDetails.Visibility=commandsOnly?Visibility.Visible:Visibility.Collapsed;
        if(playerReadouts!=null)playerReadouts.Visibility=commandsOnly?Visibility.Collapsed:Visibility.Visible;
        if(playerTitle!=null)Grid.SetColumnSpan(playerTitle,commandsOnly?2:1);
        if(commandsOnly && (!ReferenceEquals(selectedSettings,shownCommandSettings) || shownDestinations!=last.Project.CommandDefaults))
        {
            shownCommandSettings=selectedSettings;shownDestinations=last.Project.CommandDefaults;selectedSettings=last.Project.CommandDefaults.Resolve(selectedSettings);
            var rows=new System.Collections.Generic.List<string>();
            if(selectedSettings.Action==SlotAction.Marker)rows.Add(L.T("Inserisce un marker nella traccia selezionata in riproduzione, solo in Setup."));
            if(selectedSettings.EmergencyPauseAll)rows.Add(L.T("EMERGENZA: pausa tutte le tracce"));
            if(SlotActions.Midi(selectedSettings.Action))foreach(var m in SlotActions.MidiList(selectedSettings).Take(2))rows.Add($"MIDI · {m.Kind} · CH {m.Channel} · "+(m.Kind==MidiKind.ControlChange?$"CC {m.Controller} = {m.ControlValue}":m.Kind==MidiKind.Note?$"NOTE {m.Note}":m.Kind==MidiKind.ProgramChange?$"PC {m.Program}":m.Cue));
            if(SlotActions.Osc(selectedSettings.Action))foreach(var o in selectedSettings.OscCommands.Take(2))rows.Add($"OSC · {o.Host}:{o.Port} · {o.Address}");
            commandDetails.Text=string.Join("\n",rows);commandDetails.ToolTip=commandDetails.Text;
        }
    }
}
