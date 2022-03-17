using TaininIPC.Client.Interface;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

/// <summary>
/// An abstract base class which represents a associative array like table of objects mapped to integer ids.
/// </summary>
/// <typeparam name="TInput">The type of the objects passed to the add methods. Implementing classes should 
/// provide a transformation from <typeparamref name="TInput"/> to 
/// <typeparamref name="TStored"/> in <see cref="AddInternal(TInput, int)"/></typeparam>
/// <typeparam name="TStored">The type of the objects stored in the table.</typeparam>
public abstract class AbstractTable<TInput, TStored> : ITable<TInput, TStored> where TInput : notnull where TStored : notnull {

    // Internal mapping
    private readonly CritBitTree<TStored> table;
    // Provides synchronization for reads and writes of the table
    private readonly SemaphoreSlim syncSemaphore;

    // First non reserved id
    private readonly int firstAvailableId;
    // Next id available to be assigned
    private int nextAvailableId;

    /// <summary>
    /// Gets the number of reserved ids in the <see cref="AbstractTable{TInput, TStored}"/>. Reserved ids start at 0 and run through <see cref="ReservedCount"/> - 1.
    /// </summary>
    public int ReservedCount => firstAvailableId;

    /// <summary>
    /// Initializes a new <see cref="AbstractTable{TInput, TStored}"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids in the <see cref="AbstractTable{TInput, TStored}"/>.
    /// All ids from 0 to <paramref name="reservedCount"/> - 1 will be reserved.</param>
    public AbstractTable(int reservedCount) {
        // Initialize the internal table representation and the synchronization primative
        (table, syncSemaphore) = (new(), new(1, 1));

        // Setup id source based on the reservedCount
        firstAvailableId = reservedCount;
        nextAvailableId = firstAvailableId;
    }

    /// <summary>
    /// Adds <paramref name="input"/> to the table with an automatically assigned id.
    /// </summary>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task which completes with the id assigned to <paramref name="input"/>.</returns>
    public Task<int> Add(TInput input) => AddInternal(input, Interlocked.Increment(ref nextAvailableId));
    /// <summary>
    /// Adds <paramref name="input"/> to the table with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="input">The entry to add to the table.</param>
    /// <param name="id">The id to map to <paramref name="input"/>.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than <c>0</c> or greater 
    /// or equal to <see cref="ReservedCount"/></exception>
    public Task AddReserved(TInput input, int id) {
        // Ensure that the specified id falls within the reserved range.
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), "Ids must not be less than 0.");
        if (id >= firstAvailableId) throw new ArgumentOutOfRangeException(nameof(id), "The provided id is outside the reserved range.");

        // Add the item to the table
        return AddInternal(input, id);
    }

    /// <summary>
    /// Removes all entries from the table.
    /// </summary>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public async Task Clear() {
        // Aquire semaphore to guarantee exclusive access to the table.
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        table.Clear();
        syncSemaphore.Release();
    }

    /// <summary>
    /// Attempts to get the entry mapped to by the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of the entry to get.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> which represents the entry mapped to by the given
    /// <paramref name="id"/> given that it exists.</returns>
    public Task<Attempt<TStored>> TryGet(int id) => TryGet(KeyUtils.GetKey(id));
    /// <summary>
    /// Attempts to get the entry mapped to by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the entry to get.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> which represents the entry mapped to by the given
    /// <paramref name="key"/> given that it exists.</returns>
    public async Task<Attempt<TStored>> TryGet(ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return table.TryGet(key.Span, out TStored? stored).ToAttempt(stored);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Determines if the given <paramref name="id"/> is in the table.
    /// </summary>
    /// <param name="id">The id to check for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="id"/> is in the table,
    /// <see langword="false"/> otherwise.</returns>
    public Task<bool> Contains(int id) => Contains(KeyUtils.GetKey(id));
    /// <summary>
    /// Determines if the given <paramref name="key"/> is in the table.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="key"/> is in the table,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> Contains(ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return table.ContainsKey(key.Span);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Attempts to remove the entry mapped to by the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of the entry to remove.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="id"/> is in the table,
    /// <see langword="false"/> otherwise.</returns>
    public Task<bool> TryRemove(int id) => TryRemove(KeyUtils.GetKey(id));
    /// <summary>
    /// Attempts to remove the entry mapped to by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="key"/> is in the table,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> TryRemove(ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return table.TryRemove(key.Span);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <summary>
    /// An abstract helper method that should be overriden to provide the transformation between 
    /// <typeparamref name="TInput"/> and <typeparamref name="TStored"/> 
    /// and then call <see cref="AddInternalBase(TStored, int)"/> with the result of the transformation.
    /// </summary>
    /// <param name="input">The entry to add to the table.</param>
    /// <param name="id">The id to map to <paramref name="stored"/>.</param>
    /// <returns>An asyncronous task which completes with the id assigned to <paramref name="input"/>.</returns>
    protected abstract Task<int> AddInternal(TInput input, int id);
    /// <summary>
    /// A helper method which adds <paramref name="stored"/> to the table with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="stored">The entry to add to the table.</param>
    /// <param name="id">The id to map to <paramref name="stored"/>.</param>
    /// <returns>An asyncronous task which completes with the id assigned to <paramref name="stored"/>.</returns>
    /// <exception cref="ArgumentException">If <paramref name="id"/> is already present in the table.</exception>
    protected async Task<int> AddInternalBase(TStored stored, int id) {
        // Aquire semaphore to guarantee exclusive access to the table.
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool added = table.TryAdd(KeyUtils.GetKey(id), stored);
        syncSemaphore.Release();

        // If the entry could not be added to the table the id must already be in use
        if (!added) throw new ArgumentException("The provided id already exists in the table.");
        else return id;
    }
}
