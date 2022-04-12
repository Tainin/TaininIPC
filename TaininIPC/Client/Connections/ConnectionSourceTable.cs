using TaininIPC.Client.Connections.Interface;
using TaininIPC.Client.Routing;
using TaininIPC.Client.Routing.Endpoints;
using TaininIPC.Client.Routing.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Client.Connections;

/// <summary>
/// Represents a table of <see cref="Int32Key"/> keys mapped to <see cref="IConnectionSource"/> entries.
/// </summary>
public sealed class ConnectionSourceTable : Table<IConnectionSource>, IRouter {
    /// <summary>
    /// Initializes a new <see cref="ConnectionSourceTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public ConnectionSourceTable(int reservedCount) : base(reservedCount) { }

    /// <summary>
    /// Routes the specified <paramref name="frame"/> by attempting to complete a connection request
    /// through the <see cref="IConnectionSource"/> specified in it's body.
    /// </summary>
    /// <param name="frame">The frame to route.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {
        if (!frame.TryGet(MultiFrameKeys.CONNECTION_INFO_KEY, out Frame? subFrame)) return;

        Int32Key connectionSourceTableKey = new(subFrame.Get(^1));
        Attempt<IConnectionSource> attempt = await TryGet(connectionSourceTableKey).ConfigureAwait(false);
        if (attempt.TryResult(out IConnectionSource? connectionSource))
            await connectionSource.CompleteConnectionRequest(subFrame).ConfigureAwait(false);
    }
}
