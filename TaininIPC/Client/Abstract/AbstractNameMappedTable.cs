using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

/// <summary>
/// Wraps a subtype of <see cref="AbstractTable{TInput, TStored}"/> and adds an additional level of mapping from <see langword="string"/> 
/// names to the ids of the sub table.
/// </summary>
/// <typeparam name="TableType">The type of the sub table. Must extend <see cref="AbstractTable{TInput, TStored}"/>.</typeparam>
/// <typeparam name="TInput">The <typeparamref name="TInput"/> of the sub sub table - <see cref="AbstractTable{TInput, TStored}"/>.</typeparam>
/// <typeparam name="TStored">The <typeparamref name="TStored"/> of the sub table - <see cref="AbstractTable{TInput, TStored}"/>.</typeparam>*/
public abstract class AbstractNameMappedTable<TableType, TInput, TStored> : INameMappedTable<TInput, TStored> where
    TableType : AbstractTable<TInput, TStored> where TInput : notnull where TStored : notnull {

    private readonly CritBitTree<StringKey, Int32Key> forward;
    private readonly CritBitTree<Int32Key, StringKey> reverse;
    private readonly SemaphoreSlim syncSemaphore;
    private readonly TableType internalTable;

    /// <inheritdoc cref="ITable{TInput, TStored}.ReservedCount"/>
    public int ReservedCount => internalTable.ReservedCount;

    /// <summary>
    /// Initializes an instance of <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> given an instance of the 
    /// sub table type (<typeparamref name="TableType"/>)
    /// </summary>
    /// <param name="internalTable">The sub table to wrap the <see cref="AbstractNameMappedTable{TableType, TInput, TStored}"/> around.</param>
    public AbstractNameMappedTable(TableType internalTable) {
        (forward, reverse, syncSemaphore) = (new(), new(), new(1, 1));

        this.internalTable = internalTable;
    }

    /// <inheritdoc cref="INameMappedTable{TInput, TStored}.TryAddName(StringKey, Int32Key)"/>
    public async Task<bool> TryAddName(StringKey nameKey, Int32Key key) {
        if (string.IsNullOrWhiteSpace(nameKey.Id)) return false;
        if (!IsLegalName(nameKey.Id, out char illegalChar)) return false;

        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (!await internalTable.Contains(key).ConfigureAwait(false)) return false;

            if (forward.ContainsKey(nameKey)) return false;
            if (reverse.ContainsKey(key)) return false;

            forward.TryAdd(nameKey, key);
            reverse.TryAdd(key, nameKey);
            return true;
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="INameMappedTable{TInput, TStored}.TryRemoveName(StringKey)"/>
    public async Task<Attempt<Int32Key>> TryRemoveName(StringKey nameKey) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return forward.TryPop(nameKey, out Int32Key? key) ? 
                reverse.TryRemove(key!).ToAttempt(key) : Attempt<Int32Key>.Failed;
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="INameMappedTable{TInput, TStored}.TryRemoveName(Int32Key)"/>
    public async Task<bool> TryRemoveName(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return reverse.TryPop(key, out StringKey? nameKey) && forward.TryRemove(nameKey!);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="INameMappedTable{TInput, TStored}.TryGetKey(StringKey)"/>
    public async Task<Attempt<Int32Key>> TryGetKey(StringKey nameKey) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return forward.TryGet(nameKey, out Int32Key? key).ToAttempt(key);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="INameMappedTable{TInput, TStored}.TryGetNameKey(Int32Key)"/>
    public async Task<Attempt<StringKey>> TryGetNameKey(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return reverse.TryGet(key, out StringKey? nameKey).ToAttempt(nameKey);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <inheritdoc cref="ITable{TInput, TStored}.Add(TInput)"/>
    public async Task<Int32Key> Add(TInput input) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return await internalTable.Add(input);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.AddReserved(Int32Key, TInput)"/>
    public async Task AddReserved(Int32Key key, TInput input) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            await internalTable.AddReserved(key, input);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.Clear"/>
    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            await internalTable.Clear();
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.TryGet(Int32Key)"/>
    public async Task<Attempt<TStored>> TryGet(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return await internalTable.TryGet(key);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.Contains(Int32Key)"/>
    public async Task<bool> Contains(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return await internalTable.Contains(key);
        } finally {
            syncSemaphore.Release();
        }
    }
    /// <inheritdoc cref="ITable{TInput, TStored}.TryRemove(Int32Key)"/>
    public async Task<bool> TryRemove(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            return await internalTable.TryRemove(key);
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