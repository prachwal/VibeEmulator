namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static int GetMovImmediateWriteStart(int mod, int rm, int queueCount, bool prefixed)
    {
        if (!prefixed)
        {
            return mod switch
            {
                0 when rm is 0 or 3 => queueCount == 5 ? 16 : 15,
                0 when rm is 1 or 2 => queueCount == 5 ? 17 : 16,
                0 when rm == 6 => queueCount == 5 ? 15 : 14,
                0 => queueCount == 5 ? 14 : 13,
                1 when rm is 0 or 3 => queueCount == 5 ? 19 : 20,
                1 when rm is 1 or 2 => queueCount == 5 ? 20 : 21,
                1 => queueCount == 5 ? 17 : 18,
                2 when rm is 0 or 3 => queueCount == 5 ? 19 : 19,
                2 when rm is 1 or 2 => queueCount == 5 ? 20 : 20,
                2 when rm >= 4 => queueCount == 5 ? 18 : 17,
                _ => throw new ArgumentOutOfRangeException(nameof(mod)),
            };
        }

        return mod switch
        {
            0 when rm is 0 or 3 => queueCount == 5 ? 17 : 18,
            0 when rm is 1 or 2 => queueCount == 5 ? 18 : 19,
            0 when rm == 6 => 17,
            0 => queueCount == 5 ? 15 : 16,
            1 when rm is 0 or 3 => queueCount == 5 ? 22 : 21,
            1 when rm is 1 or 2 => queueCount == 5 ? 23 : 22,
            1 => queueCount == 5 ? 20 : 19,
            2 when rm is 0 or 3 => queueCount == 5 ? 21 : 21,
            2 when rm is 1 or 2 => queueCount == 5 ? 22 : 22,
            2 when rm >= 4 => queueCount == 5 ? 19 : 20,
            _ => throw new ArgumentOutOfRangeException(nameof(mod)),
        };
    }

    private static int GetMovImmediatePrefetchBytes(int queueCount, int mod, int rm, int prefix, int displacementBytes)
    {
        int finalCount = prefix == 1
            ? queueCount == 5 ? mod == 1 ? 5 : 4 : mod == 1 ? 4 : mod == 2 && rm < 4 ? 3 : 5
            : queueCount == 5 ? mod == 1 ? 4 : mod == 2 && rm < 4 ? 3 : 5 : mod == 1 ? 5 : mod == 0 ? 4 : 4;
        int instructionLength = prefix + 3 + displacementBytes;
        return Math.Max(0, finalCount - (queueCount - instructionLength));
    }

    private static (int First, int Second, int Third) GetMovImmediateFetchStarts(int mod, int rm, int queueCount, bool prefixed)
    {
        if (prefixed)
        {
            if (queueCount == 5)
            {
                int third = mod == 0 && rm == 6 ? 11
                    : mod == 1 ? rm is 0 or 3 ? 18 : rm is 1 or 2 ? 19 : 16
                        : mod == 2 ? rm >= 4 ? 11 : 14 : 0;
                return (3, 7, third);
            }

            int second = mod == 0 ? rm == 6 ? 9 : rm is 0 or 3 ? 14 : rm is 1 or 2 ? 15 : 12
                : mod == 1 ? rm is 0 or 3 ? 12 : rm is 1 or 2 ? 13 : 12
                    : rm is 0 or 3 ? 12 : rm is 1 or 2 ? 13 : 12;
            int thirdFetch = mod == 0 && rm == 6 ? 13 : mod == 2 && rm >= 4 ? 16 : 0;
            return (5, second, thirdFetch);
        }

        if (queueCount == 5)
        {
            if (mod == 0 && rm == 6)
            {
                return (3, 7, 11);
            }

            int second = mod == 0 ? rm is 0 or 3 ? 12 : rm is 1 or 2 ? 13 : 10
                : mod == 1 ? rm is 1 or 2 ? 11 : 10
                    : rm is 1 or 2 ? 11 : 10;
            int thirdFetch = mod == 2 && rm >= 4 ? 14 : 0;
            return (3, second, thirdFetch);
        }

        int secondFetch = mod == 0 ? rm == 6 ? 8 : 0
            : mod == 1 ? rm is 0 or 3 ? 16 : rm is 1 or 2 ? 17 : 14
                : mod == 2 ? rm is 1 or 2 ? 12 : 11 : 0;
        return (4, secondFetch, 0);
    }
}
