using System.IO;
using StagePad.Core;
namespace StagePad.App;
internal sealed class MemoryBudget(long bytes)
{
    private long used;
    private long limit = bytes;
    public bool SetLimit(long value, bool force = false) { if (!force && value < Used) return false; Volatile.Write(ref limit, value); return true; }
    public long Used => Volatile.Read(ref used);
    public bool Reserve(long count)
    {
        while (true)
        {
            long current = Volatile.Read(ref used);
            if (count < 0 || current > Volatile.Read(ref limit) - count) return false;
            if (Interlocked.CompareExchange(ref used, current + count, current) == current) return true;
        }
    }
    public void Release(long count) => Interlocked.Add(ref used, -count);
}
internal sealed class OwnedSource(IAudioSource inner, string cache, MemoryBudget budget, float[] peaks) : IAudioSource
{
    public float[] Peaks { get; } = peaks;
    public long Frames => inner.Frames;
    public long ResidentBytes => inner.ResidentBytes;
    public string? Error => inner.Error;
    public int ReadAt(long frame, Span<float> output) => inner.ReadAt(frame, output);
    public void Hint(long frame)=>inner.Hint(frame);
    public SeekBuffer? PrepareSeek(long frame,CancellationToken ct)
    {
        if(inner is not DiskSource || frame>=Frames)return null;
        int count=(int)Math.Min(Mixer.Rate,Frames-frame);
        long bytes=count*8L;
        if(!budget.Reserve(bytes))throw new InvalidOperationException("Budget RAM insufficiente per preparare lo spostamento.");
        try
        {
            ct.ThrowIfCancellationRequested();
            float[] samples=new float[count*2];
            using var input=new FileStream(cache,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.RandomAccess);
            input.Position=frame*8;
            input.ReadExactly(System.Runtime.InteropServices.MemoryMarshal.AsBytes(samples.AsSpan()));
            ct.ThrowIfCancellationRequested();
            return new SeekBuffer(frame,samples);
        }
        catch{budget.Release(bytes);throw;}
    }
    public LoopRegion PrepareLoop(long start,long end,CancellationToken ct)
    {
        if(start<0 || end>Frames || end-start<2400)throw new InvalidDataException("Intervallo loop non valido.");
        long frames=end-start,bytes=checked(frames*8);
        if(frames>Array.MaxLength/2 || !budget.Reserve(bytes))throw new InvalidOperationException("Budget RAM insufficiente per il loop. Accorcia l’intervallo o aumenta il budget nelle impostazioni.");
        try
        {
            ct.ThrowIfCancellationRequested();var samples=new float[checked((int)(frames*2))];
            using var input=new FileStream(cache,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.RandomAccess);
            input.Position=start*8;
            // Check cancellation between bounded reads, never on the audio callback.
            for(int offset=0;offset<samples.Length;)
            {
                ct.ThrowIfCancellationRequested();int count=Math.Min(32768,samples.Length-offset);
                input.ReadExactly(System.Runtime.InteropServices.MemoryMarshal.AsBytes(samples.AsSpan(offset,count)));offset+=count;
            }
            ct.ThrowIfCancellationRequested();return new LoopRegion(new SeekBuffer(start,samples));
        }
        catch{budget.Release(bytes);throw;}
    }
    public void Dispose()
    {
        inner.Dispose();
        if (inner is DiskSource disk && !disk.WorkerStopped)
        { Log.Warn("read-ahead", "Worker disco bloccato: riserva RAM mantenuta fino al riavvio."); return; }
        budget.Release(inner.ResidentBytes);
        try { File.Delete(cache); File.Delete(cache + ".wave"); } catch (Exception ex) { Log.Warn("cache", ex.Message); }
    }
}
