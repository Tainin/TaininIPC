using TaininIPC.Client.Interface;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Abstract;

namespace TaininIPC.Client.Endpoints;

/// <summary>
/// Represents a function which constructs an <see cref="AbstractNetworkEndpoint"/> from an <see cref="IRouter"/> 
/// which routes received <see cref="MultiFrame"/> instances.
/// </summary>
/// <param name="router">The router the endpoint should use to route it's frames.</param>
/// <returns>The constructed <see cref="AbstractNetworkEndpoint"/>.</returns>
public delegate AbstractNetworkEndpoint NetworkEndpointFactory(IRouter router);

/// <summary>
/// Represents a set of options for initializing <see cref="EndpointTableEntry"/> instances.
/// </summary>
/// <param name="NetworkFactory">The factory to use to constuct the network endpoint for the entry.</param>
/// <param name="Router">The <see cref="IRouter"/> the entry should use to route received <see cref="Data.Frames.MultiFrame"/> instances.</param>
public record EndpointTableEntryOptions(NetworkEndpointFactory NetworkFactory, IRouter Router);