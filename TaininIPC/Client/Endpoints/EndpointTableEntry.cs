using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;
using TaininIPC.Network;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client.Endpoints;

/// <summary>
/// Represents an entry in an <see cref="Endpoints.EndpointTable"/> or <see cref="NameMappedEndpointTable"/>.
/// </summary>
public sealed class EndpointTableEntry {
    /// <summary>
    /// The entry's containing table.
    /// </summary>
    public EndpointTable EndpointTable { get; private init; }
    /// <summary>
    /// The <see cref="IRouter"/> responsible for routing <see cref="MultiFrame"/> instances received through the entry.
    /// </summary>
    public IRouter Router { get; private init; }
    /// <summary>
    /// The key mapped to the entry in <see cref="EndpointTable"/>.
    /// </summary>
    public Int32Key Key { get; private init; }

    private readonly INetworkEndpoint networkEndpoint;
    private readonly FrameEndpoint frameEndpoint;

    /// <summary>
    /// Initializes an <see cref="EndpointTableEntry"/> from it's containing <paramref name="endpointTable"/>, it's
    /// <paramref name="key"/> in that table, and an <paramref name="options"/> object.
    /// </summary>
    /// <param name="key">The key which will map to the entry in it's <paramref name="endpointTable"/>.</param>
    /// <param name="endpointTable">The <see cref="Endpoints.EndpointTable"/> to which the entry belongs.</param>
    /// <param name="options">The options to use when initializing the entry.</param>
    public EndpointTableEntry(Int32Key key, EndpointTable endpointTable, EndpointTableEntryOptions options) {
        networkEndpoint = options.NetworkFactory(HandleIncomingChunk);
        frameEndpoint = new(HandleOutgoingChunk, HandleIncomingFrame);

        EndpointTable = endpointTable;
        Router = options.Router;
        Key = key;
    }

    private Task HandleOutgoingChunk(NetworkChunk chunk) => networkEndpoint.SendChunk(chunk);
    private Task HandleIncomingChunk(NetworkChunk chunk) => frameEndpoint.ApplyChunk(chunk);
    private Task HandleIncomingFrame(MultiFrame multiFrame) => Router.RouteFrame(multiFrame, this);
}
