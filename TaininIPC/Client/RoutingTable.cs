using TaininIPC.Client.Abstract;
using TaininIPC.Client.Interface;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client;

public sealed class RoutingTable : AbstractTable<IRouter, IRouter>,  IRouter {
    public RoutingTable(int reservedCount) : base(reservedCount) { }
    protected override Task<int> AddInternal(IRouter input, int id) => AddInternalBase(input, id);
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry origin) {
        (IRouter? router, bool got) = await GetInternal(frame.Get(-1).Rotate()).ConfigureAwait(false);
        if (got && router is not null) await router.RouteFrame(frame, origin).ConfigureAwait(false);
    }
}
