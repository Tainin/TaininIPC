using TaininIPC.Utils;

namespace TaininIPC.Client.Interface;

public interface ITable<TInput, TStored> where TInput : notnull where TStored : notnull {
    public Task<int> Add(TInput input);
    public Task AddReserved(TInput input, int id);
    public Task Clear();
    public Task<Attempt<TStored>> TryGet(int id);
    public Task<Attempt<TStored>> TryGet(ReadOnlyMemory<byte> key);
    public Task<bool> Contains(int id);
    public Task<bool> Contains(ReadOnlyMemory<byte> key);
    public Task<bool> TryRemove(int id);
    public Task<bool> TryRemove(ReadOnlyMemory<byte> key);
}