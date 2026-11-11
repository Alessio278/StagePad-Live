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
    public static int Run()
    {
        AppUpdates.ValidateBinary(Environment.ProcessPath!,AppUpdates.Current);
        var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/StagePad;component/Ui/Theme.xaml",UriKind.Absolute)});
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
        return 0;
    }
}
