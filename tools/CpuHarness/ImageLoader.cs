using VibeEmulator.Memory;

namespace VibeEmulator.CpuHarness;

/// <summary>
/// Wczytanie obrazu pamięci do <see cref="FlatMemory"/>. Obrazy testów Dormanna mają 64 KB i są ładowane
/// od <c>$0000</c>; krótsze obrazy (np. syntetyczne w testach) ładuje się pod adres <c>origin</c>.
/// </summary>
public static class ImageLoader
{
    /// <summary>Maksymalny rozmiar obrazu — tyle ma płaska pamięć rdzenia.</summary>
    public const int MaxSize = 0x10000;

    /// <summary>Ładuje bajty obrazu pod <paramref name="origin"/>; reszta pamięci zostaje wyzerowana.</summary>
    /// <exception cref="ArgumentException">Obraz pusty albo większy niż 64 KB.</exception>
    public static FlatMemory Load(ReadOnlySpan<byte> image, ushort origin = 0)
    {
        if (image.IsEmpty)
        {
            throw new ArgumentException("Obraz jest pusty.", nameof(image));
        }

        if (image.Length > MaxSize)
        {
            throw new ArgumentException($"Obraz ma {image.Length} B; maksimum to {MaxSize} B.", nameof(image));
        }

        if (origin + image.Length > MaxSize)
        {
            throw new ArgumentException(
                $"Obraz ({image.Length} B) nie mieści się w pamięci od ${origin:X4}.", nameof(origin));
        }

        var memory = new FlatMemory();
        memory.Load(origin, image);
        return memory;
    }
}
