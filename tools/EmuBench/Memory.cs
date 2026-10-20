using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace EmuBench;

/// <summary>
/// Pamięć emulowana: płaska, bankowana i stronicowana z TLB,
/// plus koszt dekodowania wartości o różnych szerokościach i wyrównaniach.
/// </summary>
internal static unsafe class Memory
{
    private const int MemorySize = 1 << 20; // 1 MB
    private const int Mask = MemorySize - 1;
    private const int OffsetCount = 1 << 13;

    public static IReadOnlyList<Harness.Result> Run(int ops, int rounds)
    {
        var mem = new byte[MemorySize + 8];
        new Random(7).NextBytes(mem);

        int[] offsets = MakeOffsets();

        // --- pamięć bankowana: 32 banki × 32 KB = 1 MB (ten sam footprint co płaska) ---
        byte[][] banks = new byte[32][];
        for (int i = 0; i < banks.Length; i++)
        {
            banks[i] = new byte[32 * 1024];
            new Random(i).NextBytes(banks[i]);
        }

        int[] bankMap = new int[32];
        for (int i = 0; i < bankMap.Length; i++)
        {
            bankMap[i] = (i * 13) & 31;
        }

        // --- pamięć stronicowana z TLB: 512 stron × 2 KB = 1 MB ---
        var pages = new byte[512][];
        int[] tlb = new int[512];
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i] = new byte[2 * 1024];
            tlb[i] = i;
        }

        return
        [
            new("read 8-bit flat", Harness.Measure(ops, () => Read8(mem, offsets, ops), rounds)),
            new("read 16-bit flat", Harness.Measure(ops, () => Read16(mem, offsets, ops), rounds), "BinaryPrimitives"),
            new("read 32-bit flat", Harness.Measure(ops, () => Read32(mem, offsets, ops), rounds), "BinaryPrimitives"),
            new("read 64-bit flat", Harness.Measure(ops, () => Read64(mem, offsets, ops), rounds), "BinaryPrimitives"),
            new("read 64-bit BitConverter", Harness.Measure(ops, () => Read64BitConverter(mem, offsets, ops), rounds)),
            new("read 64-bit Unsafe", Harness.Measure(ops, () => Read64Unsafe(mem, offsets, ops), rounds), "bez sprawdzeń"),
            new("read 64-bit unaligned", Harness.Measure(ops, () => Read64Unaligned(mem, offsets, ops), rounds), "offset | 1"),
            new("write 32-bit flat", Harness.Measure(ops, () => Write32(mem, offsets, ops), rounds), "BinaryPrimitives"),
            new("read 8-bit banked", Harness.Measure(ops, () => Read8Banked(banks, bankMap, offsets, ops), rounds), "32 banki"),
            new("read 8-bit paged+TLB", Harness.Measure(ops, () => Read8Paged(pages, tlb, offsets, ops), rounds), "512 stron"),
        ];
    }

    private static int[] MakeOffsets()
    {
        var offsets = new int[OffsetCount];
        var rng = new Random(99);

        for (int i = 0; i < offsets.Length; i++)
        {
            offsets[i] = rng.Next(Mask);
        }

        return offsets;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read8(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += mem[offsets[i & (OffsetCount - 1)]];
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read16(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += BinaryPrimitives.ReadInt16LittleEndian(mem.AsSpan(offsets[i & (OffsetCount - 1)]));
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read32(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += BinaryPrimitives.ReadInt32LittleEndian(mem.AsSpan(offsets[i & (OffsetCount - 1)]));
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read64(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += BinaryPrimitives.ReadInt64LittleEndian(mem.AsSpan(offsets[i & (OffsetCount - 1)]));
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read64BitConverter(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += BitConverter.ToInt64(mem, offsets[i & (OffsetCount - 1)]);
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read64Unsafe(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        fixed (byte* p = mem)
        {
            for (int i = 0; i < ops; i++)
            {
                acc += *(long*)(p + offsets[i & (OffsetCount - 1)]);
            }
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read64Unaligned(byte[] mem, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += BinaryPrimitives.ReadInt64LittleEndian(mem.AsSpan((offsets[i & (OffsetCount - 1)] | 1)));
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Write32(byte[] mem, int[] offsets, int ops)
    {
        for (int i = 0; i < ops; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(mem.AsSpan(offsets[i & (OffsetCount - 1)]), i);
        }

        return mem[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read8Banked(byte[][] banks, int[] bankMap, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            int address = offsets[i & (OffsetCount - 1)] & 0xFFFFF;   // 1 MB
            int bank = bankMap[(address >> 15) & 31];
            acc += banks[bank][address & 0x7FFF];
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Read8Paged(byte[][] pages, int[] tlb, int[] offsets, int ops)
    {
        long acc = 0;

        for (int i = 0; i < ops; i++)
        {
            int address = offsets[i & (OffsetCount - 1)] & 0xFFFFF;   // 1 MB
            int page = tlb[(address >> 11) & 511];
            acc += pages[page][address & 0x7FF];
        }

        return acc;
    }
}
