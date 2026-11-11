using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using StagePad.Core;

namespace StagePad.App;
public sealed record Request(string Op, int Slot = 0, string? Path = null, float Value = 0, string? DeviceId = null, int Latency = 40,
    SlotSettings? Settings = null, RoutingSettings? Routing = null, int Channel = 1, int Note = 0, bool Flag = false, long Epoch = 0, string? Origin = null, long Timestamp = 0, int Rows = 4, int Columns = 4, string? TargetPath = null, int Controller = -1, MidiCcBinding? MasterControl = null, MidiCcBinding? TrackControl = null);
public sealed record DeviceInfo(string Id, string Name) { public override string ToString()=>Name; }
public sealed record MidiPortState(string Instance, string Status, string? Error, string? Selected, DeviceInfo[] Devices, long Heartbeat, string LastMessage = "", long Messages = 0, bool LoggingUnavailable = false, int ActiveNotes=0, int PendingCommands=0);
public sealed record SlotInfo(int Index, string Name, string State, double Position, double Duration, float Volume, long Underruns, string? Error, bool Loading, int Revision = 0, bool Seeking=false);
public sealed record StateInfo(string Instance, string AudioStatus, string? AudioError, string? DeviceId, int Latency, float Master, long MemoryBytes, int CallbackErrors, bool LoggingUnavailable, DeviceInfo[] Devices, SlotInfo[] Slots,
    ProjectDocument? Project = null, bool ShowLocked = false, int LearnSlot = -1, string? ProjectError = null, string? MidiAlert = null, MidiPortState? MidiIn = null, MidiPortState? MidiOut = null,
    long Heartbeat = 0, long RenderHeartbeat = 0, long DeviceHeartbeat = 0, int LastStarted = 0, long TriggerSerial = 0, string? ProjectPath = null, bool ProjectLoading = false, long ProjectRevision = 0, string? OscAlert = null, long OscSent = 0, string? VolumeLearn = null, int CurrentPlaying = -1);
public sealed record Reply(bool Ok, string? Error = null, StateInfo? State = null, MidiPortState? Midi = null, float[]? Peaks = null, long UiBeat = 0, bool UiReady=false);

internal static class Wire
{
    private const int Limit = 2 * 1024 * 1024;
#if STAGEPAD_TESTS
    internal static Func<string, Request, Task<Reply>>? TestRoute;
#endif
    public static async Task Write<T>(Stream stream, T value, CancellationToken ct)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        if (bytes.Length > Limit) throw new InvalidDataException("Messaggio troppo grande.");
        byte[] header = BitConverter.GetBytes(bytes.Length);
        await stream.WriteAsync(header, ct); await stream.WriteAsync(bytes, ct); await stream.FlushAsync(ct);
    }
    public static async Task<T> Read<T>(Stream stream, CancellationToken ct)
    {
        byte[] header = new byte[4]; await stream.ReadExactlyAsync(header, ct);
        int length = BitConverter.ToInt32(header);
        if (length is <= 0 or > Limit) throw new InvalidDataException("Messaggio non valido.");
        byte[] bytes = new byte[length]; await stream.ReadExactlyAsync(bytes, ct);
        return JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidDataException("JSON vuoto.");
    }
    public static async Task<Reply> Call(string pipe, Request request, int timeoutMs = 1800)
    {
        using var ct = new CancellationTokenSource(timeoutMs);
#if STAGEPAD_TESTS
        if (TestRoute is { } route)
        {
            using var input = new MemoryStream();
            await Write(input, request, ct.Token); input.Position = 0;
            var copy = await Read<Request>(input, ct.Token);
            var reply = await route(pipe, copy).WaitAsync(ct.Token);
            using var output = new MemoryStream();
            await Write(output, reply, ct.Token); output.Position = 0;
            return await Read<Reply>(output, ct.Token);
        }
#endif
        using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(ct.Token);
        await Write(client, request, ct.Token);
        return await Read<Reply>(client, ct.Token);
    }
    public static async Task Serve(string name, Func<Request, Task<Reply>> handle, CancellationToken ct)
    {
        var clients = new SemaphoreSlim(8);
        while (!ct.IsCancellationRequested)
        {
            await clients.WaitAsync(ct);
            NamedPipeServerStream? server = null;
            try
            {
                server = new NamedPipeServerStream(name, PipeDirection.InOut, 8, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(ct);
                var connected = server;
                _ = Task.Run(async () =>
                {
                    using (connected)
                    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        timeout.CancelAfter(5000);
                        try
                        {
                            var request = await Read<Request>(connected, timeout.Token);
                            if(request.Op is "export-show" or "import-show")timeout.CancelAfter(300000);
                            if(request.Op is "save-project" or "load-project" or "rename-project" or "duplicate-project" or "delete-project")timeout.CancelAfter(25000);
                            Reply reply = await handle(request);
                            await Write(connected, reply, timeout.Token);
                        }
                        catch (Exception ex) when (ex is IOException or OperationCanceledException or JsonException or InvalidDataException)
                        { Log.Warn("ipc", ex.Message); }
                        catch (Exception ex) { Log.Error("ipc", ex); }
                        finally { clients.Release(); }
                    }
                }, CancellationToken.None);
            }
            catch { server?.Dispose(); clients.Release(); throw; }
        }
        // Client tasks can finish after cancellation; do not dispose their semaphore.
    }
}
