namespace VibeEmulator.Memory;

/// <summary>
/// Składa wielu obserwatorów magistrali za jednym slotem obserwatora (np. <c>C64Bus.Observer</c>).
/// </summary>
/// <remarks>
/// Obserwatorzy są trzymani w tablicy podmienianej w całości przy <see cref="Add"/> i <see cref="Remove"/>
/// (copy-on-write). <see cref="OnAccess"/> bierze jedną referencję do tablicy i iteruje po niej —
/// nie alokuje i nie jest psuty przez modyfikację listy w trakcie wywołań.
/// </remarks>
public sealed class CompositeBusObserver : IBusObserver
{
    private static readonly IBusObserver[] NoObservers = [];

    private readonly object _gate = new();
    private IBusObserver[] _observers = NoObservers;

    /// <summary>Liczba zarejestrowanych obserwatorów.</summary>
    public int Count => Volatile.Read(ref _observers).Length;

    /// <summary>
    /// Dodaje obserwatora na koniec kolejki wywołań. Ponowne dodanie tego samego obserwatora jest ignorowane.
    /// </summary>
    public void Add(IBusObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_gate)
        {
            IBusObserver[] current = _observers;

            if (Array.IndexOf(current, observer) >= 0)
            {
                return;
            }

            var next = new IBusObserver[current.Length + 1];
            current.CopyTo(next, 0);
            next[^1] = observer;
            Volatile.Write(ref _observers, next);
        }
    }

    /// <summary>Usuwa obserwatora. Zwraca <c>false</c>, gdy go nie było.</summary>
    public bool Remove(IBusObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_gate)
        {
            IBusObserver[] current = _observers;
            int index = Array.IndexOf(current, observer);

            if (index < 0)
            {
                return false;
            }

            if (current.Length == 1)
            {
                Volatile.Write(ref _observers, NoObservers);
                return true;
            }

            var next = new IBusObserver[current.Length - 1];
            current.AsSpan(0, index).CopyTo(next);
            current.AsSpan(index + 1).CopyTo(next.AsSpan(index));
            Volatile.Write(ref _observers, next);
            return true;
        }
    }

    /// <summary>Wywołuje wszystkich obserwatorów w kolejności dodania.</summary>
    public void OnAccess(bool write, ushort address, byte value)
    {
        IBusObserver[] snapshot = Volatile.Read(ref _observers);

        for (int i = 0; i < snapshot.Length; i++)
        {
            snapshot[i].OnAccess(write, address, value);
        }
    }
}
