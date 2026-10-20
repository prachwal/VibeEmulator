using System.Runtime.CompilerServices;

namespace EmuBench;

/// <summary>
/// Bloki podstawowe: interpreter instrukcja-po-instrukcji vs wykonanie
/// wcześniej rozwiązanej sekwencji (odpowiednik prostego block cache / dynarec).
/// </summary>
internal static class Blocks
{
    private const int BlockLength = 64;

    private static readonly byte[] Block = MakeBlock();

    public static IReadOnlyList<Harness.Result> Run(int ops, int rounds)
    {
        long blocks = Math.Max(1, ops / BlockLength);

        return
        [
            new("interpret block (switch)", Harness.Measure((int)blocks * BlockLength, () => Interpret(Block, blocks), rounds), $"{BlockLength} instr."),
            new("precompiled block", Harness.Measure((int)blocks * BlockLength, () => StraightLine(blocks), rounds), "straight-line"),
        ];
    }

    private static byte[] MakeBlock()
    {
        var block = new byte[BlockLength];
        var rng = new Random(11);

        for (int i = 0; i < block.Length; i++)
        {
            block[i] = (byte)rng.Next(16);
        }

        return block;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Interpret(byte[] block, long blocks)
    {
        int acc = 0;

        for (long b = 0; b < blocks; b++)
        {
            for (int i = 0; i < BlockLength; i++)
            {
                switch (block[i])
                {
                    case 0: acc += 1; break;
                    case 1: acc -= 2; break;
                    case 2: acc ^= 3; break;
                    case 3: acc += acc >> 1; break;
                    case 4: acc *= 3; break;
                    case 5: acc -= 1; break;
                    case 6: acc ^= acc << 2; break;
                    case 7: acc += 7; break;
                    case 8: acc >>= 1; break;
                    case 9: acc -= 3; break;
                    case 10: acc |= 1; break;
                    case 11: acc &= 0x7FFF; break;
                    case 12: acc += acc; break;
                    case 13: acc -= acc >> 2; break;
                    case 14: acc ^= 5; break;
                    default: acc += 15; break;
                }
            }
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StraightLine(long blocks)
    {
        int acc = 0;

        for (long b = 0; b < blocks; b++)
        {
            // Ten sam efekt co Interpret, ale bez dispatchu — kod wynikowy rekompilatora.
            acc = (((((((((((((((acc * 3) & 0x7FFF) ^ 7) >> 1) + 15) ^ 5) | 1) & 0x7FFF) * 3) + 3) ^ 1) - 1) + 7) * 3) & 0x7FFF);
            acc = (((((((((((((((acc * 3) & 0x7FFF) ^ 7) >> 1) + 15) ^ 5) | 1) & 0x7FFF) * 3) + 3) ^ 1) - 1) + 7) * 3) & 0x7FFF);
            acc = (((((((((((((((acc * 3) & 0x7FFF) ^ 7) >> 1) + 15) ^ 5) | 1) & 0x7FFF) * 3) + 3) ^ 1) - 1) + 7) * 3) & 0x7FFF);
            acc = (((((((((((((((acc * 3) & 0x7FFF) ^ 7) >> 1) + 15) ^ 5) | 1) & 0x7FFF) * 3) + 3) ^ 1) - 1) + 7) * 3) & 0x7FFF);
        }

        return acc;
    }
}
