using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.Client.Table;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Client.Routing;

/// <summary>
/// Represents a table of <see cref="IRouter"/> entries mapped to by <see cref="Int32Key"/> instances.
/// </summary>
public sealed class RoutingTable : Table<IRouter>, IRouter {
    /// <summary>
    /// Initializes a new <see cref="RoutingTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public RoutingTable(int reservedCount) : base(reservedCount) { }

    /// <inheritdoc cref="IRouter.RouteFrame(MultiFrame, EndpointTableEntry)"/>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? origin) {
        if (!ProtocolHelper.TryGetRoutingKey(frame, out Int32Key? routingKey)) return;

        Attempt<IRouter> routerAttempt = await TryGet(routingKey).ConfigureAwait(false);
        if (routerAttempt.TryResult(out IRouter? router))
            await router!.RouteFrame(frame, origin).ConfigureAwait(false);
    }
}
