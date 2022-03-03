using TaininIPC.Client.Abstract;

namespace TaininIPC.Client.Endpoints;

public sealed class EndpointTable : AbstractTable<EndpointTableEntryOptions, EndpointTableEntry> {
    public EndpointTable(int reservedCount) : base(reservedCount) { }
    protected override async Task<int> AddInternal(EndpointTableEntryOptions options, int id) {
        EndpointTableEntry entry = new(id, this, options);
        return await AddInternalBase(entry, id).ConfigureAwait(false);
    }
}

public sealed class NameMappedEndpointTable : NameMappedTable<EndpointTable, EndpointTableEntryOptions, EndpointTableEntry> {
    public NameMappedEndpointTable(int reservedCount) : base(new(reservedCount)) { }
}
