using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace StagePad.Core;
public enum PlayMode { OneShot, Hold, Protected, TogglePause }
public enum SlotAction { Audio, Midi, Both, Osc, AudioOsc, MidiOsc, All }
public enum MidiKind { Note, MSC, ControlChange, ProgramChange }
public enum NoteBehavior { Pulse, Gate }
public enum MscCommand { Go = 1, Stop = 2, Resume = 3 }
public enum MscAddress { Default, ExecDotPage, ExecSpacePage }
public sealed record MidiSettings
{
    public int DelayMs {get;init;}
    public MidiKind Kind { get; init; }
    public int Channel { get; init; } = 1;
    public int Note { get; init; } = 60;
    public int Velocity { get; init; } = 127;
    public int Controller { get; init; } = 1;
    public int ControlValue { get; init; } = 127;
    public int Program { get; init; }
    public NoteBehavior Behavior { get; init; }
    public int PulseMs { get; init; } = 100;
    public int DeviceId { get; init; } = 127;
    public int CommandFormat { get; init; } = 1;
    public MscCommand Command { get; init; } = MscCommand.Go;
    public string Cue { get; init; } = "1.000";
    public MscAddress Address { get; init; }
    public int Executor { get; init; } = 1;
    public int Page { get; init; } = 1;
}
public sealed record SlotSettings
{
    public string Name { get; init; } = "Slot";
    public bool? AutoName {get;init;}
    public bool EmergencyPauseAll {get;init;}
    public string Color { get; init; } = "#4A78BC";
    public string? File { get; init; }
    public float Volume { get; init; } = 1;
    public PlayMode Mode { get; init; }
    public SlotAction Action { get; init; }
    public int PadChannel { get; init; } = 1;
    public int PadNote { get; init; } = -1;
    public int FadeInMs { get; init; } = 5;
    public int FadeOutMs { get; init; } = 5;
    public MidiSettings Midi { get; init; } = new();
    public MidiSettings[]? MidiCommands { get; init; }
    public OscCommand[] OscCommands { get; init; } = [];
}
public sealed record RoutingSettings
{
    public string? AudioDevice { get; init; }
    public int BufferMs { get; init; } = 40;
    public string? MidiInput { get; init; }
    public string? MidiOutput { get; init; }
}
public sealed record GridLayout(int Rows=4,int Columns=4);
public sealed record ProjectDocument
{
    public int Schema { get; init; } = 1;
    public int SlotCount { get; init; } = 16;
    public GridLayout Grid { get; init; } = new();
    public GridLayout SetupGrid { get; init; } = new();
    public string Name { get; init; } = "Nuovo progetto";
    public float Master { get; init; } = 1;
    public MidiCcBinding MasterControl {get;init;}=new();
    public MidiCcBinding CurrentTrackControl {get;init;}=new();
    public int MemoryMiB { get; init; } = 512;
    public RoutingSettings Routing { get; init; } = new();
    public SlotSettings[] Slots { get; init; } = Enumerable.Range(0, 16).Select(Projects.DefaultSlot).ToArray();
}
public static partial class Projects
{
    public const int MaxSlots=100,MaxGridSize=10;
    public static SlotSettings DefaultSlot(int index)
    {
        if(index is <0 or >=MaxSlots)throw new ArgumentOutOfRangeException(nameof(index));
        return new SlotSettings { Name=$"Slot {index+1}",AutoName=true,Midi=new(){Note=(60+index)%128} };
    }
    public static GridLayout FitGrid(GridLayout layout,int count)
    {
        int columns=Math.Clamp(layout.Columns,1,MaxGridSize);
        int rows=Math.Clamp(layout.Rows,1,MaxGridSize);
        if(rows*columns<count)
        {
            rows=Math.Min(MaxGridSize,(count+columns-1)/columns);
            columns=Math.Max(columns,(count+rows-1)/rows);
        }
        return new(rows,columns);
    }
    public static ProjectDocument Resize(ProjectDocument project,int count)
    {
        if(count is <1 or >MaxSlots)throw new InvalidDataException("Numero slot: da 1 a 100.");
        // Keep the hidden tail in the project, so shrinking and expanding does
        // not discard names, mappings or audio paths.
        var slots=Enumerable.Range(0,Math.Max(count,project.Slots.Length))
            .Select(i=>i<project.Slots.Length?project.Slots[i]:DefaultSlot(i)).ToArray();
        var next=project with { SlotCount=count,Slots=slots,Grid=FitGrid(project.Grid,count),SetupGrid=FitGrid(project.SetupGrid,count) };
        Validate(next);return next;
    }
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")] private static partial Regex ColorPattern();
    [GeneratedRegex("^[0-9]{1,5}(\\.[0-9]{1,3})?$")] private static partial Regex CuePattern();
    public static void Validate(ProjectDocument p)
    {
        if (p.Schema != 1 || p.Slots is not { Length: >=1 and <=MaxSlots } || p.SlotCount is <1 or >MaxSlots || p.SlotCount>p.Slots.Length || p.Routing == null) throw new InvalidDataException("Progetto non valido: schema 1 e da 1 a 100 slot richiesti.");
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 100 || !float.IsFinite(p.Master) || p.Master is < 0 or > 1 || p.MemoryMiB is < 128 or > 4096) throw new InvalidDataException("Nome, master o budget RAM non validi.");
        if ((p.Routing.AudioDevice?.Length ?? 0) > 512 || (p.Routing.MidiInput?.Length ?? 0) > 512 || (p.Routing.MidiOutput?.Length ?? 0) > 512) throw new InvalidDataException("Identificatore dispositivo troppo lungo.");
        if (p.Routing.BufferMs is not (20 or 40 or 80)) throw new InvalidDataException("Buffer audio non valido.");
        if(p.Grid==null || p.Grid.Rows is <1 or >MaxGridSize || p.Grid.Columns is <1 or >MaxGridSize || p.Grid.Rows*p.Grid.Columns<p.SlotCount)throw new InvalidDataException("Griglia: righe e colonne da 1 a 10, con una cella per ogni slot attivo.");
        if(p.SetupGrid==null || p.SetupGrid.Rows is <1 or >MaxGridSize || p.SetupGrid.Columns is <1 or >MaxGridSize || p.SetupGrid.Rows*p.SetupGrid.Columns<p.SlotCount)throw new InvalidDataException("Griglia Setup: righe e colonne da 1 a 10, con una cella per ogni slot attivo.");
        MidiCcBinding.Validate(p.MasterControl,p.CurrentTrackControl);
        var mapped = new HashSet<int>();
        foreach (var s in p.Slots)
        {
            ValidateSlot(s);
            if (s.PadNote >= 0 && !mapped.Add((s.PadChannel - 1) * 128 + s.PadNote)) throw new InvalidDataException("Due slot sono associati alla stessa nota/canale del pad.");
        }
    }
    public static void ValidateSlot(SlotSettings s)
    {
        if (s == null || s.Midi == null || s.Name is null || (s.AutoName==false && string.IsNullOrWhiteSpace(s.Name)) || s.Name.Length > 100 || !ColorPattern().IsMatch(s.Color ?? "")) throw new InvalidDataException("Nome o colore slot non valido (#RRGGBB).");
        if ((s.File?.Length ?? 0) > 1024) throw new InvalidDataException("Percorso audio troppo lungo (massimo 1024 caratteri).");
        if (!Enum.IsDefined(s.Mode) || !Enum.IsDefined(s.Action) || !float.IsFinite(s.Volume) || s.Volume is < 0 or > 1 || s.PadChannel is < 1 or > 16 || s.PadNote is < -1 or > 127 || s.FadeInMs is < 0 or > 30000 || s.FadeOutMs is < 0 or > 30000) throw new InvalidDataException("Parametri audio/mappatura non validi.");
        ValidateMidi(s.Midi);
        if(s.MidiCommands is {Length: >16} || s.OscCommands is null || s.OscCommands.Length>16)throw new InvalidDataException("Massimo 16 comandi MIDI e 16 OSC per slot.");
        if(s.MidiCommands!=null)foreach(var command in s.MidiCommands){if(command==null)throw new InvalidDataException("Comando MIDI non valido.");ValidateMidi(command);}
        foreach(var command in s.OscCommands)OscCodec.Validate(command);
        if((s.MidiCommands??[s.Midi]).Sum(m=>(long)m.DelayMs)>60000 || s.OscCommands.Sum(o=>(long)o.DelayMs)>60000)throw new InvalidDataException("La sequenza non può superare 60 secondi.");
        if(s.MidiCommands!=null || s.OscCommands.Length>0)
        {
            // Bound snapshots and recovery files even with 100 fully configured slots.
            int size=JsonSerializer.SerializeToUtf8Bytes(new{s.MidiCommands,s.OscCommands}).Length;
            if(size>6000)throw new InvalidDataException("Comandi dello slot troppo grandi: riduci argomenti o messaggi (massimo 6000 byte).");
        }
        if(SlotActions.Midi(s.Action) && s.MidiCommands is {Length:0})throw new InvalidDataException("Aggiungi almeno un comando MIDI.");
        if(SlotActions.Osc(s.Action) && s.OscCommands.Length==0)throw new InvalidDataException("Aggiungi almeno un comando OSC.");
    }
    public static void ValidateMidi(MidiSettings m)
    {
        if (m is null || m.DelayMs is <0 or >30000 || m.Controller is <0 or >127 || m.ControlValue is <0 or >127 || m.Program is <0 or >127 || !Enum.IsDefined(m.Kind) || !Enum.IsDefined(m.Behavior) || !Enum.IsDefined(m.Command) || !Enum.IsDefined(m.Address) || m.Channel is < 1 or > 16 || m.Note is < 0 or > 127 || m.Velocity is < 1 or > 127 || m.PulseMs is < 10 or > 10000 || m.DeviceId is < 0 or > 127 || m.CommandFormat is not (1 or 2 or 127) || m.Executor is < 1 or > 999 || m.Page != 1 || !CuePattern().IsMatch(m.Cue ?? "")) throw new InvalidDataException("Parametri MIDI/MSC non validi. Indirizzamento MSC limitato a pagina 1 in questa versione.");
    }
    public static string NormalizeCue(string cue)
    {
        if (!CuePattern().IsMatch(cue)) throw new InvalidDataException("Cue non valida, esempio: 21.500.");
        return decimal.Parse(cue, CultureInfo.InvariantCulture).ToString("0.000", CultureInfo.InvariantCulture);
    }
}
public static class MidiCodec
{
    public static int Note(int channel, int note, int velocity, bool on)
    {
        if (channel is < 1 or > 16 || note is < 0 or > 127 || velocity is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(note));
        return (on ? 0x90 : 0x80) | (channel - 1) | (note << 8) | (velocity << 16);
    }
    public static bool TryControlChange(int raw,out int channel,out int controller,out int value)
    {
        channel=(raw&15)+1;controller=(raw>>8)&127;value=(raw>>16)&127;return (raw&0xF0)==0xB0;
    }
    public static int ControlChange(int channel,int controller,int value)
    {
        if(channel is <1 or >16 || controller is <0 or >127 || value is <0 or >127)throw new ArgumentOutOfRangeException(nameof(controller));
        return 0xB0|(channel-1)|(controller<<8)|(value<<16);
    }
    public static int ProgramChange(int channel,int program)
    {
        if(channel is <1 or >16 || program is <0 or >127)throw new ArgumentOutOfRangeException(nameof(program));
        return 0xC0|(channel-1)|(program<<8);
    }
    public static bool TryNote(int raw, out int channel, out int note, out bool down)
    {
        int status = raw & 0xF0;
        channel = (raw & 15) + 1; note = (raw >> 8) & 127;
        down = status == 0x90 && ((raw >> 16) & 127) > 0;
        return status is 0x80 or 0x90;
    }
    public static byte[] Msc(MidiSettings m)
    {
        Projects.ValidateMidi(m);
        string cue = m.Command == MscCommand.Go ? Projects.NormalizeCue(m.Cue) : "0.000";
        string data = m.Address == MscAddress.Default ? (m.Command == MscCommand.Go ? cue : "") : cue + "\0" + m.Executor + (m.Address == MscAddress.ExecDotPage ? "." : " ") + m.Page;
        return [0xF0, 0x7F, (byte)m.DeviceId, 0x02, (byte)m.CommandFormat, (byte)m.Command, .. Encoding.ASCII.GetBytes(data), 0xF7];
    }
}
