using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using StagePad.Core;
namespace StagePad.App;
internal sealed class OscOutput : IDisposable
{
    private sealed record Batch(OscCommand[] Commands,int Slot,long Tick,CancellationTokenSource Source,TaskCompletionSource<Reply>? Completion);
    private readonly Channel<Batch> queue=Channel.CreateBounded<Batch>(256);
    private readonly CancellationTokenSource stop=new();
    private readonly SemaphoreSlim capacity=new(32);
    private readonly object gate=new();
    private readonly Dictionary<int,CancellationTokenSource> bySlot=[];
    private readonly HashSet<Task> active=[];
    private readonly Task worker;
    private readonly Func<string,CancellationToken,Task<IPAddress[]>> resolve;
    private long sent;
    private int pending;
    private string? error;
    public string? Error=>Volatile.Read(ref error);
    public long Sent=>Interlocked.Read(ref sent);
    public int Pending=>Volatile.Read(ref pending);
    public OscOutput(Func<string,CancellationToken,Task<IPAddress[]>>? resolver=null){resolve=resolver??((host,ct)=>Dns.GetHostAddressesAsync(host,ct));worker=Task.Run(Run);}
    public bool Post(OscCommand[] commands,int slot=-1)=>Enqueue(commands,slot,null);
    public Task<Reply> Test(OscCommand command)
    {
        OscCodec.Validate(command);var completion=new TaskCompletionSource<Reply>(TaskCreationOptions.RunContinuationsAsynchronously);
        if(!Enqueue([command with{DelayMs=0}],-1000,completion))completion.TrySetResult(new(false,Error));return completion.Task;
    }
    private bool Enqueue(OscCommand[] commands,int slot,TaskCompletionSource<Reply>? completion)
    {
        if(commands.Length==0){completion?.TrySetResult(new(true));return true;}
        var source=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);CancellationTokenSource? old=null;
        lock(gate)
        {
            if(queue.Writer.TryWrite(new(commands,slot,Environment.TickCount64,source,completion)))
            {bySlot.TryGetValue(slot,out old);bySlot[slot]=source;Interlocked.Increment(ref pending);}
            else{source.Dispose();Volatile.Write(ref error,"Coda OSC piena: comando scartato.");return false;}
        }
        try{old?.Cancel();}catch(ObjectDisposedException){}return true;
    }
    public void Cancel(int slot){CancellationTokenSource? source;lock(gate)bySlot.TryGetValue(slot,out source);try{source?.Cancel();}catch(ObjectDisposedException){} }
    public void Panic()
    {
        CancellationTokenSource[] sources;lock(gate){sources=bySlot.Values.ToArray();bySlot.Clear();}
        foreach(var source in sources)try{source.Cancel();}catch(ObjectDisposedException){}
    }
    private void Finish(Batch batch,string? failure)
    {
        lock(gate){if(bySlot.GetValueOrDefault(batch.Slot)==batch.Source)bySlot.Remove(batch.Slot);}
        batch.Source.Dispose();Interlocked.Decrement(ref pending);batch.Completion?.TrySetResult(new(failure==null,failure));
        if(failure!=null && failure!="Sequenza annullata.")Volatile.Write(ref error,failure);
    }
    private async Task Run()
    {
        try
        {
            await foreach(var batch in queue.Reader.ReadAllAsync(stop.Token))
            {
                if(batch.Source.IsCancellationRequested){Finish(batch,"Sequenza annullata.");continue;}
                if(Environment.TickCount64-batch.Tick>1000){Finish(batch,"Comando OSC scaduto: non ripetuto.");continue;}
                if(!capacity.Wait(0)){Finish(batch,"Coda OSC piena: comando scartato.");continue;}
                var task=Send(batch);lock(gate)active.Add(task);
                _=task.ContinueWith(t=>{lock(gate)active.Remove(t);},TaskScheduler.Default);
            }
        }
        catch(OperationCanceledException) when(stop.IsCancellationRequested){}
        finally{while(queue.Reader.TryRead(out var batch))Finish(batch,"Sequenza annullata.");}
    }
    private async Task Send(Batch batch)
    {
        string? failure=null;
        try
        {
            foreach(var command in batch.Commands)
            {
                var ct=batch.Source.Token;ct.ThrowIfCancellationRequested();
                if(command.DelayMs>0)await Task.Delay(command.DelayMs,ct);
                try
                {
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(1000);
                    var addresses=IPAddress.TryParse(command.Host,out var address)?[address]:await resolve(command.Host,timeout.Token);
                    ct.ThrowIfCancellationRequested();
                    var target=addresses.FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork)??addresses.FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetworkV6)??throw new SocketException();
                    using var socket=new Socket(target.AddressFamily,SocketType.Dgram,ProtocolType.Udp);
                    await socket.SendToAsync(OscCodec.Encode(command),SocketFlags.None,new IPEndPoint(target,command.Port),timeout.Token);
                    Interlocked.Increment(ref sent);
                }
                catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
                catch(Exception ex){failure=$"OSC {command.Address}: {ex.Message}";Log.Error("osc-out",ex);}
            }
            if(failure==null)Volatile.Write(ref error,null);
        }
        catch(OperationCanceledException){failure="Sequenza annullata.";}
        catch(Exception ex){failure=ex.Message;Log.Error("osc-out",ex);}
        finally{capacity.Release();Finish(batch,failure);}
    }
    public void Dispose()
    {
        Panic();queue.Writer.TryComplete();stop.Cancel();try{worker.Wait(1500);Task[] tasks;lock(gate)tasks=active.ToArray();Task.WhenAll(tasks).Wait(1500);}catch(AggregateException){}
    }
}
