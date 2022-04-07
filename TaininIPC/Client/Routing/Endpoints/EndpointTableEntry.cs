using TaininIPC.Client.Routing.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Interface;
using TaininIPC.Protocol;

namespace TaininIPC.Client.Routing.Endpoints;

/// <summary>
/// Represents an entry in an <see cref="EndpointTable"/>.
/// </summary>
public sealed class EndpointTableEntry : IRouter {
    
    /// <summary>
    /// The endpoint used to send and receive <see cref="MultiFrame"/> instances through the entry.
    /// </summary>
    public INetworkEndpoint NetworkEndpoint { get; }
    /// <summary>
    /// The <see cref="IRouter"/> responsible for routing <see cref="MultiFrame"/> instances received through the entry.
    /// </summary>
    public IRouter Router { get; }
    /// <summary>
    /// The key mapped to the entry in <see cref="EndpointTable"/>.
    /// </summary>
    public Int32Key Key { get; }

    private Task? endpointRunTask;
    private EndpointTable table;

    private readonly int UNSTARTED = 0;
    private readonly int STARTED = 1;
    private int started = 0;

    /// <summary>
    /// Initializes an <see cref="EndpointTableEntry"/> from it's <paramref name="key"/> and an <paramref name="options"/> object.
    /// </summary>
    /// <param name="key">The key which maps to the entry in it's table.</param>
    /// <param name="table">The table which contains the entry.</param>
    /// <param name="options">The options to use when initializing the entry.</param>
    public EndpointTableEntry(Int32Key key, EndpointTable table, EndpointTableEntryOptions options) {
        NetworkEndpoint = options.Connection.ToEndpoint(this);
        Router = options.Router;
        Key = key;

        this.table = table;
    }

    /// <summary>
    /// Runs the <see cref="NetworkEndpoint"/> and holds a reference to the task representing the running endpoint.
    /// </summary>
    public void StartEndpoint() {
        if (Interlocked.CompareExchange(ref started, STARTED, UNSTARTED) != UNSTARTED) return;
        endpointRunTask = RunEndpoint();
    }

    /// <summary>
    /// Routes the specified <paramref name="frame"/> to the entry's <see cref="Router"/> with the entry as it's origin.
    /// If the <paramref name="frame"/> contains a return routing path, prepends the entry's <see cref="Key"/> to it.
    /// </summary>
    /// <param name="frame">The frame to route.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {
        frame.PrependReturnPathIfPresent(StaticRoutingKeys.ENDPOINT_TABLE_ROUTE_KEY, Key);
        try {
            await Router.RouteFrame(frame, this).ConfigureAwait(false);
        } catch {
            //TODO: How should exceptions thrown during frame routing be handled?
        }
    }

    private async Task RunEndpoint() {
        try {
            await NetworkEndpoint.Run().ConfigureAwait(false);
        } finally {
            await table.TryRemove(Key).ConfigureAwait(false);
        }
    }
}
