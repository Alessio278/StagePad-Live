using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StagePad.Core;

namespace StagePad.App;
internal sealed partial class MainWindow
{
    private UpdateWindow? updatesWindow;
    private Task OpenUpdates()
    {
        if(updatesWindow==null){updatesWindow=new UpdateWindow(SendReply,parent){Owner=this};updatesWindow.Closed+=(_,_)=>updatesWindow=null;updatesWindow.Show();}else updatesWindow.Activate();return Task.CompletedTask;
    }
    private readonly WaveformView waveform = new() { Height = 40, Margin = new Thickness(0,3,0,3) };
    private readonly ComboBox midiInput = new() { MinWidth = 200, DisplayMemberPath = "Name", SelectedValuePath = "Id" };
    private readonly ComboBox midiOutput = new() { MinWidth = 200, DisplayMemberPath = "Name", SelectedValuePath = "Id" };
    private readonly TextBlock masterPercent = Text("100%", 16);
    private Button? lockSwitch;
    private RoutingSettings? lastRoute;
    private long seenTrigger, uiBeat = Environment.TickCount64;
    private int waveSlot = -1, waveRevision = -1;
    private bool refreshingWave;
    public long UiBeat => Volatile.Read(ref uiBeat);
    private async Task SendRouting(bool includeAudio = true)
    {
        if (last?.Project == null) return;
        var old = last.Project.Routing;
        var route = old with
        {
            AudioDevice = includeAudio ? (outputs.SelectedItem as DeviceInfo)?.Id ?? old.AudioDevice : old.AudioDevice,
            BufferMs = includeAudio ? (int?)latency.SelectedItem ?? old.BufferMs : old.BufferMs,
            MidiInput = string.IsNullOrEmpty(midiInput.SelectedValue as string) ? null : midiInput.SelectedValue as string,
            MidiOutput = string.IsNullOrEmpty(midiOutput.SelectedValue as string) ? null : midiOutput.SelectedValue as string
        };
        await Send(new("configure", Routing: route));
    }
    private void EditSlot(int slot)
    {
        Select(slot);
        if (last?.ShowLocked == true) { Warn("SHOW LOCK: impostazioni bloccate."); return; }
        if (last?.Project == null) return;
        var editor = new SlotEditor(slot, last.Project.Slots[slot], settings => SendReply(new("settings", slot, Settings: settings)),SendReply) { Owner = this };
        SlotEditorPosition.Attach(editor,liveView?liveCards[slot]:cards[slot]);
        editor.Show();
    }
    private ProjectBrowser? projectBrowser;
    private Task OpenProject()
    {
        if(last?.ShowLocked==true){Warn("SHOW LOCK: apertura bloccata.");return Task.CompletedTask;}
        ShowProjectBrowser(false);return Task.CompletedTask;
    }
    private Task SaveProject(){ShowProjectBrowser(true);return Task.CompletedTask;}
    private void ShowProjectBrowser(bool saveMode)
    {
        SetView(false);
        ProjectBrowser? browser=null;
        browser=new ProjectBrowser(SendReply,()=>{if(ReferenceEquals(projectBrowser,browser))SetView(false);},saveMode,last?.ProjectPath);
        projectBrowser=browser;viewHost.Content=browser;if(gridToolbar!=null)gridToolbar.Visibility=Visibility.Collapsed;
        if(last!=null)browser.Update(last);
    }
    private async void ProjectDirectoryChanged()
    {
        if(projectBrowser!=null)await projectBrowser.Refresh();
    }
    private Button PadButton(string text, int slot)
    {
        // Capture guarantees a release when the mouse is released outside the pad.
        var button = new Button { Content = text, Margin = new Thickness(2), Padding = new Thickness(9,5,9,5), Background = Brush("#1B2B3D"), Foreground = Brush("#EDF4FC"), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
        bool held = false;
        button.PreviewMouseLeftButtonDown += async (_, e) =>
        {
            e.Handled = true; if (held) return;
            held = true; PressFeedback(button,true); button.CaptureMouse(); Select(slot); await Send(new("pad-down", slot));
        };
        button.PreviewMouseLeftButtonUp += async (_, e) =>
        {
            e.Handled = true; if (!held) return;
            held = false; PressFeedback(button,false); button.ReleaseMouseCapture(); await Send(new("pad-up", slot));
        };
        button.LostMouseCapture += async (_, _) => { if (held) { held = false; PressFeedback(button,false); await Send(new("pad-up", slot)); } };
        button.PreviewKeyDown += async (_, e) =>
        {
            if (e.Key is not (Key.Space or Key.Enter)) return;
            e.Handled = true; if (e.IsRepeat || held) return;
            held = true; PressFeedback(button,true); Select(slot); await Send(new("pad-down", slot));
        };
        button.PreviewKeyUp += async (_, e) =>
        {
            if (e.Key is not (Key.Space or Key.Enter)) return;
            e.Handled = true; if (!held) return;
            held = false; PressFeedback(button,false); await Send(new("pad-up", slot));
        };
        button.LostKeyboardFocus += async (_, _) => { if (held && !button.IsMouseCaptured) { held = false; PressFeedback(button,false); await Send(new("pad-up", slot)); } };
        return button;
    }
    private void ShowFeatures(StateInfo s)
    {
        if(s.Project!=null)ApplyLayouts(s.Project);
        string title=s.ProjectPath!=null?System.IO.Path.GetFileNameWithoutExtension(s.ProjectPath):s.Project?.Name??"Nuovo progetto";
        if(title=="Nuovo progetto")title=L.T("Senza nome");
        showName.Text="Show: "+title;showName.ToolTip=showName.Text;
        if (lockSwitch != null) { lockSwitch.Content=L.T(s.ShowLocked?"● LOCK ATTIVO":"SHOW LOCK");lockSwitch.Background=Brush(s.ShowLocked?"#493B25":"#1B2B3D");lockSwitch.Foreground=Brush(s.ShowLocked?"#FFCC80":"#B6C9DE"); }
        if(sidebarSettings!=null)sidebarSettings.IsEnabled=!s.ShowLocked;
        if(gridToolbar!=null)gridToolbar.IsEnabled=!s.ShowLocked;
        inputBadge.Text="● MIDI IN · "+PortLabel(s.MidiIn,false);outputBadge.Text="● MIDI OUT · "+PortLabel(s.MidiOut,true);
        inputBadge.Foreground=Brush(PortColor(s.MidiIn));outputBadge.Foreground=Brush(PortColor(s.MidiOut));
        settingsWindow?.Update(s);projectBrowser?.Update(s);
        bool routeChanged = s.Project != null && s.Project.Routing != lastRoute;
        if (routeChanged) { lastRoute = s.Project!.Routing; latency.SelectedItem = lastRoute.BufferMs; outputs.SelectedValue = lastRoute.AudioDevice ?? s.DeviceId; }
        FillPorts(midiInput, s.MidiIn?.Devices ?? [], s.Project?.Routing.MidiInput, routeChanged);
        FillPorts(midiOutput, s.MidiOut?.Devices ?? [], s.Project?.Routing.MidiOutput, routeChanged);
        if(!master.IsMouseCaptureWithin && !master.IsKeyboardFocusWithin)masterPercent.Text = $"{s.Master * 100:0}%";
    }
    private static bool PortReady(MidiPortState? port)=>port?.Status=="Connesso" || port?.Status.StartsWith("Porta aperta",StringComparison.Ordinal)==true;
    private static string PortLabel(MidiPortState? port,bool output)=>L.T(port==null?"AVVIO":port.Error!=null?"ERRORE":port.Selected==null?"OFF":PortReady(port)?(output?"APERTO":"CONNESSO"):"ATTESA");
    private static string PortColor(MidiPortState? port)=>port?.Error!=null?"#FFCC80":PortReady(port)?"#75E2C1":"#91A6BD";
    private static void FillPorts(ComboBox combo, DeviceInfo[] ports, string? selected, bool sync)
    {
        var options = new List<DeviceInfo> { new("", L.T("Disabilitato")) }; options.AddRange(ports);
        if (selected != null && !ports.Any(p => p.Id == selected)) options.Add(new(selected, L.T("Non disponibile · ") + selected));
        var next = options.ToArray();
        if (combo.ItemsSource is DeviceInfo[] old && old.SequenceEqual(next)) { if (sync) combo.SelectedValue = selected ?? ""; return; }
        string? draft = sync ? null : combo.SelectedValue as string;
        combo.ItemsSource = next; combo.SelectedValue = draft != null && next.Any(x => x.Id == draft) ? draft : selected ?? "";
    }
    private async Task RefreshWave(StateInfo s)
    {
        if (refreshingWave || (waveSlot == current && waveRevision == s.Slots[current].Revision && !s.Slots[current].Loading)) return;
        if (s.Slots[current].Loading) return;
        refreshingWave = true; int slot = current, revision = s.Slots[current].Revision;
        try
        {
            var reply = await Wire.Call(pipe, new("wave", slot), 1000);
            if (current == slot) { waveform.SetPeaks(reply.Peaks ?? []); waveSlot = slot; waveRevision = revision; }
        }
        catch (Exception ex) { Log.Warn("wave-ui", ex.Message); }
        finally { refreshingWave = false; }
    }
}
