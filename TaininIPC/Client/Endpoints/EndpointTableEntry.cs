using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Abstract;
using TaininIPC.Protocol;

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

    /// <summary>
    /// Routes the specified <paramref name="frame"/> to the entry's <see cref="Router"/> with the entry as it's origin.
    /// If the <paramref name="frame"/> contains a return routing path, prepends the entry's <see cref="Key"/> to it.
    /// </summary>
    /// <param name="frame">The frame to route.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {
        if (frame.TryGet(ProtocolMultiFrameKeys.RETURN_ROUTING_PATH_KEY, out Frame? returnPath))
           returnPath.Insert(Key.Memory, 0);
        return Router.RouteFrame(frame, this);
    }
}
