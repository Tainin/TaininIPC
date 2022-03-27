using TaininIPC.Client.Abstract;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Serialized;
using TaininIPC.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Client.Routing;

/// <summary>
/// Represents a table of <see cref="IRouter"/> entries mapped to by <see cref="Int32Key"/> instances.
/// </summary>
public sealed class RoutingTable : AbstractTable<IRouter, IRouter>, IRouter {
    /// <summary>
    /// Initializes a new <see cref="RoutingTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public RoutingTable(int reservedCount) : base(reservedCount) { }

    /// <inheritdoc cref="IRouter.RouteFrame(MultiFrame, EndpointTableEntry)"/>
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry? origin) => RouteFrame(this, frame, origin);
    /// <summary>
    /// No-op transformation and call to <see cref="AbstractTable{TInput, TStored}.AddInternalBase(Int32Key, TStored)"/>
    /// </summary>
    /// <param name="key">The key to map to the specified <paramref name="input"/>.</param>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    protected override Task AddInternal(Int32Key key, IRouter input) => AddInternalBase(key, input);

    /// <summary>
    /// Static helper function which routes the given <paramref name="frame"/> to the <see cref="IRouter"/> specified by the <paramref name="table"/>
    /// and the routing key embeded in the given <paramref name="frame"/>.
    /// </summary>
    /// <param name="table">The table to find the <see cref="IRouter"/> in.</param>
    /// <param name="frame">The frame to route.</param>
    /// <param name="origin">The endpoint which the <paramref name="frame"/> arived through or <see langword="null"/> if it originated locally.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public static async Task RouteFrame(ITable<IRouter, IRouter> table, MultiFrame frame, EndpointTableEntry? origin) {
        if (!ProtocolHelper.TryGetRoutingKey(frame, out Int32Key? routingKey)) return;

        Attempt<IRouter> routerAttempt = await table.TryGet(routingKey).ConfigureAwait(false);
        if (routerAttempt.TryResult(out IRouter? router))
            await router!.RouteFrame(frame, origin).ConfigureAwait(false);
    }
}
