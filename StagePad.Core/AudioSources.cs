using System.Runtime.InteropServices;

namespace StagePad.Core;

public interface IAudioSource : IDisposable
{
    long Frames { get; }
    long ResidentBytes { get; }
    string? Error { get; }
    // Only the audio callback calls ReadAt. A short read means temporary starvation,
    // except when position reaches Frames. It must never wait on a worker or a file.
    int ReadAt(long position, Span<float> stereo);
    void Hint(long position) { }
}

public sealed class MemorySource(float[] samples) : IAudioSource
{
    public long Frames => samples.LongLength / 2;
    public long ResidentBytes => samples.LongLength * sizeof(float);
    public string? Error => null;
    public int ReadAt(long position, Span<float> stereo)
    {
        if (position < 0 || position >= Frames) return 0;
        int count = (int)Math.Min(stereo.Length / 2, Frames - position);
        samples.AsSpan(checked((int)(position * 2)), count * 2).CopyTo(stereo);
        return count;
    }
    public void Dispose() { }
}

/// <summary>
/// Fixed PCM page cache. Head is pinned logically (always resident, not GC-pinned).
/// Page tags are published after complete writes. A sequence check on both sides
/// of CopyTo rejects concurrent replacement. No waiting or I/O on the consumer.
/// </summary>
public sealed class DiskSource : IAudioSource
{
    private sealed class Page(int samples)
    {
        public readonly float[] Data = new float[samples];
        public long Tag = -1;
        public int Sequence;
    }
    private readonly string path;
    private readonly int rate;
    private readonly float[] head;
    private readonly Page[] pages;
    private readonly CancellationTokenSource stop = new();
    private readonly Thread worker;
    private readonly Action<Exception>? onError;
    private long demand;
    private string? error;
    public long Frames { get; }
    public long ResidentBytes => (head.LongLength + (long)pages.Length * rate * 2) * 4;
    public string? Error => Volatile.Read(ref error);
    public bool WorkerStopped => !worker.IsAlive;
    public static long EstimateBytes(long frames, int rate) =>
        (Math.Min(frames, 5L * rate) * 2 + 12L * rate * 2) * 4;

    public DiskSource(string path, long frames, int rate = 48000, Action<Exception>? onError = null)
    {
        this.path = path; Frames = frames; this.rate = rate; this.onError = onError;
        head = new float[checked((int)Math.Min(frames * 2, rate * 2L * 5))];
        pages = Enumerable.Range(0, 12).Select(_ => new Page(rate * 2)).ToArray();
        // Constructor runs on an import worker, never the UI/audio callback.
        using (var input = File.OpenRead(path)) input.ReadExactly(MemoryMarshal.AsBytes(head.AsSpan()));
        using (var input = File.OpenRead(path)) Fill(input, 0);
        worker = new Thread(Worker) { IsBackground = true, Name = "StagePad PCM read-ahead" };
        worker.Start();
    }
    private void Fill(FileStream input, long position)
    {
        long first = Math.Max(head.Length / (rate * 2), position / rate);
        for (long tag = first; tag < first + 10 && tag * rate < Frames; tag++)
        {
            if (stop.IsCancellationRequested) return;
            var page = pages[(int)(tag % pages.Length)];
            if (Volatile.Read(ref page.Tag) == tag) continue;
            Interlocked.Increment(ref page.Sequence);
            Volatile.Write(ref page.Tag, -1);
            input.Position = tag * rate * 2 * sizeof(float);
            int samples = (int)Math.Min(rate * 2L, (Frames - tag * rate) * 2);
            input.ReadExactly(MemoryMarshal.AsBytes(page.Data.AsSpan(0, samples)));
            page.Data.AsSpan(samples).Clear();
            Volatile.Write(ref page.Tag, tag);
            Interlocked.Increment(ref page.Sequence);
        }
    }
    public void Hint(long position) => Volatile.Write(ref demand, Math.Clamp(position,0,Math.Max(0,Frames-1)));
    private void Worker()
    {
        try
        {
            using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.RandomAccess);
            while (!stop.IsCancellationRequested)
            {
                Fill(input, Volatile.Read(ref demand));
                if (stop.Token.WaitHandle.WaitOne(10)) return;
            }
        }
        catch (Exception ex) { Volatile.Write(ref error, ex.Message); try { onError?.Invoke(ex); } catch { } }
    }
    public int ReadAt(long position, Span<float> stereo)
    {
        if (position < 0 || position >= Frames) return 0;
        Volatile.Write(ref demand, position);
        int wanted = (int)Math.Min(stereo.Length / 2, Frames - position), done = 0;
        while (done < wanted)
        {
            long frame = position + done;
            int count;
            if (frame * 2 < head.Length)
            {
                count = Math.Min(wanted - done, (int)(head.Length / 2 - frame));
                head.AsSpan((int)(frame * 2), count * 2).CopyTo(stereo[(done * 2)..]);
            }
            else
            {
                long tag = frame / rate;
                var page = pages[(int)(tag % pages.Length)];
                int sequence = Volatile.Read(ref page.Sequence);
                if ((sequence & 1) != 0 || Volatile.Read(ref page.Tag) != tag) break;
                int offset = (int)(frame % rate);
                count = Math.Min(wanted - done, rate - offset);
                page.Data.AsSpan(offset * 2, count * 2).CopyTo(stereo[(done * 2)..]);
                if (Volatile.Read(ref page.Sequence) != sequence || Volatile.Read(ref page.Tag) != tag)
                { stereo.Slice(done * 2, count * 2).Clear(); break; }
            }
            done += count;
        }
        return done;
    }
    public void Dispose()
    {
        stop.Cancel();
        // Called only after the audio callback has acknowledged source replacement.
        // If disk I/O hangs, do not block the control loop or audio waiting for it.
        if (worker.Join(500)) stop.Dispose();
    }
}
