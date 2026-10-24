using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086;

/// <summary>Rdzeń Intel 8086; obsługuje bazowe transfery, adresowanie ModR/M i XLAT.</summary>
public sealed partial class Intel8086Core : ICpuCore<Intel8086State>
{
    private readonly IAddressBus _bus;
    private Intel8086State _state;
    private int _instructionTState;
    private int _instructionCycles;
    private int _queueCountAtStart;
    private int _xlatPrefixLength;
    private uint _nextFetchAddress;
    private long _cycles;
    private byte _memoryModRm;
    private ushort _leaOffset;
    private int _leaDisplacementBytes;
    private int _leaEffectiveAddressCycles;
    private Intel8086SegmentOverride _xlatSegmentOverride;
    private uint _xlatPhysicalAddress;
    private string _xlatSegmentStatus = "DS";
    private byte _xlatBhe;

    public Intel8086Core(IAddressBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        _bus = bus;
        _state = Intel8086State.Initial();
    }

    public ref Intel8086State State => ref _state;

    public int Pending => _instructionTState == 0 ? 0 : _instructionCycles - _instructionTState;

    public bool Jammed => false;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public ushort InstructionCs { get; private set; }

    public byte Opcode { get; private set; }

    public Intel8086BusCycle LastBusCycle { get; private set; }

    public void Reset(ushort pc = 0)
    {
        _state = Intel8086State.Initial(ip: pc);
        _instructionTState = 0;
        _instructionCycles = 0;
        _queueCountAtStart = 0;
        _xlatPrefixLength = 0;
        _nextFetchAddress = 0;
        _memoryModRm = 0;
        _leaOffset = 0;
        _leaDisplacementBytes = 0;
        _leaEffectiveAddressCycles = 0;
        _xlatSegmentOverride = Intel8086SegmentOverride.None;
        _xlatPhysicalAddress = 0;
        _xlatSegmentStatus = "DS";
        _xlatBhe = 0;
        _cycles = 0;
        InstructionPc = pc;
        InstructionCs = 0;
        Opcode = 0;
        LastBusCycle = default;
    }

    public void Tick()
    {
        _cycles++;
        if (_instructionTState == 0)
        {
            BeginInstruction();
        }

        _instructionTState++;
        if (Opcode == 0x8D)
        {
            TickLea();
            return;
        }

        if (Opcode == 0xD7)
        {
            TickXlat();
            return;
        }

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 2)
        {
            if (Opcode is 0x88 or 0x8A)
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                // SST pokazuje dodatkowe przesunięcie kolejki podczas kończenia
                // tej rejestrowej formy, mimo że IP przesuwa się o dwa bajty.
                PopQueue();
                _state.IP++;
                int reg = (modRm >> 3) & 7;
                int rm = modRm & 7;
                SetByteRegister(Opcode == 0x88 ? rm : reg, GetByteRegister(Opcode == 0x88 ? reg : rm));
                LastBusCycle = IdleCycle('S', modRm);
                _instructionTState = 0;
                return;
            }

            if (Opcode == 0x90)
            {
                PopQueue();
            }

            LastBusCycle = IdleCycle('-', 0);
        }
        else if (Opcode is >= 0xB0 and <= 0xB7 && _instructionTState == 3)
        {
            byte immediate = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            SetByteRegister(Opcode & 7, immediate);
            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? FetchCycle("T1", 'S', immediate)
                : IdleCycle('S', immediate);
        }
        else
        {
            if (Opcode is >= 0xB0 and <= 0xB7 && _instructionTState == 4)
            {
                PopQueue();
            }

            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? Opcode == 0x90
                    ? FetchCycle("T1", '-', 0)
                    : FetchContinuationCycle()
                : IdleCycle('-', 0);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
    }

    public void Stall()
    {
        _cycles++;
    }

    private void BeginInstruction()
    {
        if (_state.QueueCount == 0)
        {
            throw new InvalidOperationException("Cannot decode an empty prefetch queue.");
        }

        byte opcode = _state.Queue[_state.QueueHead];
        _xlatPrefixLength = 0;
        _xlatSegmentOverride = Intel8086SegmentOverride.None;
        if (TryGetXlatSegmentOverride(opcode, out Intel8086SegmentOverride segmentOverride))
        {
            if (_state.QueueCount < 2 || _state.Queue[_state.QueueHead + 1] != 0xD7)
            {
                throw new InvalidOperationException("W tym etapie prefiks segmentowy jest obsługiwany wyłącznie z XLAT.");
            }

            _xlatPrefixLength = 1;
            _xlatSegmentOverride = segmentOverride;
            opcode = 0xD7;
        }

        if (opcode is not 0x90 and not (>= 0xB0 and <= 0xB7) and not 0x88 and not 0x8A and not 0x8D and not 0xD7)
        {
            throw new InvalidOperationException($"Opcode {opcode:X2} is not implemented yet.");
        }

        InstructionPc = _state.IP;
        InstructionCs = _state.CS;
        Opcode = opcode;
        _queueCountAtStart = _state.QueueCount;
        _nextFetchAddress = PhysicalAddress(_state.CS, (ushort)(_state.IP + _queueCountAtStart));
        if (opcode is 0x88 or 0x8A)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("MOV r/m8,r8 requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1];
            if ((modRm >> 6) != 3)
            {
                throw new InvalidOperationException($"MOV {opcode:X2} memory ModR/M forms are not implemented yet.");
            }
        }

        if (opcode == 0x8D)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("LEA wymaga bajtu ModR/M w kolejce prefetch.");
            }

            _memoryModRm = _state.Queue[_state.QueueHead + 1];
            int mod = _memoryModRm >> 6;
            int rm = _memoryModRm & 7;
            if (mod == 3)
            {
                throw new InvalidOperationException("LEA wymaga operandu pamięciowego (ModR/M mod != 11).");
            }

            _leaDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
            if (_state.QueueCount < 2 + _leaDisplacementBytes)
            {
                throw new InvalidOperationException("LEA wymaga bajtów przesunięcia w kolejce prefetch.");
            }

            _leaEffectiveAddressCycles = rm switch
            {
                0 or 3 => 7,
                1 or 2 => 8,
                4 or 5 or 7 => 5,
                6 => mod == 0 ? 6 : 5,
                _ => throw new InvalidOperationException("Nieprawidłowe pole r/m dla LEA."),
            };
            ushort displacement = _leaDisplacementBytes switch
            {
                0 => 0,
                1 => _state.Queue[_state.QueueHead + 2],
                _ => (ushort)(_state.Queue[_state.QueueHead + 2] | (_state.Queue[_state.QueueHead + 3] << 8)),
            };
            _leaOffset = Intel8086EffectiveAddressDecoder.Decode(_memoryModRm, in _state, displacement).Offset;
        }

        if (opcode == 0xD7)
        {
            ushort segment = _xlatSegmentOverride switch
            {
                Intel8086SegmentOverride.None => _state.DS,
                Intel8086SegmentOverride.Es => _state.ES,
                Intel8086SegmentOverride.Cs => _state.CS,
                Intel8086SegmentOverride.Ss => _state.SS,
                Intel8086SegmentOverride.Ds => _state.DS,
                _ => throw new InvalidOperationException("Nieznany prefiks segmentowy XLAT."),
            };
            ushort offset = unchecked((ushort)(_state.BX + (byte)_state.AX));
            _xlatPhysicalAddress = (uint)(((segment << 4) + offset) & 0xFFFFF);
            _xlatSegmentStatus = GetSegmentStatus(_xlatSegmentOverride);
            _xlatBhe = (byte)((_xlatPhysicalAddress & 1) == 0 ? 1 : 0);
        }

        _instructionCycles = opcode == 0x90 ? 3 : opcode == 0x8D
            ? 2 + _leaEffectiveAddressCycles + ((_memoryModRm >> 6) == 0 ? 0 : 4)
            : opcode == 0xD7 ? (_xlatPrefixLength == 0 ? 17 - _queueCountAtStart : 8 + _queueCountAtStart)
            : 4;
        if (opcode is 0x88 or 0x8A)
        {
            _instructionCycles = 2;
        }
    }

    private void PopQueue()
    {
        if (_state.QueueCount == 0)
        {
            throw new InvalidOperationException("Cannot consume an empty prefetch queue.");
        }

        _state.QueueHead++;
        _state.QueueCount--;
    }

    private bool ShouldFetchAtInstructionEnd() => _queueCountAtStart <= 5;

    private static bool TryGetXlatSegmentOverride(byte value, out Intel8086SegmentOverride segmentOverride)
    {
        segmentOverride = value switch
        {
            0x26 => Intel8086SegmentOverride.Es,
            0x2E => Intel8086SegmentOverride.Cs,
            0x36 => Intel8086SegmentOverride.Ss,
            0x3E => Intel8086SegmentOverride.Ds,
            _ => Intel8086SegmentOverride.None,
        };
        return segmentOverride != Intel8086SegmentOverride.None;
    }

    private static string GetSegmentStatus(Intel8086SegmentOverride segmentOverride) => segmentOverride switch
    {
        Intel8086SegmentOverride.None or Intel8086SegmentOverride.Ds => "DS",
        Intel8086SegmentOverride.Es => "ES",
        Intel8086SegmentOverride.Cs => "CS",
        Intel8086SegmentOverride.Ss => "SS",
        _ => throw new ArgumentOutOfRangeException(nameof(segmentOverride)),
    };

    private Intel8086BusCycle FetchCycle(string tState, char queueStatus, byte queueData) => new(
        1,
        _nextFetchAddress,
        "--",
        "---",
        "---",
        0,
        0,
        "CODE",
        tState,
        queueStatus,
        queueData);

    private Intel8086BusCycle FetchContinuationCycle() => new(
        0,
        _nextFetchAddress,
        "CS",
        "R--",
        "---",
        0,
        0,
        "CODE",
        "T2",
        '-',
        0);

    private void SetByteRegister(int register, byte value)
    {
        switch (register)
        {
            case 0:
                _state.AX = (ushort)((_state.AX & 0xFF00) | value);
                break;
            case 1:
                _state.CX = (ushort)((_state.CX & 0xFF00) | value);
                break;
            case 2:
                _state.DX = (ushort)((_state.DX & 0xFF00) | value);
                break;
            case 3:
                _state.BX = (ushort)((_state.BX & 0xFF00) | value);
                break;
            case 4:
                _state.AX = (ushort)((_state.AX & 0x00FF) | (value << 8));
                break;
            case 5:
                _state.CX = (ushort)((_state.CX & 0x00FF) | (value << 8));
                break;
            case 6:
                _state.DX = (ushort)((_state.DX & 0x00FF) | (value << 8));
                break;
            case 7:
                _state.BX = (ushort)((_state.BX & 0x00FF) | (value << 8));
                break;
        }
    }

    private byte GetByteRegister(int register) => register switch
    {
        0 => (byte)_state.AX,
        1 => (byte)_state.CX,
        2 => (byte)_state.DX,
        3 => (byte)_state.BX,
        4 => (byte)(_state.AX >> 8),
        5 => (byte)(_state.CX >> 8),
        6 => (byte)(_state.DX >> 8),
        7 => (byte)(_state.BX >> 8),
        _ => throw new ArgumentOutOfRangeException(nameof(register)),
    };

    private static Intel8086BusCycle IdleCycle(char queueStatus, byte queueData) => new(
        0,
        0,
        "--",
        "---",
        "---",
        0,
        0,
        "PASV",
        "Ti",
        queueStatus,
        queueData);

    private static uint PhysicalAddress(ushort segment, ushort offset) =>
        (uint)(((segment << 4) + offset) & 0xFFFFF);
}
