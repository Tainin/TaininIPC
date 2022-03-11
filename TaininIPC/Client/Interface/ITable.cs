namespace TaininIPC.Client.Interface;

public interface ITable<TInput, TStored> where TInput : notnull where TStored : notnull {
    public Task<int> Add(TInput input);
    public Task AddReserved(TInput input, int id);
    public Task Clear();
    public Task<TStored> Get(int id);
    public Task<TStored> Get(ReadOnlyMemory<byte> key);
    public Task<bool> Contains(int id);
    public Task<bool> Contains(ReadOnlyMemory<byte> key);
    public Task Remove(int id);
    public Task Remove(ReadOnlyMemory<byte> key);
}