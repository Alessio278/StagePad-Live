using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace StagePad.App;
internal static class ScreenshotProgram
{
    private sealed record Shot(string File,string Description,int Width,int Height);
    private static readonly List<Shot> shots=[];
    private static string output="",session="",language="it";
    private static StateInfo? state;
    [STAThread]
    public static int Main(string[] args)
    {
        if(args.Length!=2 || !new[]{"it","en","es","fr"}.Contains(args[1]))return 2;
        output=Path.GetFullPath(args[0]);language=args[1];Directory.CreateDirectory(output);
        session=Path.Combine(Path.GetTempPath(),"StagePad-Screenshots-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(session);
        Environment.SetEnvironmentVariable("STAGEPAD_DATA_DIR",session);
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/StagePad;component/Ui/Theme.xaml",UriKind.Absolute)});
        Wire.TestRoute=(_,_)=>Task.FromResult(new Reply(true,State:state));
        app.DispatcherUnhandledException+=(_,e)=>{e.Handled=true;Failure(e.Exception);app.Shutdown(1);};
        app.Startup+=async(_,_)=>
        {
            try{await Capture();app.Shutdown(0);}
            catch(Exception ex){Failure(ex);app.Shutdown(1);}
        };
        try{return app.Run();}
        finally
        {
            foreach(Window w in app.Windows.Cast<Window>().ToArray())try{w.Close();}catch{}
            try{Directory.Delete(session,true);}catch{}
        }
    }
    private static void Failure(Exception ex)
    {
        try{File.WriteAllText(Path.Combine(output,"capture-result.json"),JsonSerializer.Serialize(new{success=false,message=Mask(ex.Message),screenshots=shots.Count}));}catch{}
    }
    private static Task<Reply> Safe(Request _)=>Task.FromResult(new Reply(true,State:state));
    private static async Task Capture()
    {
        var data=DemoData.Create(session,language);L.Initialize();L.SetProjectDirectory(Path.GetDirectoryName(data.ShowPath));L.SetAutoSaveProjects(false);L.Change(language);
        var main=new MainWindow("screenshot-demo-no-engine",Environment.ProcessId,()=>{});main.PrepareScreenshotWindow();
        await Show(main,1500,1000);
        state=data.State();main.PresentScreenshot(state,false,0,data.Peaks);await Ready(main);
        Save(main,"01-setup.png","Setup - tracce, comandi e colori");
        state=data.State(live:true);main.PresentScreenshot(state,true,0,data.Peaks);await Ready(main);
        Save(main,"02-live.png","Serata - pad popolati e riscontro dei comandi");
        state=data.State();main.PresentScreenshot(state,false,0,data.Peaks);await Ready(main);
        Save(main.ScreenshotPlayer,"03-player-marker.png","Player - forma d’onda e marker colorati");
        state=data.State(loop:true);main.PresentScreenshot(state,false,0,data.Peaks);await Ready(main);
        Save(main.ScreenshotPlayer,"04-player-loop.png","Player - loop fra due marker");
        state=data.State(loop:true);
        var markers=new MarkerWindow(0,state,12.4,"22222222222222222222222222222222",Safe,_=>Task.CompletedTask){Owner=main};
        await Show(markers,700,900);Save(markers,"05-markers.png","Gestione marker - etichetta, colore e loop");markers.Close();
        foreach(var item in new[]{(Slot:0,File:"06-slot-audio.png",Label:"Slot audio",Scroll:0.0),(Slot:6,File:"07-slot-midi.png",Label:"Slot MIDI - sequenza e destinazione",Scroll:390.0),(Slot:7,File:"08-slot-osc.png",Label:"Slot OSC - host, percorso e argomenti",Scroll:390.0),(Slot:15,File:"09-emergency.png",Label:"Emergenza - protezione di tre secondi",Scroll:0.0),(Slot:14,File:"10-slot-marker.png",Label:"Slot + Marker - azione esclusiva",Scroll:0.0)})
        {
            var editor=new SlotEditor(item.Slot,data.Project.Slots[item.Slot],_=>Task.FromResult(new Reply(true)),Safe,data.Project.CommandDefaults){Owner=main};
            await Show(editor,650,950);
            var scroll=Nodes(editor).OfType<ScrollViewer>().FirstOrDefault(s=>s.Content is StackPanel);
            if(scroll!=null){scroll.ScrollToVerticalOffset(item.Scroll);await Ready(editor);}
            Save(editor,item.File,item.Label);editor.Close();
        }
        var settings=new SettingsWindow(_=>Task.FromResult(new Reply(true)),()=>Task.CompletedTask,Safe,()=>Task.CompletedTask){Owner=main};
        await Show(settings,760,900);state=data.State();settings.Update(state);await Ready(settings);
        Save(settings,"11-settings.png","Impostazioni - lingua e destinazioni predefinite");
        if(typeof(SettingsWindow).GetField("volumeControls",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(settings) is FrameworkElement volumes)
        {
            var scroll=Nodes(settings).OfType<ScrollViewer>().First(s=>s.Content is StackPanel);
            var origin=volumes.TranslatePoint(new Point(),(UIElement)scroll.Content);
            scroll.ScrollToVerticalOffset(Math.Max(0,origin.Y-36));await Ready(settings);
        }
        Save(settings,"12-midi-volumes.png","Impostazioni - master e traccia con LEARN");settings.Close();
        state=data.State();main.PresentScreenshot(state,false,0,data.Peaks);await main.ScreenshotProjects();await Ready(main);
        Save(main,"13-shows.png","Gestione progetti - elenco show dimostrativi");main.Close();
        if(shots.Count!=13)throw new InvalidOperationException("Incomplete capture set.");
        File.WriteAllText(Path.Combine(output,"capture-result.json"),JsonSerializer.Serialize(new{success=true,language,mode="isolated-demo",content="native-wpf-client-areas",scale=2,screenshots=shots},new JsonSerializerOptions{WriteIndented=true}));
        string title=WebUtility.HtmlEncode(DemoText.Get(language,"title"));
        var html=new StringBuilder($"<!doctype html><html lang=\"{language}\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>{title}</title><style>body{{margin:0;padding:32px;background:#0b111a;color:#edf4fc;font:16px system-ui}}main{{max-width:1500px;margin:auto}}h1{{color:#75e2c1}}figure{{margin:32px 0}}img{{display:block;max-width:100%;height:auto;border:1px solid #293c51;border-radius:12px}}figcaption{{padding:10px 0;color:#aec2d7}}a{{color:#75e2c1}}</style><main><h1>{title}</h1><p>{WebUtility.HtmlEncode(DemoText.Get(language,"notice"))}</p>");
        foreach(var shot in shots)html.Append($"<figure><a href=\"{WebUtility.HtmlEncode(shot.File)}\"><img loading=\"lazy\" src=\"{WebUtility.HtmlEncode(shot.File)}\" alt=\"{WebUtility.HtmlEncode(shot.Description)}\"></a><figcaption>{WebUtility.HtmlEncode(shot.Description)}</figcaption></figure>");
        File.WriteAllText(Path.Combine(output,"index.html"),html+"</main></html>");
        File.WriteAllText(Path.Combine(output,"README.md"),"# "+DemoText.Get(language,"title")+"\n\n"+DemoText.Get(language,"readme")+"\n\n"+DemoText.Get(language,"notice")+"\n\n"+string.Join("\n",shots.Select(shot=>$"- [{shot.Description}]({shot.File})"))+"\n");
    }
    private static async Task Show(Window window,double width,double height)
    {
        window.IsHitTestVisible=false;window.Focusable=false;window.WindowStartupLocation=WindowStartupLocation.Manual;
        window.Left=12;window.Top=12;window.Show();window.WindowState=WindowState.Normal;window.Width=width;window.Height=height;await Ready(window);
        window.Width=width;window.Height=height;await Ready(window);
    }
    private static async Task Ready(Window window)
    {
        await window.Dispatcher.InvokeAsync(()=>window.UpdateLayout(),DispatcherPriority.Loaded);
        await Task.Delay(180);
        await window.Dispatcher.InvokeAsync(()=>window.UpdateLayout(),DispatcherPriority.Render);
    }
    private static IEnumerable<DependencyObject> Nodes(DependencyObject root)
    {
        yield return root;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var item in Nodes(child))yield return item;
    }
    private static string Mask(string text)=>text.Replace(session,@"%LOCALAPPDATA%\StagePad\demo",StringComparison.OrdinalIgnoreCase);
    private static void Save(Window window,string file,string description)
    {
        if(window.Content is not FrameworkElement content)throw new InvalidOperationException("Window content unavailable.");
        Save(content,file,description);
    }
    private static void Save(FrameworkElement content,string file,string description)
    {
        foreach(var item in Nodes(content))
        {
            if(item is TextBlock block && block.Text.Contains(session,StringComparison.OrdinalIgnoreCase))block.Text=Mask(block.Text);
            else if(item is TextBox text && text.Text.Contains(session,StringComparison.OrdinalIgnoreCase))text.Text=Mask(text.Text);
        }
        content.UpdateLayout();double width=content.ActualWidth,height=content.ActualHeight;
        if(width<80 || height<60 || !double.IsFinite(width+height))throw new InvalidOperationException("Invalid capture dimensions: "+file);
        const int scale=2;int pixelWidth=(int)Math.Ceiling(width*scale),pixelHeight=(int)Math.Ceiling(height*scale);
        var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(11,17,26)),null,new Rect(0,0,width,height));
            dc.DrawRectangle(new VisualBrush(content){Stretch=Stretch.Fill},null,new Rect(0,0,width,height));
        }
        var bitmap=new RenderTargetBitmap(pixelWidth,pixelHeight,96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(drawing);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var fileStream=File.Create(Path.Combine(output,file)))encoder.Save(fileStream);
        shots.Add(new(file,DemoText.Get(language,file),pixelWidth,pixelHeight));
    }
}
