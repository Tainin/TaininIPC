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
public abstract class AbstractTable<TInput, TStored> where TInput : notnull where TStored : notnull {

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
    /// Adds the specified <typeparamref name="TInput"/> to the <see cref="AbstractTable{TInput, TStored}"/> with an automatically assigned id.
    /// </summary>
    /// <param name="input">The <typeparamref name="TInput"/> to add to the <see cref="AbstractTable{TInput, TStored}"/>.</param>
    /// <returns>An asyncronous task which completes with the id assigned to the added <typeparamref name="TInput"/>.</returns>
    public Task<int> Add(TInput input) => AddInternal(input, Interlocked.Increment(ref nextAvailableId));
    /// <summary>
    /// Adds the specified <typeparamref name="TInput"/> to the <see cref="AbstractTable{TInput, TStored}"/> 
    /// with the specified id. The id must be within the reserved range.
    /// </summary>
    /// <param name="input">The <typeparamref name="TInput"/> to add to the <see cref="AbstractTable{TInput, TStored}"/>.</param>
    /// <param name="id">The id to map to the added <typeparamref name="TInput"/></param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the specified <paramref name="id"/> is less than 0 or
    /// greater than or equal to <see cref="ReservedCount"/></exception>
    public Task AddReserved(TInput input, int id) {
        // Ensure that the specified id falls within the reserved range.
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), "Ids must not be less than 0.");
        if (id >= firstAvailableId) throw new ArgumentOutOfRangeException(nameof(id), "The provided id is outside the reserved range.");

        // Add the item to the table
        return AddInternal(input, id);
    }
    /// <summary>
    /// Removes all <typeparamref name="TStored"/> in the <see cref="AbstractTable{TInput, TStored}"/>.
    /// </summary>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public async Task Clear() {
        // Aquire semaphore to guarantee exclusive access to the table.
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        table.Clear();
        syncSemaphore.Release();
    }
    /// <summary>
    /// Gets the <typeparamref name="TStored"/> mapped to the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of the <typeparamref name="TStored"/> to get.</param>
    /// <returns>An asyncronous task which completes with the 
    /// <typeparamref name="TStored"/> mapped to the provided <paramref name="id"/></returns>
    public Task<TStored> Get(int id) => Get(KeyUtils.GetKey(id));
    /// <summary>
    /// Gets the <typeparamref name="TStored"/> mapped to the provided <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the <typeparamref name="TStored"/> to get.</param>
    /// <returns>An asyncronous task which completes with the 
    /// <typeparamref name="TStored"/> mapped to the provided <paramref name="key"/></returns>
    public async Task<TStored> Get(ReadOnlyMemory<byte> key) {
        // Call the helper method to get the item
        (TStored? stored, bool got) = await GetInternal(key).ConfigureAwait(false);
        // If the item was gotten return it.
        if (got && stored is not null) return stored;
        // Otherwise throw exception
        throw new ArgumentException("The provided key was not found in the table.");
    }
    /// <summary>
    /// Removes the <typeparamref name="TStored"/> mapped to the provided <paramref name="id"/>
    /// </summary>
    /// <param name="id">The id of the <typeparamref name="TStored"/> to get.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task Remove(int id) => Remove(KeyUtils.GetKey(id));
    /// <summary>
    /// Removes the <typeparamref name="TStored"/> mapped to the provided <paramref name="key"/>
    /// </summary>
    /// <param name="key">The key of the <typeparamref name="TStored"/> to get.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public async Task Remove(ReadOnlyMemory<byte> key) {
        // Aquire semaphore to guarantee exclusive access to the table.
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool removed = table.TryRemove(key.Span);
        syncSemaphore.Release();

        if (removed) return;

        throw new ArgumentException("The provided key was not found in the table.");
    }

    /// <summary>
    /// An abstract helper method that should be overriden to provide the transformation between 
    /// <typeparamref name="TInput"/> and <typeparamref name="TStored"/> 
    /// and then call <see cref="AddInternalBase(TStored, int)"/> with the result of the transformation.
    /// </summary>
    /// <param name="input">The <typeparamref name="TInput"/> to add to the <see cref="AbstractTable{TInput, TStored}"/>.</param>
    /// <param name="id">The id to map to the added <typeparamref name="TInput"/></param>
    /// <returns>An asyncronous task which completes with the id assigned to the added <typeparamref name="TInput"/>.</returns>
    protected abstract Task<int> AddInternal(TInput input, int id);
    /// <summary>
    /// A helper method which adds the specified <typeparamref name="TStored"/> to the <see cref="AbstractTable{TInput, TStored}"/>
    /// with the specified id.
    /// </summary>
    /// <param name="stored">The <typeparamref name="TStored"/> to add to the <see cref="AbstractTable{TInput, TStored}"/>.</param>
    /// <param name="id">The id to map the added <typeparamref name="TStored"/> to.</param>
    /// <returns>An asyncronous task which completes with the id assigned to the added <typeparamref name="TInput"/>.</returns>
    /// <exception cref="ArgumentException"></exception>
    protected async Task<int> AddInternalBase(TStored stored, int id) {
        // Aquire semaphore to guarantee exclusive access to the table.
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool added = table.TryAdd(KeyUtils.GetKey(id), stored);
        syncSemaphore.Release();

        // If the it was added return it's id
        if (added && stored is not null) return id;
        // Otherwise throw exception
        throw new ArgumentException("The provided id already exists in the table.");
    }
    /// <summary>
    /// Helper method to get a <typeparamref name="TStored"/> from the 
    /// <see cref="AbstractTable{TInput, TStored}"/> by it's <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the <typeparamref name="TStored"/> to get.</param>
    /// <returns>An asyncronous task which completes with a two tuple containing the <typeparamref name="TStored"/> 
    /// mapped to the provided <paramref name="key"/> if the key existed in the <see cref="AbstractTable{TInput, TStored}"/>,
    /// otherwise the <see langword="default"/> of <typeparamref name="TStored"/> and a flag indicating whether it existed.</returns>
    protected async Task<(TStored? stored, bool got)> GetInternal(ReadOnlyMemory<byte> key) {
        // Aquire semaphore to guarantee exclusive access to the table.
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool got = table.TryGet(key.Span, out TStored? stored);
        syncSemaphore.Release();
        return (got ? stored : default, got);
    }
}
