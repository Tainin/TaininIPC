using System.Buffers.Binary;
using System.Text;
using TaininIPC.Client.Interface;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

public abstract class AbstractNameMappedTable<TableType, TInput, TStored> : ITable<TInput,TStored> 
    where TableType : AbstractTable<TInput, TStored> where TInput : notnull where TStored : notnull {

    private readonly CritBitTree<int> forward;
    private readonly CritBitTree<string> reverse;
    private readonly SemaphoreSlim syncSemaphore;

    protected readonly TableType internalTable;

    public AbstractNameMappedTable(TableType internalTable) {
        (forward, reverse, syncSemaphore) = (new(), new(), new(1, 1));

        this.internalTable = internalTable;
    }

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
    public Task<int> Add(TInput input) => syncSemaphore.AquireAndRun(internalTable.Add, input);

    public Task AddReserved(TInput input, int id) => syncSemaphore.AquireAndRun(internalTable.AddReserved, input, id);
    public async Task AddReserved(string name, TInput input, int id) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            await internalTable.AddReserved(input, id).ConfigureAwait(false);
            await SetNameInternal(name, id, ReadOnlyMemory<byte>.Empty).ConfigureAwait(false);
        } finally {
            syncSemaphore.Release();
        }
    }

    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        await internalTable.Clear().ConfigureAwait(false);
        forward.Clear();
        reverse.Clear();
        syncSemaphore.Release();
    }

    public Task SetName(string name, int id) => syncSemaphore.AquireAndRun(SetNameInternal, name, id, ReadOnlyMemory<byte>.Empty);
    public Task SetName(string name, ReadOnlyMemory<byte> key) => syncSemaphore.AquireAndRun(SetNameInternal, name, 0, key);

    public async Task<TStored> Get(string name) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            ReadOnlyMemory<byte> nameKey = Encoding.UTF8.GetBytes(name);
            if (!forward.TryGet(nameKey.Span, out int id))
                throw new InvalidOperationException("The specified name does not exist in the table.");
            return await internalTable.Get(id).ConfigureAwait(false);
        } finally {
            syncSemaphore.Release();
        }
    }
    public Task<TStored> Get(int id) => syncSemaphore.AquireAndRun(internalTable.Get, id);
    public Task<TStored> Get(ReadOnlyMemory<byte> key) => syncSemaphore.AquireAndRun(internalTable.Get, key);

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
    public Task<bool> Contains(int id) => syncSemaphore.AquireAndRun(internalTable.Contains, id);
    public Task<bool> Contains(ReadOnlyMemory<byte> key) => syncSemaphore.AquireAndRun(internalTable.Contains, key);

    public Task Remove(string name) => RemoveInternal(name, 0, ReadOnlyMemory<byte>.Empty);
    public Task Remove(int id) => RemoveInternal(string.Empty, id, ReadOnlyMemory<byte>.Empty);
    public Task Remove(ReadOnlyMemory<byte> key) => RemoveInternal(string.Empty, 0, key);

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
    private static bool IsLegalName(string name, out char illegalChar) {
        illegalChar = char.MaxValue;
        foreach (char ch in name) {
            if (IsLegalNameCharacter(ch)) continue;
            illegalChar = ch;
            return false;
        }
        return true;
    }
    private static bool IsLegalNameCharacter(char ch) {
        if ('a' <= ch && ch <= 'z') return true;
        if ('A' <= ch && ch <= 'Z') return true;
        if ('0' <= ch && ch <= '9') return true;
        if (ch is '-' or '_') return true;
        if (ch is '[' or ']') return true;
        if (ch is '(' or ')') return true;
        if (ch is '<' or '>') return true;
        return false;
    }
}