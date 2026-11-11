using System.Runtime.InteropServices;
namespace StagePad.Core;
public static class WavePeaks
{
    // Offline PCM analysis only. Never invoked on a render or UI thread.
    public static float[] Build(string pcm, int bins = 1024)
    {
        using var input = File.OpenRead(pcm);
        long frames = input.Length / 8;
        if (frames == 0) return [];
        var peaks = new float[bins]; var samples = new float[32768]; long frame = 0;
        while (input.Position < input.Length)
        {
            int bytes = (int)Math.Min(samples.Length * 4L, input.Length - input.Position);
            input.ReadExactly(MemoryMarshal.AsBytes(samples.AsSpan(0, bytes / 4)));
            for (int i = 0; i < bytes / 4; i += 2, frame++)
            {
                int bin = (int)Math.Min(bins - 1L, frame * bins / frames);
                float peak = Math.Max(Math.Abs(samples[i]), Math.Abs(samples[i + 1]));
                if (float.IsFinite(peak)) peaks[bin] = Math.Max(peaks[bin], Math.Min(1, peak));
            }
        }
        return peaks;
    }
}
