using NAudio.Wave;
using StagePad.App;

if (args.FirstOrDefault() == "--decode")
{
    Importer.Decode(args[1], args[2], CancellationToken.None);
    return;
}

var directory = Path.Combine(Path.GetTempPath(), "stagepad-import-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    var path = Path.Combine(directory, "reference.wav");
    var second = new float[44100];
    for (int i = 0; i < second.Length; i++)
        second[i] = (float)(2 * Math.Sin(2 * Math.PI * 440 * i / second.Length));
    using (var writer = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)))
        for (int i = 0; i < 1500; i++) writer.WriteSamples(second, 0, second.Length);

    foreach (int mib in new[] { 800, 128 })
    {
        var budget = new MemoryBudget(mib * 1048576L);
        using (var source = Importer.Import(path, directory, budget, CancellationToken.None))
        {
            bool resident = source.ResidentBytes == source.Frames * 8;
            if (resident != (mib == 800) || Math.Abs(source.Frames - 1500L * 48000) > 64)
                throw new InvalidDataException("Unexpected storage mode or resampled duration.");
            var block = new float[96000];
            if (source.ReadAt(48000, block) != 48000 || block.Max() > 1 || block.Max() < .99f ||
                block.Min() < -1 || block.Any(x => !float.IsFinite(x)))
                throw new InvalidDataException("Invalid PCM after peak correction.");
            Console.WriteLine($"{mib} MiB: {source.Frames} frames, resident={resident}, peak={block.Max()}");
        }
        if (budget.Used != 0) throw new InvalidOperationException("PCM reservation was not released.");
    }
}
finally { Directory.Delete(directory, true); }
