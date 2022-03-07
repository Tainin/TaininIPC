using TaininIPC.Client.Abstract;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client.Routing;

public sealed class NameMappedRoutingTable : AbstractNameMappedTable<RoutingTable, IRouter, IRouter>, IRouter {
    public NameMappedRoutingTable(int reservedCount, RoutingTable.KeyExtractor keyExtractor) : base(new(reservedCount, keyExtractor)) { }

    public Task RouteFrame(MultiFrame frame, EndpointTableEntry origin) => internalTable.RouteFrame(frame, origin);
}