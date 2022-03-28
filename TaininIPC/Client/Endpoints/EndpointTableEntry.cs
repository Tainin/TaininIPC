using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Abstract;

namespace TaininIPC.Client.Endpoints;

/// <summary>
/// Represents an entry in an <see cref="Endpoints.EndpointTable"/> or <see cref="NameMappedEndpointTable"/>.
/// </summary>
public sealed class EndpointTableEntry : IRouter {
    
    /// <summary>
    /// The endpoint used to send and receive <see cref="MultiFrame"/> instances through the entry.
    /// </summary>
    public AbstractNetworkEndpoint NetworkEndpoint { get; }
    /// <summary>
    /// The entry's containing table.
    /// </summary>
    public EndpointTable EndpointTable { get; }
    /// <summary>
    /// The <see cref="IRouter"/> responsible for routing <see cref="MultiFrame"/> instances received through the entry.
    /// </summary>
    public IRouter Router { get; }
    /// <summary>
    /// The key mapped to the entry in <see cref="EndpointTable"/>.
    /// </summary>
    public Int32Key Key { get; }

    /// <summary>
    /// Initializes an <see cref="EndpointTableEntry"/> from it's containing <paramref name="endpointTable"/>, it's
    /// <paramref name="key"/> in that table, and an <paramref name="options"/> object.
    /// </summary>
    /// <param name="key">The key which will map to the entry in it's <paramref name="endpointTable"/>.</param>
    /// <param name="endpointTable">The <see cref="Endpoints.EndpointTable"/> to which the entry belongs.</param>
    /// <param name="options">The options to use when initializing the entry.</param>
    public EndpointTableEntry(Int32Key key, EndpointTable endpointTable, EndpointTableEntryOptions options) {
        NetworkEndpoint = options.NetworkFactory(this);
        Router = options.Router;

        EndpointTable = endpointTable;
        Key = key;
    }

    /// <inheritdoc cref="IRouter.RouteFrame(MultiFrame, EndpointTableEntry?)"/>
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) => Router.RouteFrame(frame, this);
}
