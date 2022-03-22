using TaininIPC.Client.Abstract;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Serialized;
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
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry origin) {
        Int32Key routingKey = new(Protocol.ExtractRoutingKey(frame));
        Attempt<IRouter> routerAttempt = await TryGet(routingKey).ConfigureAwait(false);
        if (routerAttempt.TryResult(out IRouter? router))
            await router!.RouteFrame(frame, origin).ConfigureAwait(false);
    }
    /// <summary>
    /// No-op transformation and call to <see cref="AbstractTable{TInput, TStored}.AddInternalBase(Int32Key, TStored)"/>
    /// </summary>
    /// <param name="key">The key to map to the specified <paramref name="input"/>.</param>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    protected override Task AddInternal(Int32Key key, IRouter input) => AddInternalBase(key, input);
}
