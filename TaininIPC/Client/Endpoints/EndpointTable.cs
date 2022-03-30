using TaininIPC.Client.Interface;
using TaininIPC.Client.Table;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Client.Endpoints;

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
        if (!ProtocolHelper.TryGetRoutingKey(frame, out Int32Key? routingKey)) return;

        Attempt<EndpointTableEntry> attempt = await TryGet(routingKey).ConfigureAwait(false);
        if (attempt.TryResult(out EndpointTableEntry? entry))
            await entry.NetworkEndpoint.SendMultiFrame(frame).ConfigureAwait(false);
    }
}
