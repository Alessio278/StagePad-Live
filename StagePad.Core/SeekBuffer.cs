namespace StagePad.Core;
/// <summary>Immutable PCM prepared off-thread to cover a streaming seek while read-ahead catches up.</summary>
public sealed class SeekBuffer
{
    private readonly ReadOnlyMemory<float> samples;
    public SeekBuffer(long startFrame, float[] samples) : this(startFrame, samples.AsMemory(), samples.LongLength * 4) { }
    internal SeekBuffer(long startFrame, ReadOnlyMemory<float> samples, long residentBytes)
    {
        StartFrame = startFrame;
        this.samples = samples;
        ResidentBytes = residentBytes;
    }
    public long StartFrame { get; }
    public long Frames => samples.Length / 2;
    public long ResidentBytes { get; }
    public int ReadAt(long position, Span<float> output)
    {
        long offset = position - StartFrame;
        if (offset < 0 || offset >= Frames) return 0;
        int frames = (int)Math.Min(output.Length / 2, Frames - offset);
        samples.Span.Slice(checked((int)(offset * 2)), frames * 2).CopyTo(output);
        return frames;
    }
}
