using System.Buffers.Binary;
using System.Text;
using TaininIPC.Client.Interface;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

/// <summary>
/// Wraps a subtype of <see cref="AbstractTable{TInput, TStored}"/> and adds an additional level of mapping from <see langword="string"/> 
/// names to the ids of the sub table.
/// </summary>
/// <typeparam name="TableType">The type of the sub table. Must extend <see cref="AbstractTable{TInput, TStored}"/>.</typeparam>
/// <typeparam name="TInput">The <typeparamref name="TInput"/> of the sub sub table - <see cref="AbstractTable{TInput, TStored}"/>.</typeparam>
/// <typeparam name="TStored">The <typeparamref name="TStored"/> of the sub table - <see cref="AbstractTable{TInput, TStored}"/>.</typeparam>
public abstract class AbstractNameMappedTable<TableType, TInput, TStored> : ITable<TInput,TStored> 
    where TableType : AbstractTable<TInput, TStored> where TInput : notnull where TStored : notnull {

    // string name to int id mapping
    private readonly CritBitTree<int> forward;
    // int id to string name mapping
    private readonly CritBitTree<string> reverse;

    // Provides synchronization for reads and writes of the table
    private readonly SemaphoreSlim syncSemaphore;

    // The sub table.
    protected readonly TableType internalTable;

    /// <summary>
    /// Initializes an instance of <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> given an instance of the 
    /// sub table type (<typeparamref name="TableType"/>)
    /// </summary>
    /// <param name="internalTable">The sub table to wrap the <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> around.</param>
    public AbstractNameMappedTable(TableType internalTable) {
        (forward, reverse, syncSemaphore) = (new(), new(), new(1, 1));

        this.internalTable = internalTable;
    }

    /// <summary>
    /// Adds a new entry to the <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> with an automatically assigned id
    /// and the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name to map to the added entry.</param>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task which completes with the id assigned to the added entry.</returns>
    public async Task<int> Add(string name, TInput input) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            int id = await internalTable.Add(input).ConfigureAwait(false);
            await SetNameInternal(name, id, ReadOnlyMemory<byte>.Empty).ConfigureAwait(false);
            return id;
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <summary>
    /// Adds a new unnamed entry to the <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> with an automatically assigned id.
    /// </summary>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task which completes with the id assigned to the added entry.</returns>
    public Task<int> Add(TInput input) => syncSemaphore.AquireAndRun(internalTable.Add, input);

    /// <summary>
    /// Adds a new entry to the <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> with the specified <paramref name="name"/>
    /// and <paramref name="id"/>.
    /// </summary>
    /// <param name="name">The name to map to the added entry.</param>
    /// <param name="input">The entry to add to the table.</param>
    /// <param name="id">The id to map to the added entry.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public async Task AddReserved(string name, TInput input, int id) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            await internalTable.AddReserved(input, id).ConfigureAwait(false);
            await SetNameInternal(name, id, ReadOnlyMemory<byte>.Empty).ConfigureAwait(false);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <summary>
    /// Adds a new unnamed entry to the <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> with 
    /// the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="input">The entry to add to the table.</param>
    /// <param name="id">The id to map to the added entry.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task AddReserved(TInput input, int id) => syncSemaphore.AquireAndRun(internalTable.AddReserved, input, id);

    /// <summary>
    /// Clears all entries from the table.
    /// </summary>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        await internalTable.Clear().ConfigureAwait(false);
        forward.Clear();
        reverse.Clear();
        syncSemaphore.Release();
    }

    /// <summary>
    /// Sets the <paramref name="name"/> which maps to a given <paramref name="id"/>.
    /// </summary>
    /// <param name="name">The name to map to the <paramref name="id"/>.</param>
    /// <param name="id">The id which the <paramref name="name"/> should map to.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task SetName(string name, int id) => syncSemaphore.AquireAndRun(SetNameInternal, name, id, ReadOnlyMemory<byte>.Empty);
    /// <summary>
    /// Sets the <paramref name="name"/> which maps to a given <paramref name="key"/>.
    /// </summary>
    /// <param name="name">The name to map to the <paramref name="key"/>.</param>
    /// <param name="key">The id which the <paramref name="name"/> should map to.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task SetName(string name, ReadOnlyMemory<byte> key) => syncSemaphore.AquireAndRun(SetNameInternal, name, 0, key);

    /// <summary>
    /// Gets an entry by it's <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name mapped to the entry to get.</param>
    /// <returns>An asyncronous task which completes with the entry which the specified <paramref name="name"/> maps to.</returns>
    /// <exception cref="InvalidOperationException">If the name does not exist in the 
    /// <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/></exception>
    public async Task<TStored> Get(string name) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            ReadOnlyMemory<byte> nameKey = Encoding.UTF8.GetBytes(name);
            // Attempts to get the id of the entry to get
            if (!forward.TryGet(nameKey.Span, out int id))
                throw new InvalidOperationException("The specified name does not exist in the table.");
            return await internalTable.Get(id).ConfigureAwait(false);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <summary>
    /// Gets an entry by it's <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id which maps to the entry to get.</param>
    /// <returns>An asyncronous task which completes with the entry which the specified <paramref name="id"/> maps to.</returns>
    public Task<TStored> Get(int id) => syncSemaphore.AquireAndRun(internalTable.Get, id);
    /// <summary>
    /// Gets an entry by it's <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key which maps to the entry to get.</param>
    /// <returns>An asyncronous task which completes with the entry which the specified <paramref name="key"/> maps to.</returns>
    public Task<TStored> Get(ReadOnlyMemory<byte> key) => syncSemaphore.AquireAndRun(internalTable.Get, key);

    /// <summary>
    /// Checks if the given <paramref name="name"/> exists in the table.
    /// </summary>
    /// <param name="name">The name to check for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="name"/> was found
    /// and <see langword="false"/> otherwise.</returns>
    public async Task<bool> Contains(string name) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            ReadOnlyMemory<byte> nameKey = Encoding.UTF8.GetBytes(name);
            if (!forward.TryGet(nameKey.Span, out int id)) return false;
            else return await internalTable.Contains(id).ConfigureAwait(false);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <summary>
    /// Checks if the given <paramref name="id"/> exists in the table.
    /// </summary>
    /// <param name="id">The id to check for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="id"/> was found
    /// and <see langword="false"/> otherwise.</returns>
    public Task<bool> Contains(int id) => syncSemaphore.AquireAndRun(internalTable.Contains, id);
    /// <summary>
    /// Checks if the given <paramref name="key"/> exists in the table.
    /// </summary>
    /// <param name="key">The id to check for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="key"/> was found
    /// and <see langword="false"/> otherwise.</returns>
    public Task<bool> Contains(ReadOnlyMemory<byte> key) => syncSemaphore.AquireAndRun(internalTable.Contains, key);

    /// <summary>
    /// Removes the entry which the given <paramref name="name"/> maps to from the table.
    /// </summary>
    /// <param name="name">The name of the entry to remove.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task Remove(string name) => RemoveInternal(name, 0, ReadOnlyMemory<byte>.Empty);
    /// <summary>
    /// Removes the entry which the given <paramref name="id"/> maps to from the table.
    /// </summary>
    /// <param name="id">The id of the entry to remove.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task Remove(int id) => RemoveInternal(string.Empty, id, ReadOnlyMemory<byte>.Empty);
    /// <summary>
    /// Removes the entry which the given <paramref name="key"/> maps to from the table.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    public Task Remove(ReadOnlyMemory<byte> key) => RemoveInternal(string.Empty, 0, key);

    /// <summary>
    /// Helper method which sets the name mapping of an entry with the given <paramref name="id"/> or <paramref name="key"/>.
    /// </summary>
    /// <param name="name">The name to set to the entry.</param>
    /// <param name="id">The id of the entry to name.</param>
    /// <param name="key">The key of the entry to name.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    /// <remarks>
    /// Only one of <paramref name="id"/> or <paramref name="key"/> must be specified per call.
    /// If <paramref name="key"/> is empty, <paramref name="id"/> will be calculated from it.
    /// Otherwise <paramref name="id"/> will be calculated from <paramref name="key"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">If any of the following occur;
    /// 1: <paramref name="name"/> is null or empty.
    /// 2: <paramref name="name"/> contains one or more characters which are not legal for a name.
    /// 3: The <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> does not contain an entry which is mapped
    /// to by the provided <paramref name="id"/> or <paramref name="key"/></exception>
    private async Task SetNameInternal(string name, int id, ReadOnlyMemory<byte> key) {
        if (key.IsEmpty) key = KeyUtils.GetKey(id);
        else id = BinaryPrimitives.ReadInt32BigEndian(key.Span);

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Name cannot be null or empty when setting name mapping.");

        if (!IsLegalName(name, out char illegalChar))
            throw new InvalidOperationException($"The provided name is not legal as it contained the following character: '{illegalChar}'");

        if (!await internalTable.Contains(key).ConfigureAwait(false))
            throw new InvalidOperationException("Cannot set a name to an id / key that does not exist in the table.");

        ReadOnlyMemory<byte> nameKey = Encoding.UTF8.GetBytes(name);

        if (!forward.TryAdd(nameKey, id)) forward.TryUpdate(nameKey.Span, id);
        if (!reverse.TryAdd(key, name)) reverse.TryUpdate(key.Span, name);
    }
    /// <summary>
    /// Helper method that removes the entry mapped to from the given <paramref name="name"/>, <paramref name="id"/>,
    /// or <paramref name="key"/>.
    /// </summary>
    /// <param name="name">The name of the entry to remove.</param>
    /// <param name="id">The id of the entry to remove.</param>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>An asyncronous task representing the opperation.</returns>
    /// <remarks>
    /// Only one of <paramref name="name"/>, <paramref name="id"/>, or <paramref name="key"/> must be specified per call.
    /// If <paramref name="name"/> is not empty <paramref name="id"/> will be retrieved from the name mapping table.
    /// If <paramref name="key"/> is empty it will be calculated from the <paramref name="id"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">If the specified name does not map to an id within the table.</exception>
    private async Task RemoveInternal(string name, int id, ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (!string.IsNullOrEmpty(name))
                if (!forward.TryGet(Encoding.UTF8.GetBytes(name), out id))
                    throw new InvalidOperationException("The specified name does not exist in the table.");

            if (key.IsEmpty) key = KeyUtils.GetKey(id);

            if (reverse.TryGet(key.Span, out string? mappedName)) {
                reverse.TryRemove(key.Span);
                forward.TryRemove(Encoding.UTF8.GetBytes(mappedName ?? string.Empty));
            }

            await internalTable.Remove(key).ConfigureAwait(false);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Static helper function which determines if the given <paramref name="name"/> is a valid name for an entry in an 
    /// <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/>.
    /// </summary>
    /// <param name="name">The name to check for legality.</param>
    /// <param name="illegalChar">If the <paramref name="name"/> is legal, undefined, otherwise, the first 
    /// character which is not legal.</param>
    /// <returns><see langword="true"/> if the given <paramref name="name"/> is legal, <see langword="false"/> otherwise.</returns>
    private static bool IsLegalName(string name, out char illegalChar) {
        illegalChar = char.MaxValue;
        foreach (char ch in name) {
            if (IsLegalNameCharacter(ch)) continue;
            illegalChar = ch;
            return false;
        }
        return true;
    }
    /// <summary>
    /// Static helper function which determines if the given <paramref name="ch"/> is a valid character for an
    /// <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> name.
    /// </summary>
    /// <param name="ch">The character to check for legality.</param>
    /// <returns><see langword="true"/> if the given <paramref name="ch"/> is legal, <see langword="false"/> otherwise.</returns>
    private static bool IsLegalNameCharacter(char ch) {
        if ('a' <= ch && ch <= 'z') return true; // lowercase letters are legal
        if ('A' <= ch && ch <= 'Z') return true; // uppercase letters are legal
        if ('0' <= ch && ch <= '9') return true; // digits are legal
        if (ch is '-' or '_') return true; // dash and underscore are legal
        if (ch is '[' or ']') return true; // square brackets are legal
        if (ch is '(' or ')') return true; // parentheses are legal
        if (ch is '<' or '>') return true; // angle barckets are legal
        return false;
    }
}