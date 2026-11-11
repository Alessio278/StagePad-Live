using System.Collections.Concurrent;
using StagePad.Core;
namespace NAudio.Midi
{
    public sealed record Caps(int Manufacturer = 1, int ProductId = 2, string ProductName = "Fake MIDI");
    public sealed class MidiInMessageEventArgs(int raw) : EventArgs { public int RawMessage => raw; }
    public static class FakeMidi
    {
        public static volatile bool InputPresent = true, OutputPresent = true, InputBusy, OutputBusy, OutputError;
        public static readonly ConcurrentQueue<int> Notes = new();
        public static readonly ConcurrentQueue<byte[]> SysEx = new();
        public static readonly ManualResetEventSlim OutGate = new(true);
        public static readonly ConcurrentDictionary<MidiIn, byte> Inputs = new();
        public static void Emit(int raw) { foreach (var input in Inputs.Keys) input.Emit(raw); }
    }
    public sealed class MidiIn : IDisposable
    {
        public static int NumberOfDevices => FakeMidi.InputPresent ? 1 : 0;
        public static Caps DeviceInfo(int index) => new();
        public event EventHandler<MidiInMessageEventArgs>? MessageReceived;
        public event EventHandler<MidiInMessageEventArgs>? ErrorReceived;
        public MidiIn(int index) { if (FakeMidi.InputBusy) throw new IOException("Input occupato simulato"); FakeMidi.Inputs[this] = 0; }
        public void Start() { }
        public void Stop() { }
        public void Emit(int raw) => MessageReceived?.Invoke(this, new(raw));
        public void Fail() => ErrorReceived?.Invoke(this, new(0));
        public void Dispose() => FakeMidi.Inputs.TryRemove(this, out _);
    }
    public sealed class MidiOut : IDisposable
    {
        public static int NumberOfDevices => FakeMidi.OutputPresent ? 1 : 0;
        public static Caps DeviceInfo(int index) => new();
        public MidiOut(int index) { if (FakeMidi.OutputBusy) throw new IOException("Output occupato simulato"); }
        private static void Check() { FakeMidi.OutGate.Wait(); if (FakeMidi.OutputError) throw new IOException("Errore uscita simulato"); }
        public void Send(int raw) { Check(); FakeMidi.Notes.Enqueue(raw); }
        public void SendBuffer(byte[] bytes) { Check(); FakeMidi.SysEx.Enqueue((byte[])bytes.Clone()); }
        public void Dispose() { }
    }
}
namespace StagePad.App
{
    internal sealed class AudioDevice : IDisposable
    {
        private readonly CancellationTokenSource stop = new();
        private readonly Thread thread;
        private long heartbeat;
        private string? id = "fake-audio";
        public string Status => "Fake audio ready";
        public string? Error => null;
        public string? DeviceId => id;
        public int Latency { get; private set; } = 40;
        public DeviceInfo[] Devices => [new("fake-audio", "Fake audio")];
        public long Heartbeat => Volatile.Read(ref heartbeat);
        public AudioDevice(Mixer mixer)
        {
            thread = new Thread(() =>
            {
                var buffer = new float[960];
                while (!stop.IsCancellationRequested)
                { mixer.Render(buffer); Volatile.Write(ref heartbeat, Environment.TickCount64); stop.Token.WaitHandle.WaitOne(10); }
            }) { IsBackground = true }; thread.Start();
        }
        public void Configure(string? value, int latency) { id = value ?? "fake-audio"; Latency = latency; }
        public void Dispose() { stop.Cancel(); thread.Join(1000); }
    }
    internal static class Importer
    {
        public static IAudioSource Import(string original, string cache, MemoryBudget budget, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!File.Exists(original)) throw new FileNotFoundException("Missing simulated audio", original);
            if (File.ReadAllText(original) == "CORRUPT") throw new InvalidDataException("Corrupt simulated audio");
            var samples = new float[48000 * 2 * 40];
            if (!budget.Reserve(samples.LongLength * 4)) throw new InvalidOperationException("Budget exhausted");
            return new OwnedSource(new MemorySource(samples), Path.Combine(cache, Guid.NewGuid() + ".unused"), budget, [0,.5f,1,.5f,0]);
        }
    }
}
