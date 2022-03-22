using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

/// <summary>
/// An <see langword="abstract"/> base for implementations of <see cref="ITable{TInput, TStored}"/>
/// </summary>
/// <typeparam name="TInput">The type of objects passed to the <c>Add</c> methods.</typeparam>
/// <typeparam name="TStored">The type of the entries stored in the table.</typeparam>
/// <remarks><typeparamref name="TInput"/> and <typeparamref name="TStored"/> are separate so that implementations can
/// control the initialization of <typeparamref name="TStored"/> instances. For instance instanciating <typeparamref name="TStored"/> from 
/// an <c>Options</c> type (<typeparamref name="TInput"/>). If this behavior is not needed, implementations can simply use the same type for
/// both type parameters and provide a no-op transformation.</remarks>
public abstract class AbstractTable<TInput, TStored> : ITable<TInput, TStored> where TInput : notnull where TStored : notnull {

    // Internal mapping
    private readonly CritBitTree<Int32Key, TStored> table;
    // Provides synchronization for reads and writes of the table
    private readonly SemaphoreSlim syncSemaphore;

    // First non reserved id
    private readonly int firstAvailableId;
    // Next id available to be assigned
    private int nextAvailableId;

    /// <inheritdoc cref="ITable{TInput, TStored}.ReservedCount"/>
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

    /// <inheritdoc cref="ITable{TInput, TStored}.Add(TInput)"/>
    public async Task<Int32Key> Add(TInput input) {
        Int32Key key = new(Interlocked.Increment(ref nextAvailableId));
        await AddInternal(key, input).ConfigureAwait(false);
        return key;
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.AddReserved(Int32Key, TInput)"/>
    public Task AddReserved(Int32Key key, TInput input) => AddInternal(key, input);
    /// <inheritdoc cref="ITable{TInput, TStored}.Clear"/>
    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            table.Clear();
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.TryGet(Int32Key)"/>
    public async Task<Attempt<TStored>> TryGet(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return table.TryGet(key, out TStored? stored).ToAttempt(stored);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.Contains(Int32Key)"/>
    public async Task<bool> Contains(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return table.ContainsKey(key);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.TryRemove(Int32Key)"/>
    public async Task<bool> TryRemove(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return table.TryRemove(key);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <summary>
    /// An abstract helper method that should be overriden to provide the transformation between <typeparamref name="TInput"/>
    /// and <typeparamref name="TStored"/> and then call <see cref="AddInternalBase(Int32Key, TStored)"/> with the result of the transformation.
    /// </summary>
    /// <param name="key">The key to map to the specified <paramref name="input"/></param>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    protected abstract Task AddInternal(Int32Key key, TInput input);
    /// <summary>
    /// A helper method which adds the specified <paramref name="stored"/> to the table with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to map to the specified <paramref name="stored"/></param>
    /// <param name="stored">The entry to add to the table.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    /// <exception cref="ArgumentException">If the specified <paramref name="key"/> already exists in the table.</exception>
    protected async Task AddInternalBase(Int32Key key, TStored stored) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (!table.TryAdd(key, stored)) throw new ArgumentException("The provided key already exists in the table.");
        } finally {
            syncSemaphore.Release();
        }
    }
}
