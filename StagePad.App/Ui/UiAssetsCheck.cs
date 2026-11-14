using System.Windows;
using StagePad.Core;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace StagePad.App;
// Executed by Build.ps1 on Windows against the published single-file executable.
// This catches runtime Freezable/resource failures that cross-compilation cannot.
internal static class UiAssetsCheck
{
    private static IEnumerable<DependencyObject> LogicalChildren(DependencyObject parent)
    {
        foreach(object child in LogicalTreeHelper.GetChildren(parent))
            if(child is DependencyObject dependency){yield return dependency;foreach(var descendant in LogicalChildren(dependency))yield return descendant;}
    }
    public static int Run()
    {
        AppUpdates.ValidateBinary(Environment.ProcessPath!,AppUpdates.Current);
        var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/StagePad;component/Ui/Theme.xaml",UriKind.Absolute)});
        var option=new System.Windows.Controls.CheckBox{Content="Dark themed option",IsChecked=true,Width=240};
        option.Measure(new Size(240,50));option.Arrange(new Rect(0,0,240,50));option.UpdateLayout();
        if(option.Template.FindName("Box",option) is not System.Windows.Controls.Border optionBox || optionBox.CornerRadius.TopLeft!=5 || optionBox.Background is not SolidColorBrush optionFill || optionFill.Color!=Color.FromRgb(0x75,0xE2,0xC1))throw new InvalidOperationException("Checkbox does not use the rounded dark-theme template.");
        if(option.Template.FindName("Tick",option) is not System.Windows.Shapes.Path optionTick || optionTick.Visibility!=Visibility.Visible)throw new InvalidOperationException("Checked checkbox glyph is not visible.");
        option.IsChecked=false;option.UpdateLayout();if(optionTick.Visibility!=Visibility.Collapsed)throw new InvalidOperationException("Unchecked checkbox glyph remains visible.");
        option.IsThreeState=true;option.IsChecked=null;option.UpdateLayout();
        if(option.Template.FindName("Mixed",option) is not System.Windows.Controls.Border mixed || mixed.Visibility!=Visibility.Visible)throw new InvalidOperationException("Indeterminate checkbox glyph is not visible.");
        var logo=LogoImage.Create();
        if(!logo.IsFrozen || logo.Width<=0 || logo.Height<=0)throw new InvalidOperationException("Invalid vector logo.");
        foreach(int size in new[]{16,32,64,256})
        {
            var visual=new DrawingVisual();
            using(var drawing=visual.RenderOpen())drawing.DrawImage(logo,new Rect(0,0,size,size));
            var bitmap=new RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
            byte[] pixels=new byte[size*size*4];bitmap.CopyPixels(pixels,size*4,0);
            int white=0;
            for(int i=0;i<pixels.Length;i+=4)if(pixels[i]>240 && pixels[i+1]>240 && pixels[i+2]>240 && pixels[i+3]>240)white++;
            if(white<size*size/10)throw new InvalidOperationException("Logo rasterization failed.");
        }
        var settings=new SlotSettings {EmergencyPauseAll=true,Action=SlotAction.All,MidiCommands=[new(){Kind=MidiKind.ControlChange,Controller=7,ControlValue=63},new(){Kind=MidiKind.ProgramChange,Program=12}],OscCommands=[new(){Arguments=[new(OscArgumentType.Float,"0.5"),new(OscArgumentType.True)]}]};
        var editor=new SlotEditor(0,settings,_=>Task.FromResult(new Reply(true)));
        try
        {
            var restored=editor.Read();
            if(!restored.EmergencyPauseAll || restored.Action!=SlotAction.All || restored.MidiCommands?.Length!=2 || restored.MidiCommands[0].ControlValue!=63 || restored.OscCommands.Length!=1 || restored.OscCommands[0].Arguments[0].Value!="0.5")throw new InvalidOperationException("Multi-output editor roundtrip failed.");
            new WindowInteropHelper(editor).EnsureHandle();
        }
        finally{editor.Close();}
        var controls=new SettingsWindow(_=>Task.FromResult(new Reply(true)),()=>Task.CompletedTask,_=>Task.FromResult(new Reply(true)));
        controls.Update(new("check","Audio pronto",null,null,40,1,0,0,false,[],[],Project:new(){MasterControl=new(2,7,MidiControlMode.RelativeTwosComplement,true,.02f),CurrentTrackControl=new(3,11)}));
        if(controls.ReadBindings()!=(new MidiCcBinding(2,7,MidiControlMode.RelativeTwosComplement,true,.02f),new MidiCcBinding(3,11)))throw new InvalidOperationException("MIDI volume settings roundtrip failed.");
        controls.Close();
        var markerMidiEditor=new MidiCommandEditor(new(){DelayMs=500,Behavior=NoteBehavior.Gate},()=>{},markerTrigger:true);
        if(markerMidiEditor.Read().DelayMs!=0 || markerMidiEditor.Read().Behavior!=NoteBehavior.Pulse)throw new InvalidOperationException("Marker MIDI trigger must be immediate with automatic Note Off.");
        var markerOscEditor=new OscCommandEditor(new(){DelayMs=500,Arguments=[new(OscArgumentType.Int,"7")]},()=>{},markerTrigger:true);
        if(markerOscEditor.Read().DelayMs!=0 || markerOscEditor.Read().Arguments.Single().Value!="7")throw new InvalidOperationException("Marker OSC editor roundtrip failed.");
        var waveformCheck=new WaveformView{Width=420,Height=82};waveformCheck.SetPeaks([0,.5f,1,.5f,0]);waveformCheck.SetMarkers([new(){Seconds=1,Label="Ingresso",Color="#AA88FF"}],5);
        waveformCheck.Measure(new Size(420,82));waveformCheck.Arrange(new Rect(0,0,420,82));var waveformBitmap=new RenderTargetBitmap(420,82,96,96,PixelFormats.Pbgra32);waveformBitmap.Render(waveformCheck);
        var emptyWave=new WaveformView{Width=180,Height=72};
        emptyWave.SetPeaks([.5f,.5f]);emptyWave.Measure(new Size(180,72));emptyWave.Arrange(new Rect(0,0,180,72));
        var populated=new RenderTargetBitmap(180,72,96,96,PixelFormats.Pbgra32);populated.Render(emptyWave);
        emptyWave.SetPeaks([]);emptyWave.UpdateLayout();var emptyBitmap=new RenderTargetBitmap(180,72,96,96,PixelFormats.Pbgra32);emptyBitmap.Render(emptyWave);
        byte[] emptyPixels=new byte[180*72*4];emptyBitmap.CopyPixels(emptyPixels,180*4,0);
        for(int y=25;y<70;y++)
        {
            int offset=y*180*4;
            if(emptyPixels[offset]!=0x28 || emptyPixels[offset+1]!=0x1B || emptyPixels[offset+2]!=0x10)throw new InvalidOperationException("Empty player still draws a cursor or stale waveform.");
        }
        var scrollCheck=new System.Windows.Controls.ScrollViewer{Width=210,Height=130,HorizontalScrollBarVisibility=System.Windows.Controls.ScrollBarVisibility.Auto,VerticalScrollBarVisibility=System.Windows.Controls.ScrollBarVisibility.Auto,Content=new System.Windows.Controls.Border{Width=600,Height=600,Background=Brushes.Transparent}};
        scrollCheck.Measure(new Size(210,130));scrollCheck.Arrange(new Rect(0,0,210,130));scrollCheck.UpdateLayout();
        var scrollBitmap=new RenderTargetBitmap(210,130,96,96,PixelFormats.Pbgra32);scrollBitmap.Render(scrollCheck);byte[] scrollPixels=new byte[210*130*4];scrollBitmap.CopyPixels(scrollPixels,210*4,0);
        int corner=(128*210+208)*4;
        if(scrollPixels[corner]!=0x28 || scrollPixels[corner+1]!=0x1B || scrollPixels[corner+2]!=0x10)throw new InvalidOperationException("ScrollViewer corner does not use the dark application surface.");
        scrollCheck.ScrollToHorizontalOffset(100);scrollCheck.ScrollToVerticalOffset(100);scrollCheck.UpdateLayout();
        if(scrollCheck.HorizontalOffset<=0 || scrollCheck.VerticalOffset<=0)throw new InvalidOperationException("ScrollViewer scrolling stopped working.");
        foreach(var sample in new[]{new float[]{.5f},new float[]{.5f,.5f,.5f}})
        {
            var resizableWave=new WaveformView();resizableWave.SetPeaks(sample);
            foreach(var size in new[]{new Size(250,72),new Size(900,72),new Size(400,120)})
            {
                resizableWave.Width=size.Width;resizableWave.Height=size.Height;resizableWave.Measure(size);resizableWave.Arrange(new Rect(size));resizableWave.UpdateLayout();
                var bitmap=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(resizableWave);
                byte[] pixels=new byte[(int)(size.Width*size.Height)*4];bitmap.CopyPixels(pixels,(int)size.Width*4,0);
                int x=(int)(size.Width*.95),y=(int)(23+(size.Height-23)/2),offset=(y*(int)size.Width+x)*4;
                if(pixels[offset]!=0xB1 || pixels[offset+1]!=0xB9 || pixels[offset+2]!=0x75)throw new InvalidOperationException("Waveform does not fill the resized viewport.");
            }
        }
        var updateDialog=new UpdateWindow(_=>Task.FromResult(new Reply(true)),Environment.ProcessId);new WindowInteropHelper(updateDialog).EnsureHandle();updateDialog.Close();
        foreach(bool saveMode in new[]{false,true})
        {
            var browser=new ProjectBrowser(_=>Task.FromResult(new Reply(true)),()=>{},saveMode,null);
            browser.Update(new("check","Audio pronto",null,null,40,1,0,0,false,[],[],Project:new(),ProjectDirty:true));
            foreach(var size in new[]{new Size(740,500),new Size(420,260)}){browser.Measure(size);browser.Arrange(new Rect(size));browser.UpdateLayout();}
        }
        var markerState=new StateInfo("check","Audio pronto",null,null,40,1,0,0,false,[],[new(0,"Check","Ready",0,5,1,0,null,false)],Project:new(){Slots=[new(){Markers=[new(){Seconds=1,Label="Ingresso"}]}],SlotCount=1});
        var markerDialog=new MarkerWindow(0,markerState,0,null,_=>Task.FromResult(new Reply(true)),_=>Task.CompletedTask);
        new WindowInteropHelper(markerDialog).EnsureHandle();markerDialog.Update(markerState with{ShowLocked=true});markerDialog.Close();
        var a=new AudioMarker{Seconds=1,Label="Loop start"};var b=new AudioMarker{Seconds=4,Label="Loop end"};
        var appliedLoop=new MarkerLoop(true,a.Id,b.Id);
        var captureState=markerState with{Project=new(){Slots=[new(){Markers=[a,b],MarkerLoop=appliedLoop}],SlotCount=1},Slots=[markerState.Slots[0] with{Position=2.5}]};
        Task<Reply> CaptureCommand(Request command)
        {
            if(command.Op=="marker-add")
            {
                var setting=captureState.Project!.Slots[0];
                captureState=captureState with{Project=captureState.Project with{Slots=[setting with{Markers=AudioMarkers.Change(setting.Markers,command.Op,command.Marker,null,5)}]}};
            }
            return Task.FromResult(new Reply(true,State:captureState));
        }
        var captureDialog=new MarkerWindow(0,captureState with{Slots=[captureState.Slots[0] with{Position=1}]},1,null,CaptureCommand,_=>Task.CompletedTask);
        captureDialog.Update(captureState);
        if(!LogicalChildren(captureDialog).OfType<System.Windows.Controls.TextBox>().Any(c=>c.Text==AudioMarkers.Format(2.5)))throw new InvalidOperationException("New marker position does not follow the cursor.");
        captureDialog.AddAt(2.5).GetAwaiter().GetResult();
        if(captureState.Project!.Slots[0].MarkerLoop!=appliedLoop || !LogicalChildren(captureDialog).OfType<System.Windows.Controls.ListBox>().Any(c=>c.Items.Count==3))throw new InvalidOperationException("Quick marker insertion after applying loop did not refresh the list.");
        captureState=captureState with{Slots=[captureState.Slots[0] with{Position=2.75}]};captureDialog.Update(captureState);
        if(!LogicalChildren(captureDialog).OfType<System.Windows.Controls.TextBox>().Any(c=>c.Text==AudioMarkers.Format(2.75)))throw new InvalidOperationException("Marker position field did not follow a cursor move while the editor was open.");
        if(LogicalChildren(captureDialog).OfType<System.Windows.Controls.CheckBox>().Any())throw new InvalidOperationException("Obsolete loop checkbox remains in marker editor.");
        captureDialog.Close();
        string signalSuffix="-test-"+Guid.NewGuid().ToString("N");
        using(var closeSignal=LifecycleSignals.Create(signalSuffix))
        {
            var main=new MainWindow("ui-check-no-transport",Environment.ProcessId,()=>LifecycleSignals.RequestClose(signalSuffix));
            new WindowInteropHelper(main).EnsureHandle();
            main.Close();
            if(!closeSignal.WaitOne(100))throw new InvalidOperationException("Closing the main window did not signal shutdown.");
        }
        var window=new Window();
        try
        {
            WindowAppearance.Apply(window);
            if(window.Icon is not BitmapSource icon || icon.PixelWidth<512)throw new InvalidOperationException("Window PNG icon is missing.");
            if(new WindowInteropHelper(window).EnsureHandle()==IntPtr.Zero)throw new InvalidOperationException("Window creation failed.");
        }
        finally{window.Close();app.Shutdown();}
        System.IO.Directory.CreateDirectory(Log.Root);
        System.IO.File.WriteAllText(System.IO.Path.Combine(Log.Root,"ui-check-success.json"),System.Text.Json.JsonSerializer.Serialize(new{version=AppUpdates.Current.ToString(3),passed=true,platform=Environment.OSVersion.ToString(),checks=new[]{"themed checkbox checked/unchecked/indeterminate","logo rasterization","slot editor roundtrip","MIDI volume settings","marker trigger editors","empty player without cursor","dark scrollbar corner","two-axis scrolling","waveform resize","project browser layout","marker window","quick marker insertion in applied loop","cursor-following marker field","loop controls without checkbox","close signals","window icon"}}));
        return 0;
    }
}
