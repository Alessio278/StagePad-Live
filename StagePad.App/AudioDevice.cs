using NAudio.CoreAudioApi;
using NAudio.Wave;
using StagePad.Core;

namespace StagePad.App;
internal sealed class AudioDevice : IDisposable
{
    private sealed class Provider(Mixer mixer) : IWaveProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(Mixer.Rate, 2);
        public int Read(byte[] buffer, int offset, int count)
        {
            // WASAPI always receives a complete block, including digital silence.
            var samples = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(buffer.AsSpan(offset, count));
            mixer.Render(samples); return count;
        }
    }
    private readonly Mixer mixer;
    private readonly CancellationTokenSource stop = new();
    private readonly Thread thread;
    private WasapiOut? output;
    private MMDevice? device;
    private string? requestedId, actualId, error;
    private string status = "Avvio audio";
    private DeviceInfo[] devices = [];
    private int requestedLatency = 40, version, openedVersion = -1, failed;
    private long callbacks;
    public long Heartbeat => Volatile.Read(ref callbacks);
    private readonly float[] maintenance = new float[2];
    public string Status => Volatile.Read(ref status);
    public string? Error => Volatile.Read(ref error);
    public string? DeviceId => Volatile.Read(ref actualId);
    public int Latency => Volatile.Read(ref requestedLatency);
    public DeviceInfo[] Devices => Volatile.Read(ref devices);
    public AudioDevice(Mixer mixer)
    {
        this.mixer = mixer; mixer.Freeze(true);
        thread = new Thread(Run) { IsBackground = true, Name = "StagePad audio device recovery" };
        thread.SetApartmentState(ApartmentState.MTA); thread.Start();
    }
    public void Configure(string? id, int latency)
    {
        Volatile.Write(ref requestedId, id);
        Volatile.Write(ref requestedLatency, latency);
        Interlocked.Increment(ref version);
    }
    private void Close()
    {
        mixer.Freeze(true);
        var old = output; output = null;
        if (old != null)
        {
            old.PlaybackStopped -= Stopped;
            try { old.Stop(); old.Dispose(); } catch (Exception ex) { Log.Error("audio-close", ex); }
        }
        try { device?.Dispose(); } catch (Exception ex) { Log.Error("audio-device-close", ex); } finally { device = null; }
    }
    private void Stopped(object? sender, StoppedEventArgs e)
    {
        // No device calls/logging here; hand recovery back to the device worker.
        if (e.Exception != null) Volatile.Write(ref error, e.Exception.Message);
        mixer.Freeze(true); Interlocked.Exchange(ref failed, 1);
    }
    private void Enumerate(MMDeviceEnumerator enumerator)
    {
        var result = new List<DeviceInfo>();
        foreach (var d in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            using (d) result.Add(new(d.ID, d.FriendlyName));
        Volatile.Write(ref devices, result.ToArray());
    }
    private void Run()
    {
        int retry = 0; DateTime nextRetry = DateTime.MinValue, nextScan = DateTime.MinValue;
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow >= nextScan)
                    {
                        Enumerate(enumerator); nextScan = DateTime.UtcNow.AddSeconds(2);
                        if (device != null && device.State != DeviceState.Active) Interlocked.Exchange(ref failed, 1);
                    }
                    if (openedVersion != Volatile.Read(ref version))
                    { Close(); openedVersion = Volatile.Read(ref version); nextRetry = DateTime.MinValue; retry = 0; }
                    if (Interlocked.Exchange(ref failed, 0) != 0)
                    {
                        Close(); Volatile.Write(ref status, "Uscita persa - riproduzione sospesa");
                        nextRetry = DateTime.UtcNow.AddSeconds(1);
                        Log.Warn("audio", Error ?? "Dispositivo rimosso");
                    }
                    if (output == null)
                    {
                        // Only this worker renders maintenance blocks, and only after the
                        // former output has stopped. Applies commands while no endpoint exists.
                        mixer.Render(maintenance);
                        if (DateTime.UtcNow >= nextRetry)
                        {
                            string? id = Volatile.Read(ref requestedId);
                            // Resolve default once. Never silently switch to another endpoint.
                            id ??= Volatile.Read(ref actualId);
                            device = id == null ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) : enumerator.GetDevice(id);
                            if (device.State != DeviceState.Active) throw new InvalidOperationException("L'uscita selezionata non è disponibile.");
                            Volatile.Write(ref actualId, device.ID);
                            var candidate = new WasapiOut(device, AudioClientShareMode.Shared, true, Latency);
                            output = candidate; candidate.PlaybackStopped += Stopped;
                            candidate.Init(new Provider(mixer));
                            mixer.Freeze(false); candidate.Play();
                            Volatile.Write(ref error, null); Volatile.Write(ref status, "Audio pronto · " + device.FriendlyName);
                            retry = 0;
                        }
                    }
                    // Counter used by the control heartbeat; never wait for UI or IPC here.
                    Volatile.Write(ref callbacks, Environment.TickCount64);
                }
                catch (Exception ex)
                {
                    Close(); Volatile.Write(ref error, ex.Message); Volatile.Write(ref status, "Audio non disponibile - nuovo tentativo");
                    Log.Error("audio-device", ex);
                    nextRetry = DateTime.UtcNow.AddSeconds(Math.Min(5, ++retry));
                    nextScan = DateTime.UtcNow.AddSeconds(2);
                }
                if (stop.Token.WaitHandle.WaitOne(10)) break;
            }
        }
        catch (Exception ex) { Volatile.Write(ref error, ex.Message); Volatile.Write(ref status, "Errore sottosistema audio"); Log.Error("audio-worker", ex); }
        finally { Close(); }
    }
    public void Dispose() { stop.Cancel(); if (thread.Join(2000)) stop.Dispose(); }
}
