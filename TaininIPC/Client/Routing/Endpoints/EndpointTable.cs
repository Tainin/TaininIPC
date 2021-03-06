using TaininIPC.Client.Routing.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Client.Routing.Endpoints;

/// <summary>
/// Represents a table of <see cref="Int32Key"/> keys mapped to <see cref="EndpointTableEntry"/> entries.
/// </summary>
public sealed class EndpointTable : Table<EndpointTableEntry>, IRouter {

    /// <summary>
    /// Initializes a new <see cref="EndpointTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public EndpointTable(int reservedCount) : base(reservedCount) { }

    /// <summary>
    /// Routes the specified <paramref name="frame"/> by sending it through the endpoint specified by it's next routing key.
    /// </summary>
    /// <param name="frame">The frame to route.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {

        if (!frame.TryGetNextRoutingKey(out Int32Key? routingKey)) return;

        Attempt<EndpointTableEntry> attempt = await TryGet(routingKey).ConfigureAwait(false);
        if (attempt.TryResult(out EndpointTableEntry? entry))
            await entry.NetworkEndpoint.SendMultiFrame(frame).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Table{T}.OnAdded(T)"/>
    protected override void OnAdded(EndpointTableEntry entry) => entry.StartEndpoint();
    /// <inheritdoc cref="Table{T}.OnRemoved(T)"/>
    protected override void OnRemoved(EndpointTableEntry entry) => entry.NetworkEndpoint.Stop();
}
