using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NVorbis;
using StagePad.Core;

namespace StagePad.App;
internal static class Importer
{
    private sealed record CacheInfo(long Samples, float Peak, float[] Peaks);
    private sealed class VorbisSamples(VorbisReader reader) : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(reader.SampleRate, reader.Channels);
        public int Read(float[] buffer, int offset, int count) => reader.ReadSamples(buffer, offset, count);
    }
    public static void Decode(string original, string cache, CancellationToken ct)
    {
        IDisposable? decoder = null;
        try
        {
            ISampleProvider samples;
            string extension = System.IO.Path.GetExtension(original).ToLowerInvariant();
            if (extension is ".ogg" or ".oga")
            {
                var vorbis = new VorbisReader(original); decoder = vorbis; samples = new VorbisSamples(vorbis);
            }
            else if (extension == ".wav")
            {
                var wav = new WaveFileReader(original); decoder = wav; samples = wav.ToSampleProvider();
            }
            else
            {
                var mf = new MediaFoundationReader(original); decoder = mf; samples = mf.ToSampleProvider();
            }
            if (samples.WaveFormat.Channels == 1) samples = new MonoToStereoSampleProvider(samples) { LeftVolume = 1, RightVolume = 1 };
            if (samples.WaveFormat.Channels != 2) throw new NotSupportedException("Sono supportati file mono e stereo.");
            if (samples.WaveFormat.SampleRate != Mixer.Rate) samples = new WdlResamplingSampleProvider(samples, Mixer.Rate);
            float[] block = new float[32768]; long total = 0; float peak = 0;
            using (var output = new FileStream(cache, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072))
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    int count = samples.Read(block, 0, block.Length);
                    if (count == 0) break;
                    if ((count & 1) != 0) throw new InvalidDataException("Campioni stereo incompleti.");
                    for (int i = 0; i < count; i++)
                    {
                        peak = Math.Max(peak, Math.Abs(block[i]));
                        if (!float.IsFinite(block[i])) throw new InvalidDataException("Il decoder ha prodotto campioni non validi.");
                    }
                    output.Write(MemoryMarshal.AsBytes(block.AsSpan(0, count)));
                    total += count;
                    if (total > Mixer.Rate * 2L * 60 * 60 * 2) throw new InvalidDataException("File superiore al limite cautelativo di 2 ore.");
                }
                output.Flush(true);
            }
            decoder.Dispose(); decoder = null;
            if (total < 2) throw new InvalidDataException("Il file non contiene audio decodificabile.");
            if (peak > 1)
            {
                using var pcm = new FileStream(cache, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                for (long remaining = total; remaining > 0;)
                {
                    ct.ThrowIfCancellationRequested();
                    int count = (int)Math.Min(block.Length, remaining);
                    var bytes = MemoryMarshal.AsBytes(block.AsSpan(0, count));
                    pcm.ReadExactly(bytes);
                    for (int i = 0; i < count; i++) block[i] /= peak;
                    pcm.Position -= bytes.Length;
                    pcm.Write(bytes);
                    remaining -= count;
                }
                pcm.Flush(true);
                Log.Warn("pcm-peak", $"Peak {peak:R}; applied constant gain {1 / peak:R}");
            }
            File.WriteAllText(cache + ".wave", JsonSerializer.Serialize(new CacheInfo(total, peak, WavePeaks.Build(cache))));
        }
        finally { decoder?.Dispose(); }
    }
    public static IAudioSource Import(string original, string sessionCache, MemoryBudget budget, CancellationToken ct)
    {
        if (!File.Exists(original)) throw new FileNotFoundException("File audio non trovato.", original);
        Directory.CreateDirectory(sessionCache);
        string cache = System.IO.Path.Combine(sessionCache, Guid.NewGuid().ToString("N") + ".pcm");
        long reserved = 0;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromMinutes(15));
            var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("--decode"); info.ArgumentList.Add(original); info.ArgumentList.Add(cache); info.ArgumentList.Add(Environment.ProcessId.ToString());
            using var child = Process.Start(info) ?? throw new InvalidOperationException("Impossibile avviare il decoder.");
            try { child.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult(); }
            catch { try { child.Kill(true); child.WaitForExit(2000); } catch { } throw; }
            if (child.ExitCode != 0)
            {
                string detail = File.Exists(cache + ".error") ? File.ReadAllText(cache + ".error") : "Decoder terminato in errore o file non supportato.";
                throw new InvalidDataException(detail);
            }
            long bytes = new FileInfo(cache).Length;
            if (bytes < 8 || bytes % 8 != 0) throw new InvalidDataException("Cache PCM non valida.");
            long total = bytes / 4;
            ct.ThrowIfCancellationRequested();
            long frames = total / 2;
            string metadata = cache + ".wave";
            if (!File.Exists(metadata) || new FileInfo(metadata).Length > 65536)
                throw new InvalidDataException("Cache PCM non valida.");
            var infoPcm = JsonSerializer.Deserialize<CacheInfo>(File.ReadAllText(metadata));
            if (infoPcm is null || infoPcm.Samples != total || infoPcm.Peaks is null ||
                infoPcm.Peaks.Length != Math.Min(1024L, frames) || infoPcm.Peaks.Any(p => !float.IsFinite(p) || p < 0 || p > 1))
                throw new InvalidDataException("Cache PCM non valida.");
            float[] peaks = infoPcm.Peaks;
            IAudioSource? source = null;
            if (frames <= Mixer.Rate * 60L * 25 && budget.Reserve(total * 4))
            {
                reserved = total * 4;
                try
                {
                    float[] data = new float[checked((int)total)];
                    using var input = File.OpenRead(cache); input.ReadExactly(MemoryMarshal.AsBytes(data.AsSpan()));
                    source = new MemorySource(data);
                }
                catch (OutOfMemoryException)
                {
                    budget.Release(reserved);
                    reserved = 0;
                }
            }
            if (source is null)
            {
                long estimate = DiskSource.EstimateBytes(frames, Mixer.Rate);
                if (!budget.Reserve(estimate)) throw new InvalidOperationException("Budget RAM esaurito. Libera uno slot o aumenta il budget RAM nelle Impostazioni.");
                reserved = estimate;
                source = new DiskSource(cache, frames, Mixer.Rate, ex => Log.Error("read-ahead", ex));
            }
            Log.Warn("pcm-ready", $"{source.GetType().Name}: {frames} frames, {reserved} resident bytes, decoded peak {infoPcm.Peak:R}");
            return new OwnedSource(source, cache, budget, peaks);
        }
        catch
        {
            if (reserved > 0) budget.Release(reserved);
            try { File.Delete(cache); File.Delete(cache + ".wave"); } catch { }
            throw;
        }
        finally { try { File.Delete(cache + ".error"); } catch { } }
    }
}
