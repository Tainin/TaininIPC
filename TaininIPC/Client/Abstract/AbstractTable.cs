using System.Buffers.Binary;
using TaininIPC.Client.Interface;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

public abstract class AbstractTable<TInput, TStored> : ITable<TInput, TStored> where TInput : notnull where TStored : notnull {

    private readonly CritBitTree<TStored> table;
    private readonly SemaphoreSlim syncSemaphore;

    private readonly int firstAvailableId;
    private int nextAvailableId;

    public AbstractTable(int reservedCount) {
        (table, syncSemaphore) = (new(), new(1, 1));

        firstAvailableId = reservedCount;
        nextAvailableId = firstAvailableId;
    }
    
    public Task<int> Add(TInput input) => AddInternal(input, Interlocked.Increment(ref nextAvailableId));
    public Task<int> AddReserved(TInput input, int id) => id < firstAvailableId ? AddInternal(input, id) :
        throw new ArgumentOutOfRangeException(nameof(id), "The provided id is outside the reserved range.");
    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        table.Clear();
        syncSemaphore.Release();
    }
    public Task<TStored> Get(int id) => Get(GetKey(id));
    public async Task<TStored> Get(ReadOnlyMemory<byte> key) {
        (TStored? stored, bool got) = await GetInternal(key).ConfigureAwait(false);
        if (got && stored is not null) return stored;
        throw new ArgumentException("The provided key was not found in the table.");
    }
    public Task Remove(int id) => Remove(GetKey(id));
    public async Task Remove(ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool removed = table.TryRemove(key.Span);
        syncSemaphore.Release();

        if (removed) return;

        throw new ArgumentException("The provided key was not found in the table.");
    }

    protected abstract Task<int> AddInternal(TInput input, int id);
    protected async Task<int> AddInternalBase(TStored stored, int id) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool added = table.TryAdd(GetKey(id), stored);
        syncSemaphore.Release();

        if (added && stored is not null) return id;

        throw new ArgumentException("The provided id already exists in the table.");
    }
    protected async Task<(TStored? stored, bool got)> GetInternal(ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool got = table.TryGet(key.Span, out TStored? stored);
        syncSemaphore.Release();
        return (got ? stored : default, got);
    }

    private static ReadOnlyMemory<byte> GetKey(int id) {
        byte[] key = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(key, id);
        return key;
    }
}
