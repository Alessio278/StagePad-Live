using System.Diagnostics;
using System.IO;
using System.Windows;

namespace StagePad.App;
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        string role = args.FirstOrDefault() ?? "supervisor";
        string normalRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"StagePad");
        if(Environment.GetEnvironmentVariable("STAGEPAD_DATA_DIR") is {} custom && Path.GetFullPath(custom).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.Combine(normalRoot,"preview-markers"),StringComparison.OrdinalIgnoreCase))Environment.SetEnvironmentVariable("STAGEPAD_DATA_DIR",normalRoot);
        Log.Start(role.TrimStart('-'));
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Warn("fatal", e.ExceptionObject.ToString() ?? "Errore fatale");
        TaskScheduler.UnobservedTaskException += (_, e) => { Log.Error("task", e.Exception); e.SetObserved(); };
        try
        {
            return role switch
            {
                "--apply-update" => UpdateExecutor.Run(args),
                "--post-update" => Supervisor(args[1]),
                "--check-ui-assets" => UiAssetsCheck.Run(),
                "--decode" => DecodeMain(args[1], args[2], int.Parse(args[3])),
                "--midi-in" => MidiMain(args[1], int.Parse(args[2]), true),
                "--midi-out" => MidiMain(args[1], int.Parse(args[2]), false),
                "--audio" => AudioMain(args[1], int.Parse(args[2])),
                "--ui" => UiMain(args[1], int.Parse(args[2])),
                _ => Supervisor()
            };
        }
        catch(Exception ex)
        {
            Log.Error("main",ex);
            if(role=="--check-ui-assets")try{Directory.CreateDirectory(Log.Root);File.WriteAllText(Path.Combine(Log.Root,"ui-check-error.txt"),ex.ToString());}catch{}
            return 2;
        }
    }
    private static int DecodeMain(string original, string cache, int parent)
    {
        using var parentCheck = new Timer(_ => { if (!Alive(parent)) Environment.Exit(3); }, null, 2000, 2000);
        try
        {
            Exception? failure = null;
            var decoder = new Thread(() =>
            {
                try { Importer.Decode(original, cache, CancellationToken.None); }
                catch (Exception ex) { failure = ex; }
            }) { IsBackground = true, Name = "StagePad isolated decoder" };
            decoder.SetApartmentState(ApartmentState.MTA); decoder.Start(); decoder.Join();
            if (failure != null) throw new InvalidOperationException(failure.Message, failure);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("decode", ex);
            try { File.WriteAllText(cache + ".error", ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message); } catch { }
            return 2;
        }
    }
    private static Process Launch(string role, string pipe)
    {
        var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory };
        info.ArgumentList.Add(role); info.ArgumentList.Add(pipe); info.ArgumentList.Add(Environment.ProcessId.ToString());
        return Process.Start(info) ?? throw new InvalidOperationException("Impossibile avviare " + role);
    }
    private static int Supervisor(string? healthToken=null)
    {
        using var instance=SingleInstance.Enter();
        if(instance==null)return 0;
        using var previousPreview=new Mutex(false,"Local\\StagePad-Markers-Preview-"+Environment.UserName);
        bool legacyFree;try{legacyFree=previousPreview.WaitOne(0);}catch(AbandonedMutexException){legacyFree=true;}
        if(!legacyFree){MessageBox.Show("Chiudi la precedente applicazione prima di avviare StagePad.","StagePad");return 1;}
        try{LegacyDataMigration.Run(Log.Root);}catch(Exception ex){previousPreview.ReleaseMutex();Log.Error("data-migration",ex);MessageBox.Show("Recupero dei progetti non completato. I dati precedenti sono conservati.\n"+ex.Message,"StagePad");return 2;}
        using var closeSignal=LifecycleSignals.Create("-"+Environment.ProcessId);
        _=Task.Run(()=>
        {
            try
            {
                string cache=Path.Combine(Log.Root,"cache");
                if(Directory.Exists(cache))foreach(string folder in Directory.EnumerateDirectories(cache))
                    if(Directory.GetLastWriteTimeUtc(folder)<DateTime.UtcNow.AddDays(-1))Directory.Delete(folder,true);
            }
            catch(Exception ex){Log.Warn("cache-prune",ex.Message);}
        });
        string pipe = "stagepad-" + Guid.NewGuid().ToString("N");
        string[] roles = ["--audio", "--midi-in", "--midi-out", "--ui"];
        var processes = new Dictionary<string, Process>();
        var launched = new Dictionary<string, long>();
        var failures = roles.ToDictionary(r => r, _ => 0);
        var missingSince = roles.ToDictionary(r => r, _ => 0L);
        bool closing=false;long nextProbe=0;
        try
        {
            foreach(string role in roles){processes[role]=Launch(role,pipe);launched[role]=Environment.TickCount64;}
            while (!closing)
            {
                if(closeSignal.WaitOne(0))break;
                foreach (string role in roles)
                {
                    var child = processes[role];
                    if (!child.HasExited) continue;
                    if (role == "--ui" && (child.ExitCode == 0 || failures[role]>=2)) { closing = true; break; }
                    int backoff = Math.Min(30000, 1000 * Math.Max(1, failures[role]));
                    if (Environment.TickCount64 - launched[role] < backoff) continue;
                    Log.Warn("watchdog", $"Riavvio isolato {role}. Le tracce non vengono avviate automaticamente.");
                    try
                    {
                        var replacement = Launch(role, pipe); child.Dispose(); processes[role] = replacement;
                        launched[role] = Environment.TickCount64; missingSince[role] = 0; failures[role]++;
                    }
                    catch (Exception ex) { Log.Error("watchdog-restart", ex); launched[role] = Environment.TickCount64; failures[role]++; }
                }
                if (closing) break;
                if (Environment.TickCount64 >= nextProbe)
                {
                    nextProbe = Environment.TickCount64 + 1000;
                    string[] names = [pipe, pipe + ".in", pipe + ".out", pipe + ".ui"];
                    var replies = Task.WhenAll(names.Select(Probe)).GetAwaiter().GetResult();
                    if(healthToken!=null && replies[3]?.UiReady==true && replies[0]?.State?.Project!=null)
                    {UpdateExecutor.Healthy(healthToken);healthToken=null;}
                    for (int i = 0; i < roles.Length; i++)
                    {
                        string role = roles[i]; var child = processes[role];
                        if (child.HasExited || Environment.TickCount64 - launched[role] < 30000) continue;
                        long beat = i switch { 0 => replies[i]?.State?.Heartbeat ?? 0, 1 or 2 => replies[i]?.Midi?.Heartbeat ?? 0, _ => replies[i]?.UiBeat ?? 0 };
                        if (beat == 0)
                        {
                            if (missingSince[role] == 0) missingSince[role] = Environment.TickCount64;
                        }
                        else missingSince[role] = 0;
                        bool stale = beat == 0 ? Environment.TickCount64 - missingSince[role] > 30000 : Environment.TickCount64 - beat > 30000;
                        if (i == 0 && replies[i]?.State is { } state)
                        {
                            bool deviceStalled = Environment.TickCount64 - state.DeviceHeartbeat > 30000;
                            bool renderStalled = Environment.TickCount64 - state.RenderHeartbeat > 30000;
                            stale |= deviceStalled && renderStalled;
                        }
                        if (stale)
                        {
                            Log.Warn("watchdog", $"Heartbeat assente: recupero solo {role}.");
                            try { child.Kill(); child.WaitForExit(1000); }
                            catch (Exception ex) { Log.Warn("watchdog-kill", ex.Message); }
                        }
                        else if (Environment.TickCount64 - launched[role] > 60000) failures[role] = 0;
                    }
                }
                Thread.Sleep(100);
            }
        }
        finally
        {
            var shutdowns=new[]{pipe,pipe+".in",pipe+".out"}.Select(async name=>
            {try{await Wire.Call(name,new("shutdown"),500);}catch(Exception ex){Log.Warn("shutdown",ex.Message);}});
            Task.WhenAll(shutdowns).Wait(750);
            ChildProcessCleanup.StopAll(processes.Values);
            foreach(var child in processes.Values)child.Dispose();
            previousPreview.ReleaseMutex();
        }

        return 0;
    }
    private static async Task<Reply?> Probe(string pipe)
    { try { return await Wire.Call(pipe, new("state"), 600); } catch { return null; } }
    private static int MidiMain(string pipe, int parent, bool input)
    {
        using MidiModule module = input ? new MidiInputModule(pipe) : new MidiOutputModule(pipe);
        module.Start();
        using var parentCheck = new Timer(_ => { if (!Alive(parent)) ParentGone(()=>{_ = module.Handle(new("shutdown"));}); }, null, 2000, 2000);
        try { Wire.Serve(pipe + (input ? ".in" : ".out"), module.Handle, module.Shutdown).GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        return 0;
    }
    private static int parentLost;
    private static void ParentGone(Action stop)
    {
        if(Interlocked.Exchange(ref parentLost,1)!=0)return;
        stop();
        _=Task.Run(async()=>{await Task.Delay(2000);Environment.Exit(3);});
    }
    private static bool Alive(int pid)
    { try { using var p = Process.GetProcessById(pid); return !p.HasExited; } catch { return false; } }
    private static int AudioMain(string pipe, int parent)
    {
        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
        using var coordinator = new AudioCoordinator(pipe);
        using var parentCheck = new Timer(_ =>
        {
            if (!Alive(parent)) ParentGone(()=>{_ = coordinator.Handle(new("shutdown"));});
        }, null, 2000, 2000);
        try { Wire.Serve(pipe, coordinator.Handle, coordinator.Shutdown).GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        return 0;
    }
    private static int UiMain(string pipe, int parent)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/StagePad;component/Ui/Theme.xaml", UriKind.Absolute) });
        app.DispatcherUnhandledException += (_, e) =>
        {
            Log.Error("ui-dispatcher", e.Exception);
            // Do not continue with potentially corrupted UI state; supervisor restores it.
            e.Handled = true; Environment.Exit(2);
        };
        L.Initialize();
        var window = new MainWindow(pipe, parent);
        using var stop = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try { await Wire.Serve(pipe + ".ui", _ => Task.FromResult(new Reply(true, UiBeat: window.UiBeat,UiReady:window.Ready)), stop.Token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Log.Error("ui-heartbeat", ex); }
        });
        int result = app.Run(window); stop.Cancel(); return result;
    }
}
