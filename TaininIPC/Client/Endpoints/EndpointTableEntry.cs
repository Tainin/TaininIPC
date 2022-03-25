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
    /// The <see cref="INetworkEndpoint"/> which handles sending and receiving <see cref="NetworkChunk"/> instances for the entry.
    /// </summary>
    public INetworkEndpoint NetworkEndpoint { get; private init; }
    /// <summary>
    /// The <see cref="Network.FrameEndpoint"/> which handles sending and receiveing <see cref="MultiFrame"/> instances for the entry.
    /// </summary>
    public FrameEndpoint FrameEndpoint { get; private init; }
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

    /// <summary>
    /// Initializes an <see cref="EndpointTableEntry"/> from it's containing <paramref name="endpointTable"/>, it's
    /// <paramref name="key"/> in that table, and an <paramref name="options"/> object.
    /// </summary>
    /// <param name="key">The key which will map to the entry in it's <paramref name="endpointTable"/>.</param>
    /// <param name="endpointTable">The <see cref="Endpoints.EndpointTable"/> to which the entry belongs.</param>
    /// <param name="options">The options to use when initializing the entry.</param>
    public EndpointTableEntry(Int32Key key, EndpointTable endpointTable, EndpointTableEntryOptions options) {
        NetworkEndpoint = options.NetworkFactory(HandleIncomingChunk);
        FrameEndpoint = new(HandleOutgoingChunk, HandleIncomingFrame);

        EndpointTable = endpointTable;
        Router = options.Router;
        Key = key;
    }

    private Task HandleOutgoingChunk(NetworkChunk chunk) => NetworkEndpoint.SendChunk(chunk);
    private Task HandleIncomingChunk(NetworkChunk chunk) => FrameEndpoint.ApplyChunk(chunk);
    private Task HandleIncomingFrame(MultiFrame multiFrame) => Router.RouteFrame(multiFrame, this);
}
