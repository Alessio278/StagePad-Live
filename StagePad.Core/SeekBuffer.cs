namespace StagePad.Core;
/// <summary>Immutable PCM prepared off-thread to cover a streaming seek while read-ahead catches up.</summary>
public sealed class SeekBuffer(long startFrame, float[] samples)
{
    public long StartFrame { get; } = startFrame;
    public long Frames => samples.LongLength / 2;
    public long ResidentBytes => samples.LongLength * 4;
    public int ReadAt(long position, Span<float> output)
    {
        long offset = position - StartFrame;
        if (offset < 0 || offset >= Frames) return 0;
        int frames = (int)Math.Min(output.Length / 2, Frames - offset);
        samples.AsSpan(checked((int)(offset * 2)), frames * 2).CopyTo(output);
        return frames;
    }
}
