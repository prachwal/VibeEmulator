namespace VibeEmulator.Core.Tests;

public class FibonacciTests
{
    [Theory]
    [InlineData(0, 0L)]
    [InlineData(1, 1L)]
    [InlineData(2, 1L)]
    [InlineData(10, 55L)]
    [InlineData(20, 6765L)]
    [InlineData(25, 75025L)]
    public void Recursive_ReturnsExpectedValue(int n, long expected)
    {
        // Rekurencja naiwna ma złożoność O(φⁿ) — dla n = 92 to ~3×10¹⁹ wywołań,
        // czyli zawieszenie testu. Duże n sprawdzają tylko warianty liniowe/logarytmiczne.
        Assert.Equal(expected, Fibonacci.Recursive(n));
    }

    [Theory]
    [InlineData(0, 0L)]
    [InlineData(1, 1L)]
    [InlineData(2, 1L)]
    [InlineData(10, 55L)]
    [InlineData(20, 6765L)]
    [InlineData(50, 12586269025L)]
    [InlineData(92, 7540113804746346429L)]
    public void EfficientImplementations_ReturnExpectedValue(int n, long expected)
    {
        Assert.Equal(expected, Fibonacci.Iterative(n));
        Assert.Equal(expected, Fibonacci.Memoized(n));
        Assert.Equal(expected, Fibonacci.FastDoubling(n));
    }

    [Fact]
    public void NegativeArgument_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fibonacci.Recursive(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fibonacci.Iterative(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fibonacci.Memoized(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fibonacci.FastDoubling(-1));
    }

    [Fact]
    public void MaxExactArgument_DoesNotOverflow()
    {
        long value = Fibonacci.FastDoubling(Fibonacci.MaxExactArgument);

        Assert.True(value > 0);
        Assert.Equal(value, Fibonacci.Iterative(Fibonacci.MaxExactArgument));
    }
}
