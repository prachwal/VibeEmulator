namespace VibeEmulator.Memory;

/// <summary>Magistrala pamięci z fizycznym adresem szerszym niż 16 bitów.</summary>
public interface IAddressBus
{
    byte Read(uint address);

    void Write(uint address, byte value);
}
