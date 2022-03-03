
namespace TaininIPC.Client.Interface;

public interface ITable<TInput, TStored>
    where TInput : notnull
    where TStored : notnull {
    Task<int> Add(TInput input);
    Task AddReserved(TInput input, int id);
    Task Clear();
    Task<TStored> Get(int id);
    Task<TStored> Get(ReadOnlyMemory<byte> key);
    Task Remove(int id);
    Task Remove(ReadOnlyMemory<byte> key);
}