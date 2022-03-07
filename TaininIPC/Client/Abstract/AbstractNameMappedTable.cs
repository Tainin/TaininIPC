using System.Text;
using TaininIPC.Client.Interface;
using TaininIPC.Utils;

namespace TaininIPC.Client.Abstract;

public abstract class AbstractNameMappedTable<TableType, TInput, TStored> where TableType : ITable<TInput, TStored>
    where TInput : notnull where TStored : notnull {

    private readonly CritBitTree<int> nameMap;
    private readonly SemaphoreSlim syncSemaphore;

    protected readonly TableType internalTable;

    public AbstractNameMappedTable(TableType table) {
        (nameMap, syncSemaphore) = (new(), new(1, 1));
        internalTable = table;
    }

    public Task<int> Add(string name, TInput input) => AddInternal(name, 0, input, reserved: false, named: true);
    public Task<int> AddUnnamed(TInput input) => AddInternal(string.Empty, 0, input, reserved: false, named: false);
    public Task AddReserved(string name, int id, TInput input) => AddInternal(name, id, input, reserved: true, named: true);
    public Task AddReservedUnnamed(int id, TInput input) => AddInternal(string.Empty, id, input, reserved: true, named: false);

    public async Task Clear() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        await internalTable.Clear().ConfigureAwait(false);
        nameMap.Clear();
        syncSemaphore.Release();
    }

    public Task<TStored> Get(int id) => GetInternal(string.Empty, id, ReadOnlyMemory<byte>.Empty);
    public Task<TStored> Get(ReadOnlyMemory<byte> key) => GetInternal(string.Empty, 0, key);
    public Task<TStored> Get(string name) => GetInternal(name, 0, ReadOnlyMemory<byte>.Empty);

    public Task Remove(int id) => RemoveInternal(string.Empty, id, ReadOnlyMemory<byte>.Empty);
    public Task Remove(ReadOnlyMemory<byte> key) => RemoveInternal(string.Empty, 0, key);
    public Task Remove(string name) => RemoveInternal(name, 0, ReadOnlyMemory<byte>.Empty);

    private async Task<int> AddInternal(string name, int id, TInput input, bool reserved, bool named) {
        if (string.IsNullOrEmpty(name)) 
            throw new ArgumentException("Name must not be null or empty", nameof(name));
        ReadOnlyMemory<byte> nameKey = Encoding.UTF8.GetBytes(name);
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (reserved) await internalTable.AddReserved(input, id).ConfigureAwait(false);
            else id = await internalTable.Add(input).ConfigureAwait(false);

            if (!named) return id;

            if (nameMap.TryAdd(nameKey, id)) return id;

            await internalTable.Remove(id).ConfigureAwait(false);
            throw new ArgumentException($"The {nameof(AbstractNameMappedTable<TableType, TInput, TStored>)} " +
                $"alreay contains an entry with the provided name.", nameof(name));
        } finally {
            syncSemaphore.Release();
        }
    }
    private async Task<TStored> GetInternal(string name, int id, ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (!key.IsEmpty) return await internalTable.Get(key).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(name)) id = GetIdFromString(name);

            try {
                return await internalTable.Get(id).ConfigureAwait(false);
            } catch (ArgumentException) {
                nameMap.TryRemove(Encoding.UTF8.GetBytes(name));
                throw;
            }
            
        } finally {
            syncSemaphore.Release();
        }
    }
    private async Task RemoveInternal(string name, int id, ReadOnlyMemory<byte> key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (key.IsEmpty) {
                if (!string.IsNullOrEmpty(name)) id = GetIdFromString(name);
                await internalTable.Remove(id).ConfigureAwait(false);
            } else await internalTable.Remove(key).ConfigureAwait(false);
        } finally {
            nameMap.TryRemove(Encoding.UTF8.GetBytes(name));
            syncSemaphore.Release();
        }
    }
    private int GetIdFromString(string name) {
        ReadOnlyMemory<byte> nameKey = Encoding.UTF8.GetBytes(name);
        if (!nameMap.TryGet(nameKey.Span, out int id))
            throw new ArgumentException($"The {nameof(AbstractNameMappedTable<TableType, TInput, TStored>)} " +
                $"does not contain an entry with the provided name.", nameof(name));
        return id;
    }
}