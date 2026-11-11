namespace StagePad.Core;

public enum VoiceState { Empty, Ready, Playing, Paused, Stopped, Ended, Error, Stopping }
public enum AudioOp { Install, Play, Pause, Stop, Volume, Master, Test, Configure, Trigger, Release, Seek }
public readonly record struct AudioCommand(AudioOp Op, int Slot = 0, float Value = 0, IAudioSource? Source = null, long Ticket = 0, int Epoch = 0, PlayMode Mode = PlayMode.OneShot, int FadeInMs = 5, int FadeOutMs = 5, long Frame = 0, SeekBuffer? Seek = null);
public readonly record struct VoiceView(VoiceState State, long Frame, long Frames, float Volume, long Starvations);

/// <summary>Single producer/control thread; single consumer/audio callback.</summary>
public sealed class SpscQueue<T> where T : struct
{
    private readonly T[] items;
    private long read, write;
    public SpscQueue(int capacity) => items = new T[capacity > 1 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity))];
    public int RemainingCapacity => items.Length-(int)(Volatile.Read(ref write)-Volatile.Read(ref read));
    public bool TryWrite(T value)
    {
        long w = write;
        if (w - Volatile.Read(ref read) >= items.Length) return false;
        items[(int)(w % items.Length)] = value;
        Volatile.Write(ref write, w + 1); return true;
    }
    public bool TryRead(out T value)
    {
        long r = read;
        if (r == Volatile.Read(ref write)) { value = default; return false; }
        int i = (int)(r % items.Length);
        value = items[i]; items[i] = default;
        Volatile.Write(ref read, r + 1); return true;
    }
}

public sealed class Mixer
{
    public const int Rate = 48000;
    private long renderHeartbeat;
    public long RenderHeartbeat => Volatile.Read(ref renderHeartbeat);
    private sealed class Voice
    {
        public IAudioSource? Source;
        public long Frame, Starvations;
        public int State;
        public float Volume = 1, Gain, Envelope, ReleaseStep;
        public PlayMode Mode;
        public int FadeIn = 240, FadeOut = 240;
        public SeekBuffer? Seek;
        public bool Cued,EmergencyPaused;
        public float SeekGain = 1;
    }
    private readonly Voice[] voices = Enumerable.Range(0, Projects.MaxSlots).Select(_ => new Voice()).ToArray();
    private readonly float[] scratch = new float[4096];
    private readonly SpscQueue<AudioCommand> commands = new(512);
    private int appliedPanic,panicEpoch,appliedPause,pauseEpoch,freeze,callbackFaults,epoch;
    private float master = 1f, masterGain = 1f;
    private long appliedTicket;
    private readonly float[] testTone = MakeTestTone();
    private int testFrame = -1;
    private static float[] MakeTestTone()
    {
        var data = new float[Rate];
        for (int i = 0; i < data.Length; i++)
        {
            float envelope = Math.Min(1, Math.Min(i / 240f, (data.Length - 1 - i) / 240f));
            data[i] = (float)Math.Sin(2 * Math.PI * 440 * i / Rate) * .0630957f * envelope;
        }
        return data;
    }
    public long AppliedTicket => Volatile.Read(ref appliedTicket);
    public int CallbackFaults => Volatile.Read(ref callbackFaults);
    public float Master => Volatile.Read(ref master);
    public void Freeze(bool value) => Volatile.Write(ref freeze, value ? 1 : 0);
    public int CommandCapacityRemaining => commands.RemainingCapacity;
    public bool Post(AudioCommand command) => commands.TryWrite(command with { Epoch = Volatile.Read(ref epoch) });
    public void Panic(){Interlocked.Increment(ref epoch);Interlocked.Increment(ref panicEpoch);}
    public void EmergencyPause(){Interlocked.Increment(ref epoch);Interlocked.Increment(ref pauseEpoch);}
    public VoiceView View(int slot)
    {
        var v = voices[slot];
        var src = Volatile.Read(ref v.Source);
        return new((VoiceState)Volatile.Read(ref v.State), Volatile.Read(ref v.Frame), src?.Frames ?? 0,
            Volatile.Read(ref v.Volume), Volatile.Read(ref v.Starvations));
    }
    private void Controls()
    {
        ApplyPanic();
        for (int n = 0; n < 64 && commands.TryRead(out var c); n++)
        {
            ApplyPanic();
            if ((c.Op is AudioOp.Play or AudioOp.Test or AudioOp.Trigger or AudioOp.Seek) && c.Epoch != Volatile.Read(ref epoch))
            {
                // A cancelled seek still retires the old preview before acknowledging its ticket.
                // Panic must cancel the cursor change, while keeping buffer lifetime accounting safe.
                if(c.Op==AudioOp.Seek && (uint)c.Slot<Projects.MaxSlots)
                { voices[c.Slot].Seek=null; if(c.Ticket>0)Volatile.Write(ref appliedTicket,c.Ticket); }
                continue;
            }
            if (c.Op == AudioOp.Test) { testFrame = 0; continue; }
            if (c.Op == AudioOp.Master) { master = Math.Clamp(c.Value, 0, 1); continue; }
            if ((uint)c.Slot >= Projects.MaxSlots) continue;
            var v = voices[c.Slot];
            switch (c.Op)
            {
                case AudioOp.Install:
                    v.Frame = 0; v.Gain = 0; v.Envelope = 0; v.Starvations = 0; v.Seek=null;v.Cued=false;v.EmergencyPaused=false;v.SeekGain=1;
                    Volatile.Write(ref v.Source, c.Source);
                    Volatile.Write(ref v.State, (int)(c.Source == null ? VoiceState.Empty : VoiceState.Ready));
                    break;
                case AudioOp.Configure:
                    v.Mode = c.Mode; v.FadeIn = Math.Max(1, c.FadeInMs * Rate / 1000); v.FadeOut = Math.Max(1, c.FadeOutMs * Rate / 1000); break;
                case AudioOp.Trigger:
                    if (v.Source == null) break;
                    if (v.Mode == PlayMode.Protected && (VoiceState)v.State is VoiceState.Playing or VoiceState.Paused or VoiceState.Stopping) break;
                    if (v.Mode == PlayMode.TogglePause && (VoiceState)v.State == VoiceState.Playing) { v.State = (int)VoiceState.Paused; break; }
                    if (v.Mode != PlayMode.TogglePause || (VoiceState)v.State != VoiceState.Paused) { if(!v.Cued)v.Frame=0; v.Gain=v.Volume;v.Envelope=0;v.SeekGain=1; }
                    v.Cued=false;
                    v.EmergencyPaused=false;v.State = (int)VoiceState.Playing; break;
                case AudioOp.Release:
                    if(v.EmergencyPaused)break;
                    if (v.Mode == PlayMode.Hold)
                    {
                        if ((VoiceState)v.State == VoiceState.Playing) { v.ReleaseStep = v.Envelope / v.FadeOut; v.State = (int)VoiceState.Stopping; }
                        else if ((VoiceState)v.State == VoiceState.Paused) { v.State = (int)VoiceState.Stopped; v.Frame = 0; v.Gain = 0; v.Envelope = 0; }
                    }
                    break;
                case AudioOp.Play:
                    if (v.Source != null)
                    {
                        if ((VoiceState)v.State != VoiceState.Paused) { if(!v.Cued)v.Frame=0;v.Gain=v.Volume;v.Envelope=0;v.SeekGain=1; }
                        v.Cued=false;
                        v.EmergencyPaused=false;v.State = (int)VoiceState.Playing;
                    }
                    break;
                case AudioOp.Pause:
                    if ((VoiceState)v.State == VoiceState.Playing) v.State = (int)VoiceState.Paused;
                    break;
                case AudioOp.Stop:
                    v.Cued=false;v.EmergencyPaused=false;
                    if ((VoiceState)v.State == VoiceState.Playing) { v.ReleaseStep = v.Envelope / v.FadeOut; v.State = (int)VoiceState.Stopping; }
                    else { v.State = (int)VoiceState.Stopped; v.Frame = 0; v.Gain = 0; v.Envelope = 0; }
                    break;
                case AudioOp.Seek:
                    if(v.Source==null)break;
                    v.Frame=Math.Clamp(c.Frame,0,v.Source.Frames);v.Seek=c.Seek;v.Starvations=0;
                    v.Source.Hint(v.Frame);
                    if((VoiceState)v.State is VoiceState.Playing or VoiceState.Paused)v.SeekGain=0;
                    else if((VoiceState)v.State!=VoiceState.Paused){v.State=(int)VoiceState.Stopped;v.Cued=true;v.Envelope=0;}
                    break;
                case AudioOp.Volume: v.Volume = Math.Clamp(c.Value, 0, 1); break;
            }
            // After this acknowledgement, the callback no longer references the old source.
            if (c.Ticket > 0) Volatile.Write(ref appliedTicket, c.Ticket);
        }
        ApplyPanic();
    }
    private void ApplyPanic()
    {
        int requested=Volatile.Read(ref panicEpoch);
        if(requested!=appliedPanic)
        {
            testFrame=-1;
            for(int i=0;i<Projects.MaxSlots;i++)
            {voices[i].State=(int)(voices[i].Source==null?VoiceState.Empty:VoiceState.Stopped);voices[i].Frame=0;voices[i].Gain=0;voices[i].Envelope=0;voices[i].Cued=false;voices[i].EmergencyPaused=false;}
            appliedPanic=requested;
        }
        int pause=Volatile.Read(ref pauseEpoch);
        if(pause!=appliedPause)
        {
            testFrame=-1;
            foreach(var voice in voices)if((VoiceState)voice.State is VoiceState.Playing or VoiceState.Stopping){voice.State=(int)VoiceState.Paused;voice.EmergencyPaused=true;}
            appliedPause=pause;
        }
    }

    public void Render(Span<float> output)
    {
        Volatile.Write(ref renderHeartbeat, Environment.TickCount64);
        output.Clear();
        try
        {
            Controls();
            if (Volatile.Read(ref freeze) != 0) return;
            for (int offset = 0; offset < output.Length; offset += scratch.Length)
            {
                int samples = Math.Min(scratch.Length, output.Length - offset) & ~1;
                if (samples == 0) break;
                var target = output.Slice(offset, samples);
                foreach (var v in voices)
                {
                    if ((VoiceState)v.State is not (VoiceState.Playing or VoiceState.Stopping) || v.Source == null) continue;
                    try
                    {
                        v.Source.Hint(v.Frame);
                        int frames=v.Seek?.ReadAt(v.Frame,scratch.AsSpan(0,samples))??0;
                        if(frames<samples/2 && v.Frame+frames<v.Source.Frames)
                            frames+=v.Source.ReadAt(v.Frame+frames,scratch.AsSpan(frames*2,samples-frames*2));
                        // A silent/starved source must not prevent the operator stopping it.
                        if (frames == 0 && (VoiceState)v.State == VoiceState.Stopping)
                        { v.Frame = 0; v.Envelope = 0; v.Gain = 0; v.State = (int)VoiceState.Stopped; continue; }
                        if (frames < samples / 2 && v.Frame + frames < v.Source.Frames) v.Starvations++;
                        for (int i = 0; i < frames; i++)
                        {
                            // 5 ms attack and gain smoothing. No dynamic allocation or locks.
                            bool stopping = (VoiceState)v.State == VoiceState.Stopping;
                            v.Envelope = stopping ? Math.Max(0, v.Envelope - v.ReleaseStep) : Math.Min(1, v.Envelope + 1f / v.FadeIn);
                            v.Gain += Math.Clamp(v.Volume - v.Gain, -1f / 240, 1f / 240);
                            int j = i * 2;
                            v.SeekGain=Math.Min(1,v.SeekGain+1f/240);
                            float amplitude = v.Gain * v.Envelope * v.SeekGain;
                            target[j] += scratch[j] * amplitude; target[j + 1] += scratch[j + 1] * amplitude;
                        }
                        if ((VoiceState)v.State == VoiceState.Stopping && v.Envelope <= 0.000001f)
                        { v.Frame = 0; v.State = (int)VoiceState.Stopped; continue; }
                        v.Frame += frames;
                        if (v.Frame >= v.Source.Frames) v.State = (int)VoiceState.Ended;
                        if (v.Source.Error != null) v.State = (int)VoiceState.Error;
                    }
                    catch { v.State = (int)VoiceState.Error; Interlocked.Increment(ref callbackFaults); }
                }
                for (int i = 0; i < target.Length; i += 2)
                {
                    if (testFrame >= 0)
                    {
                        float tone = testTone[testFrame++]; target[i] += tone; target[i + 1] += tone;
                        if (testFrame >= testTone.Length) testFrame = -1;
                    }
                    masterGain += Math.Clamp(master - masterGain, -1f / 240, 1f / 240);
                    target[i] = Safe(target[i] * masterGain); target[i + 1] = Safe(target[i + 1] * masterGain);
                }
            }
        }
        catch { output.Clear(); Interlocked.Increment(ref callbackFaults); }
    }
    private static float Safe(float x) => float.IsFinite(x) ? Math.Clamp(x, -1, 1) : 0;
}
