#if false
using TaininIPC.Client.Abstract;

namespace TaininIPC.Client.Endpoints;

public sealed class NameMappedEndpointTable : AbstractNameMappedTable<EndpointTable, EndpointTableEntryOptions, EndpointTableEntry> {
    public NameMappedEndpointTable(int reservedCount) : base(new(reservedCount)) { }
}
#endif