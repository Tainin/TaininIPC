using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Abstract;

namespace TaininIPC.Client.Endpoints;

/// <summary>
/// Represents an entry in an <see cref="EndpointTable"/>.
/// </summary>
public sealed class EndpointTableEntry : IRouter {
    
    /// <summary>
    /// The endpoint used to send and receive <see cref="MultiFrame"/> instances through the entry.
    /// </summary>
    public AbstractNetworkEndpoint NetworkEndpoint { get; }
    /// <summary>
    /// The <see cref="IRouter"/> responsible for routing <see cref="MultiFrame"/> instances received through the entry.
    /// </summary>
    public IRouter Router { get; }
    /// <summary>
    /// The key mapped to the entry in <see cref="EndpointTable"/>.
    /// </summary>
    public Int32Key Key { get; }

    /// <summary>
    /// Initializes an <see cref="EndpointTableEntry"/> from it's <paramref name="key"/> and an <paramref name="options"/> object.
    /// </summary>
    /// <param name="key">The key which maps to the entry in it's table.</param>
    /// <param name="options">The options to use when initializing the entry.</param>
    public EndpointTableEntry(Int32Key key, EndpointTableEntryOptions options) {
        NetworkEndpoint = options.NetworkFactory(this);
        Router = options.Router;
        Key = key;
    }

    //TODO: Implement breadcrumb return route path tracing here
    //      Check protocol helper RETURN_PATH_KEY
    //      Should probably write custom xml-doc
    /// <inheritdoc cref="IRouter.RouteFrame(MultiFrame, EndpointTableEntry?)"/>
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) => Router.RouteFrame(frame, this);
}
