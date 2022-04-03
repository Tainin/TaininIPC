using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Abstract;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client.Routing.Endpoints;

/// <summary>
/// Represents a function which constructs an <see cref="INetworkEndpoint"/> given an <see cref="IRouter"/> 
/// to route <see cref="MultiFrame"/> instances received through it.
/// </summary>
/// <param name="router">The router the endpoint should use to route it's frames.</param>
/// <returns>The constructed <see cref="INetworkEndpoint"/>.</returns>
public delegate INetworkEndpoint NetworkEndpointFactory(IRouter router);

/// <summary>
/// Represents a set of options for initializing <see cref="EndpointTableEntry"/> instances.
/// </summary>
/// <param name="NetworkFactory">The factory to use to constuct the network endpoint for the entry.</param>
/// <param name="Router">The <see cref="IRouter"/> the entry should use to route received <see cref="MultiFrame"/> instances.</param>
public record EndpointTableEntryOptions(NetworkEndpointFactory NetworkFactory, IRouter Router);