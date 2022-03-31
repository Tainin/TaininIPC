using System.Diagnostics;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Utils;

namespace TaininIPC.Client.Table;

/// <summary>
/// Represents a thread-safe mapping from <see cref="string"/> names and <see cref="Int32Key"/> keys to <typeparamref name="T"/> elements.
/// </summary>
/// <typeparam name="T">The type of elements to store in the table.</typeparam>
public class Table<T> where T : notnull {
    private class AddHandle : ITableAddHandle<T> {
        public Int32Key Key { get; }

        private readonly Table<T> container;

        public AddHandle(Table<T> table, Int32Key key) => (container, Key) = (table, key);

        public async Task Add(Func<Int32Key, T> entryFactory) {
            await container.syncSemaphore.WaitAsync().ConfigureAwait(false);
            bool added = container.table.TryAdd(Key, entryFactory(Key));
            Debug.Assert(added);
            container.syncSemaphore.Release();
        }
    }

    private readonly CritBitTree<Int32Key, T> table;
    private readonly SemaphoreSlim syncSemaphore;

    private int nextAvailableId;
    private readonly int[] reservations;

    /// <summary>
    /// The number of ids reserved for manual assignment
    /// </summary>
    /// <remarks>
    /// Reserved ids start at <c>0</c> and end at <see cref="ReservedCount"/><c> - 1</c>.
    /// </remarks>
    public int ReservedCount { get; }

    /// <summary>
    /// Initializes a new <see cref="Table{TStored}"/> given the number of ids to reserve.
    /// </summary>
    /// <param name="reservedCount">The number of ids to reserve.</param>
    public Table(int reservedCount) {
        (table, syncSemaphore) = (new(), new(1, 1));

        ReservedCount = reservedCount;
        nextAvailableId = reservedCount;
        reservations = new int[reservedCount];
    }

    /// <summary>
    /// Gets an <see cref="ITableAddHandle{TStored}"/> with an automatically assigned key.
    /// </summary>
    /// <returns>The add handle.</returns>
    /// <exception cref="OverflowException">If incrementing the available id index causes an overflow.</exception>
    public ITableAddHandle<T> GetAddHandle() {
        int assignedId = Interlocked.Increment(ref nextAvailableId);
        if (assignedId < 0) throw new OverflowException("Overflow occured while attempting to get the next available id.");
        return new AddHandle(this, new(assignedId));
    }
    /// <summary>
    /// Gets an <see cref="ITableAddHandle{TStored}"/> with the specified key.
    /// </summary>
    /// <param name="reservedKey">The key to assign to the add handle.</param>
    /// <returns>The add handle.</returns>
    /// <exception cref="InvalidOperationException">If the <paramref name="reservedKey"/>'s reservation has already been claimed.</exception>
    public ITableAddHandle<T> GetAddHandle(Int32Key reservedKey) {
        int reservation = Interlocked.CompareExchange(ref reservations[reservedKey.Id], 1, 0);
        if (reservation != 0) throw new InvalidOperationException($"The specified {nameof(reservedKey)} is already in use.");
        return new AddHandle(this, reservedKey);
    }
    /// <summary>
    /// Attempts to get the entry mapped to by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key which maps to the entry to get.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> which represents
    /// the entry mapped to by the given <paramref name="key"/> if it is present in the table.</returns>
    public async Task<Attempt<T>> TryGet(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool hasResult = table.TryGet(key, out T? result);
        syncSemaphore.Release();
        return hasResult.ToAttempt(result);
    }
    /// <summary>
    /// Determines if the given <paramref name="key"/> exists in the table.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the given <paramref name="key"/> exists in the table,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> Contains(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool contains = table.ContainsKey(key);
        syncSemaphore.Release();
        return contains;
    }
    /// <summary>
    /// Attempts to remove the entry mapped to by the given <paramref name="key"/> from the table.
    /// </summary>
    /// <param name="key">The key to remove from the table.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the given <paramref name="key"/> existed in the table
    /// and was removed, <see langword="false"/> otherwise.</returns>
    public async Task<bool> TryRemove(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool removed = table.TryRemove(key);
        if (removed && key.Id < reservations.Length) // Release reservation claim if necessary
            Interlocked.Exchange(ref reservations[key.Id], 0);
        syncSemaphore.Release();
        return removed;
    }
    /// <summary>
    /// Clears all entries from the table.
    /// </summary>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        table.Clear();
        syncSemaphore.Release();
    }
}