using System.Collections.Concurrent;
using System.Diagnostics;
using NAudio.Midi;
using StagePad.Core;

namespace StagePad.App;
internal abstract class MidiModule : IDisposable
{
    protected readonly string audioPipe;
    protected readonly CancellationTokenSource stop = new();
    protected readonly string instance = Guid.NewGuid().ToString("N");
    protected string? desired, error;
    protected string status = "Disabilitato", lastMessage = "";
    protected long count, heartbeat = Environment.TickCount64;
    protected int version, openVersion = -1, fault;
    protected DeviceInfo[] devices = [];
    protected Thread? worker;
    private MidiPortState? snapshot;
    protected MidiModule(string audioPipe) => this.audioPipe = audioPipe;
    protected abstract bool Input { get; }
    protected virtual int ActiveNotes=>0;
    protected virtual int PendingCount=>0;
    protected virtual Task<Reply> Deferred(Request r)=>Task.FromResult(Command(r));
    protected abstract void Tick();
    protected abstract void ClosePort();
    protected abstract void OpenPort(int index);
    protected virtual Reply Command(Request r) => new(false, "Comando MIDI sconosciuto.");
    public Task<Reply> Handle(Request r)
    {
        if (r.Op == "state") return Task.FromResult(new Reply(true, Midi: Volatile.Read(ref snapshot) ?? new(instance, status, error, desired, devices, Volatile.Read(ref heartbeat))));
        if (r.Op == "shutdown") { stop.Cancel(); return Task.FromResult(new Reply(true)); }
        if (r.Op == "configure") { Volatile.Write(ref desired, r.DeviceId); Interlocked.Increment(ref version); return Task.FromResult(new Reply(true)); }
        return Deferred(r);
    }
    public CancellationToken Shutdown => stop.Token;
    public void Start()
    {
        worker = new Thread(Run) { IsBackground = true, Name = Input ? "StagePad MIDI IN" : "StagePad MIDI OUT" }; worker.Start();
    }
    private void Run()
    {
        bool opened = false; long nextScan = 0, nextRetry = 0;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                long now = Environment.TickCount64;
                if (now >= nextScan)
                {
                    int total = Input ? MidiIn.NumberOfDevices : MidiOut.NumberOfDevices;
                    var entries = new List<DeviceInfo>();
                    for (int i = 0; i < total; i++)
                    {
                        if (Input) { var c = MidiIn.DeviceInfo(i); entries.Add(new($"{c.Manufacturer}:{c.ProductId}:{c.ProductName}", c.ProductName)); }
                        else { var c = MidiOut.DeviceInfo(i); entries.Add(new($"{c.Manufacturer}:{c.ProductId}:{c.ProductName}", c.ProductName)); }
                    }
                    devices = entries.ToArray(); nextScan = now + 1000;
                    string? target = Volatile.Read(ref desired);
                    int matches = target == null ? 0 : devices.Count(x => x.Id == target);
                    if (opened && matches != 1) Interlocked.Exchange(ref fault, 1);
                }
                if (openVersion != Volatile.Read(ref version) || Interlocked.Exchange(ref fault, 0) != 0)
                { ClosePort(); opened = false; openVersion = Volatile.Read(ref version); nextRetry = 0; }
                string? selected = Volatile.Read(ref desired);
                if (!opened && now >= nextRetry)
                {
                    if (selected == null) { status = "Disabilitato"; error = null; }
                    else
                    {
                        int[] matching = devices.Select((d, i) => (d, i)).Where(x => x.d.Id == selected).Select(x => x.i).ToArray();
                        if (matching.Length != 1) throw new InvalidOperationException(matching.Length == 0 ? "Porta non disponibile; riconnessione automatica in attesa." : "Dispositivi MIDI identici: impossibile riconnettere in modo univoco.");
                        OpenPort(matching[0]); opened = true; error = null;
                        status = Input ? "Connesso" : "Porta aperta · ricezione non verificata";
                    }
                    nextRetry = now + 2000;
                }
                Tick();
                Volatile.Write(ref heartbeat, Environment.TickCount64);
                Volatile.Write(ref snapshot, new(instance, status, error, selected, devices, heartbeat, lastMessage, count, Log.Unavailable,ActiveNotes,PendingCount));
            }
            catch (Exception ex)
            {
                error = ex.Message; status = "Errore · retry automatico"; Log.Error(Input ? "midi-in" : "midi-out", ex);
                try { ClosePort(); } catch (Exception close) { Log.Error("midi-close", close); }
                opened = false; nextRetry = Environment.TickCount64 + 2000;
                Volatile.Write(ref heartbeat, Environment.TickCount64);
                Volatile.Write(ref snapshot, new(instance, status, error, desired, devices, heartbeat, lastMessage, count, Log.Unavailable,ActiveNotes,PendingCount));
            }
            if (stop.Token.WaitHandle.WaitOne(5)) break;
        }
        try { ClosePort(); } catch (Exception ex) { Log.Error("midi-close", ex); }
    }
    public void Dispose() { stop.Cancel(); if (worker?.Join(1000) == true) stop.Dispose(); }
}
internal sealed class MidiInputModule(string audioPipe) : MidiModule(audioPipe)
{
    private readonly BlockingCollection<(int Raw, long Tick)> incoming = new(256);
    private readonly HashSet<int> held = [];
    private MidiIn? input;
    private string connection = "";
    private int overflow;
    protected override bool Input => true;
    protected override void OpenPort(int index)
    {
        input = new MidiIn(index);
        input.MessageReceived += Received;
        input.ErrorReceived += ErrorReceived;
        connection = Guid.NewGuid().ToString("N"); held.Clear();
        input.Start();
    }
    private void Received(object? sender, MidiInMessageEventArgs e)
    { if (!ReferenceEquals(sender, input)) return; if (!incoming.TryAdd((e.RawMessage, Environment.TickCount64))) Interlocked.Increment(ref overflow); }
    private void ErrorReceived(object? sender, MidiInMessageEventArgs e) => Interlocked.Exchange(ref fault, 1);
    protected override void ClosePort()
    {
        if (input != null)
        {
            var old = input; input = null;
            old.MessageReceived -= Received; old.ErrorReceived -= ErrorReceived;
            try{Wire.Call(audioPipe,new("midi-disconnected",Origin:connection),500).GetAwaiter().GetResult();}catch(Exception ex){Log.Warn("midi-disconnect",ex.Message);}
            try { old.Stop(); } finally { old.Dispose(); }
        }
        held.Clear(); while (incoming.TryTake(out _)) { }
    }
    protected override void Tick()
    {
        if (Interlocked.Exchange(ref overflow, 0) > 0)
        {
            while(incoming.TryTake(out _)){}held.Clear();
            try{Wire.Call(audioPipe,new("midi-disconnected",Origin:connection),500).GetAwaiter().GetResult();}catch(Exception ex){Log.Warn("midi-overflow-release",ex.Message);}
            error="Overflow MIDI IN: rilasciati i pad premuti; eventi in eccesso scartati.";Log.Warn("midi-in",error);
        }
        for (int n = 0; n < 32 && incoming.TryTake(out var message); n++)
        {
            if(MidiCodec.TryControlChange(message.Raw,out int ccChannel,out int controller,out int value))
            {
                lastMessage=$"CH {ccChannel} · CC {controller} · {value}";count++;
                try
                {
                    var reply=Wire.Call(audioPipe,new("midi-cc",Channel:ccChannel,Controller:controller,Value:value,Origin:connection,Timestamp:message.Tick),500).GetAwaiter().GetResult();
                    error=reply.Ok?null:reply.Error;
                }
                catch(Exception ex){error=ex.Message;Log.Warn("midi-cc",ex.Message);}
                continue;
            }
            if (!MidiCodec.TryNote(message.Raw, out int ch, out int note, out bool down)) continue;
            int key = ch * 128 + note;
            if (down ? !held.Add(key) : !held.Remove(key)) continue;
            lastMessage = $"CH {ch} · nota {note} · {(down ? "ON" : "OFF")}"; count++;
            try
            {
                var reply = Wire.Call(audioPipe, new(down ? "midi-down" : "midi-up", Channel: ch, Note: note, Origin: connection, Timestamp: message.Tick), 500).GetAwaiter().GetResult();
                if (!reply.Ok) { error = reply.Error; Log.Warn("midi-input-command", reply.Error ?? "Rifiutato"); } else error = null;
            }
            catch (Exception ex) { error = "Motore audio non raggiungibile: evento perso."; Log.Warn("midi-input", ex.Message); }
        }
    }
}
internal sealed class MidiOutputModule(string audioPipe) : MidiModule(audioPipe)
{
    private readonly BlockingCollection<Request> outgoing = new(256);
    private sealed record Scheduled(int Slot,int Index,MidiSettings Settings,long Due,long Order,string? TestId);
    private readonly List<Scheduled> scheduled=[];
    private readonly ConcurrentDictionary<string,TaskCompletionSource<Reply>> tests=new();
    private long sequence;
    protected override int ActiveNotes=>outstanding.Count;
    protected override int PendingCount=>scheduled.Count;
    protected override Task<Reply> Deferred(Request r)
    {
        if(r.Op!="test-send")return base.Deferred(r);
        var list=r.Settings==null?[]:SlotActions.MidiList(r.Settings);
        if(list.Length!=1)return Task.FromResult(new Reply(false,"Seleziona un solo comando per il test."));
        Projects.ValidateMidi(list[0]);string id=Guid.NewGuid().ToString("N");
        var tcs=new TaskCompletionSource<Reply>(TaskCreationOptions.RunContinuationsAsynchronously);tests[id]=tcs;
        var reply=Command(r with{Op="send",Origin=id,Flag=true,Epoch=Volatile.Read(ref epoch),Slot=1000,Settings=r.Settings! with{MidiCommands=[list[0] with{DelayMs=0,Behavior=NoteBehavior.Pulse}]}});
        if(!reply.Ok){tests.TryRemove(id,out _);tcs.TrySetResult(reply);}return tcs.Task;
    }
    private void TestResult(string? id,string? failure=null){if(id!=null && tests.TryRemove(id,out var tcs))tcs.TrySetResult(new(failure==null,failure));}

    private readonly Dictionary<(int Slot,int Command), int> gates = [];
    private readonly Dictionary<int, long> pulses = [];
    private readonly HashSet<int> outstanding = [];
    private MidiOut? output;
    private long epoch, panicEpoch = -1;
    protected override bool Input => false;
    protected override Reply Command(Request r)
    {
        if (r.Op == "panic")
        {
            long current;
            do { current = Volatile.Read(ref panicEpoch); if (current >= r.Epoch) return new(true); }
            while (Interlocked.CompareExchange(ref panicEpoch, r.Epoch, current) != current);
            return new(true);
        }
        if (r.Op is "send" or "release" or "cancel-slot")
        {
            if (r.Settings == null && r.Op == "send") return new(false, "Parametri MIDI mancanti.");
            if (r.Settings != null){var list=SlotActions.MidiList(r.Settings);if(list.Length>16)return new(false,"Massimo 16 comandi MIDI e 16 OSC per slot.");foreach(var m in list)Projects.ValidateMidi(m);}
            return outgoing.TryAdd(r) ? new(true) : new(false, "Coda MIDI OUT piena; comando scartato.");
        }
        return base.Command(r);
    }
    protected override void OpenPort(int index)
    {
        output = new MidiOut(index);
        // Release notes known to have been active before a USB disconnect. Never replay GO.
        foreach (int key in outstanding.ToArray()) Off(key);
        outstanding.Clear(); gates.Clear(); pulses.Clear();
    }
    protected override void ClosePort()
    {
        scheduled.Clear();foreach(var id in tests.Keys)TestResult(id,"Porta MIDI non disponibile.");
        if (output == null) return;
        var old = output;
        try { foreach (int key in outstanding.ToArray()) Off(key); }
        catch (Exception ex) { Log.Warn("midi-release", ex.Message); }
        finally { output = null; old.Dispose(); gates.Clear(); pulses.Clear(); }
    }
    private void Short(int raw)
    { if (output == null) throw new InvalidOperationException("Porta MIDI OUT non aperta: comando non inviato."); output.Send(raw); count++; }
    private void Off(int key) { Short(MidiCodec.Note(key / 128 + 1, key % 128, 0, false)); outstanding.Remove(key); }
    private void Release(int slot)
    {
        var previous=gates.Where(x=>x.Key.Slot==slot).ToArray();
        foreach(var gate in previous)gates.Remove(gate.Key);
        foreach(int key in previous.Select(x=>x.Value).Distinct())if(!gates.Values.Contains(key) && !pulses.ContainsKey(key))Off(key);
    }
    protected override void Tick()
    {
        long pendingPanic = Interlocked.Exchange(ref panicEpoch, -1);
        if (pendingPanic > epoch)
        {
            epoch = Math.Max(epoch, pendingPanic); scheduled.Clear();foreach(var id in tests.Keys)TestResult(id,"Sequenza annullata.");gates.Clear(); pulses.Clear();
            foreach (int key in outstanding.ToArray()) { if (output != null) Off(key); }
        }
        for (int n = 0; n < 32 && outgoing.TryTake(out var r); n++)
        {
            if (r.Epoch < epoch){if(r.Flag)TestResult(r.Origin,"Sequenza annullata.");continue;}
            if (r.Op == "send" && r.Timestamp > 0 && Environment.TickCount64 - r.Timestamp > 500) { error = "Comando scaduto, non ripetuto.";if(r.Flag)TestResult(r.Origin,error); continue; }
            if(r.Epoch>epoch)
            {scheduled.Clear();foreach(var id in tests.Keys)if(id!=r.Origin)TestResult(id,"Sequenza annullata.");gates.Clear();pulses.Clear();foreach(int key in outstanding.ToArray())if(output!=null)Off(key);epoch=r.Epoch;}
            if(r.Op is "release" or "cancel-slot")
            {
                scheduled.RemoveAll(p=>p.Slot==r.Slot && (r.Op=="cancel-slot" || p.Settings.Kind==MidiKind.Note && p.Settings.Behavior==NoteBehavior.Gate));
                Release(r.Slot);continue;
            }
            if (output == null) { error = "Comando MIDI scartato: porta non disponibile."; Log.Warn("midi-out", error);if(r.Flag)TestResult(r.Origin,error); continue; }
            Release(r.Slot);scheduled.RemoveAll(p=>p.Slot==r.Slot);
            var commands=SlotActions.MidiList(r.Settings!);
            if(scheduled.Count+commands.Length>1024){error="Coda MIDI OUT piena; comando scartato.";if(r.Flag)TestResult(r.Origin,error);continue;}
            long due=Environment.TickCount64;
            for(int index=0;index<commands.Length;index++)
            {due+=commands[index].DelayMs;scheduled.Add(new(r.Slot,index,commands[index],due,++sequence,r.Flag?r.Origin:null));}
            DispatchDue();
        }
        DispatchDue();
        foreach (int key in pulses.Where(x => x.Value <= Environment.TickCount64).Select(x => x.Key).ToArray())
        { pulses.Remove(key); if (!gates.Values.Contains(key)) Off(key); }
    }
    private void DispatchDue()
    {
        foreach(var item in scheduled.Where(p=>p.Due<=Environment.TickCount64).OrderBy(p=>p.Due).ThenBy(p=>p.Order).ToArray())
        {
            scheduled.Remove(item);
            if(Environment.TickCount64-item.Due>500){TestResult(item.TestId,"Comando scaduto, non ripetuto.");continue;}
            var m=item.Settings;
            try
            {
                if(m.Kind==MidiKind.MSC){output!.SendBuffer(MidiCodec.Msc(m));count++;lastMessage=$"MSC {m.Command} · cue {m.Cue}";}
                else if(m.Kind==MidiKind.ControlChange){Short(MidiCodec.ControlChange(m.Channel,m.Controller,m.ControlValue));lastMessage=$"CC {m.Controller} · {m.ControlValue}";}
                else if(m.Kind==MidiKind.ProgramChange){Short(MidiCodec.ProgramChange(m.Channel,m.Program));lastMessage=$"Program {m.Program}";}
                else
                {
                    int key=(m.Channel-1)*128+m.Note;Short(MidiCodec.Note(m.Channel,m.Note,m.Velocity,true));outstanding.Add(key);
                    if(m.Behavior==NoteBehavior.Gate)gates[(item.Slot,item.Index)]=key;else pulses[key]=Environment.TickCount64+m.PulseMs;
                    lastMessage=$"CH {m.Channel} · nota {m.Note} · ON";
                }
                TestResult(item.TestId);
            }
            catch(Exception ex){TestResult(item.TestId,ex.Message);throw;}
        }

    }

}
