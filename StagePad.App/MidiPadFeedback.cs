using NAudio.Midi;
using StagePad.Core;
namespace StagePad.App;
// Used only by the isolated MIDI OUT worker. It never runs on the audio or UI thread.
internal sealed class MidiPadFeedback : IDisposable
{
    private MidiOut? output;
    private string? selected;
    private long retryAt,lastOffer;
    private sealed record Desired(PadLedFrame? Frame,string? CommandOutput);
    private Desired? desired;
    private readonly CancellationTokenSource stop=new();
    private Thread? worker;
    private string status="OFF";
    private readonly Dictionary<int,int> sent=[];
    public string Status {get=>Volatile.Read(ref status);private set=>Volatile.Write(ref status,value);}
    public void Apply(PadLedFrame? frame,string? commandOutput)
    {
        if(frame?.OutputId==null && worker==null)return;
        long now=Environment.TickCount64;if(now-lastOffer<80)return;lastOffer=now;
        Volatile.Write(ref desired,new Desired(frame,commandOutput));
        if(worker==null){worker=new Thread(Run){IsBackground=true,Name="StagePad LED feedback"};worker.Start();}
    }
    private void Run()
    {
        try{while(!stop.IsCancellationRequested){var value=Volatile.Read(ref desired);ApplyCore(value?.Frame,value?.CommandOutput);if(stop.Token.WaitHandle.WaitOne(40))break;}}
        catch(Exception ex){Status=ex.Message;Log.Error("pad-led-worker",ex);}
        finally{try{ClosePort();}catch(Exception ex){Log.Warn("pad-led-close",ex.Message);}}
    }
    private void ApplyCore(PadLedFrame? frame,string? commandOutput)
    {
        long now=Environment.TickCount64;
        if(frame==null || frame.Tick<=0 || now-frame.Tick>700){ClosePort();Status="OFF";return;}
        if(frame.OutputId is not {} id){ClosePort();selected=null;Status="OFF";return;}
        if(id==commandOutput){ClosePort();Status="Porta LED e comandi MIDI devono essere diverse.";return;}
        if(id!=selected){ClosePort();selected=id;retryAt=0;}
        if(now<retryAt)return;
        try
        {
            if(output==null)
            {
                var matches=Enumerable.Range(0,MidiOut.NumberOfDevices).Where(i=>{var c=MidiOut.DeviceInfo(i);return $"{c.Manufacturer}:{c.ProductId}:{c.ProductName}"==id;}).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("Porta LED non disponibile o ambigua.");
                output=new MidiOut(matches[0]);sent.Clear();
            }
            var next=frame.Messages.ToDictionary(raw=>raw&0xFFFF);
            foreach(int key in sent.Keys.Where(key=>!next.ContainsKey(key)).ToArray()){output.Send(key);sent.Remove(key);}
            foreach(var pair in next)if(!sent.TryGetValue(pair.Key,out int old) || old!=pair.Value){output.Send(pair.Value);sent[pair.Key]=pair.Value;}
            Status="Connesso · profilo LED generico";
        }
        catch(Exception ex){ClosePort();retryAt=now+2000;Status=ex.Message;Log.Warn("pad-led",ex.Message);}
    }
    private void ClosePort()
    {
        if(output==null)return;
        try{foreach(int key in sent.Keys)output.Send(key);}catch(Exception ex){Log.Warn("pad-led-off",ex.Message);}
        finally{output.Dispose();output=null;sent.Clear();}
    }
    public void Dispose()
    {
        stop.Cancel();if(worker==null || worker.Join(500))stop.Dispose();
    }
}
