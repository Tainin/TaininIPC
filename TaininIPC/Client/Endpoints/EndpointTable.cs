using TaininIPC.Client.Abstract;
using TaininIPC.CritBitTree.Keys;

namespace TaininIPC.Client.Endpoints;

public sealed class EndpointTable : AbstractTable<EndpointTableEntryOptions, EndpointTableEntry> {
    public EndpointTable(int reservedCount) : base(reservedCount) { }
    protected override async Task AddInternal(Int32Key key, EndpointTableEntryOptions options) =>
        await AddInternalBase(key, new(key, this, options)).ConfigureAwait(false);
}
