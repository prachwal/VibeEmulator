namespace VibeEmulator.Core;

/// <summary>
/// Implementacje ciągu Fibonacciego o różnych złożonościach.
/// Wszystkie zwracają <c>long</c>, więc poprawne wyniki mieszczą się w zakresie
/// <c>n</c> od 0 do 92; powyżej następuje przepełnienie.
/// </summary>
public static class Fibonacci
{
    /// <summary>Maksymalny argument bez przepełnienia <see cref="long"/>.</summary>
    public const int MaxExactArgument = 92;

    /// <summary>Rekurencja naiwna, O(φⁿ). Punkt odniesienia dla pozostałych wariantów.</summary>
    public static long Recursive(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        return RecursiveCore(n);
    }

    /// <summary>Wersja iteracyjna, O(n), stała pamięć.</summary>
    public static long Iterative(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        long a = 0;
        long b = 1;
        for (int i = 0; i < n; i++)
        {
            long next = a + b;
            a = b;
            b = next;
        }

        return a;
    }

    /// <summary>Memoizacja rekurencji w <see cref="Dictionary{TKey,TValue}"/>, O(n) czasu, O(n) pamięci.</summary>
    public static long Memoized(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        return MemoizedCore(n, new Dictionary<int, long>(n + 1));
    }

    /// <summary>Fast doubling, O(log n) — najmniejsza liczba operacji dla dużych <paramref name="n"/>.</summary>
    public static long FastDoubling(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        return FastDoublingCore(n).Value;
    }

    private static long RecursiveCore(int n) => n < 2 ? n : RecursiveCore(n - 1) + RecursiveCore(n - 2);

    private static long MemoizedCore(int n, Dictionary<int, long> cache)
    {
        if (n < 2)
        {
            return n;
        }

        if (cache.TryGetValue(n, out long cached))
        {
            return cached;
        }

        long value = MemoizedCore(n - 1, cache) + MemoizedCore(n - 2, cache);
        cache[n] = value;
        return value;
    }

    private static (long Value, long Next) FastDoublingCore(int n)
    {
        if (n == 0)
        {
            return (0, 1);
        }

        (long a, long b) = FastDoublingCore(n >> 1);
        long c = a * ((b << 1) - a);
        long d = a * a + b * b;

        return (n & 1) == 0 ? (c, d) : (d, c + d);
    }
}
