using TaininIPC.Client.Interface;
using TaininIPC.Network;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client.Endpoints;
/// <summary>
/// Represents a function which constructs an <see cref="INetworkEndpoint"/> from a <see cref="ChunkHandler"/> 
/// which processes recieved <see cref="Data.Protocol.NetworkChunk"/> instances.
/// </summary>
/// <param name="chunkHandler">The handler the <see cref="INetworkEndpoint"/> should use for recieved <see cref="Data.Protocol.NetworkChunk"/>
/// instances.</param>
/// <returns>The constructed <see cref="INetworkEndpoint"/>.</returns>
public delegate INetworkEndpoint NetworkEndpointFactory(ChunkHandler chunkHandler);
/// <summary>
/// Represents a set of options for initializing <see cref="EndpointTableEntry"/> instances.
/// </summary>
/// <param name="NetworkFactory">The factory to use to constuct the network endpoint for the entry.</param>
/// <param name="Router">The <see cref="IRouter"/> the entry should use to route received <see cref="Data.Serialized.MultiFrame"/> instances.</param>
public record EndpointTableEntryOptions(NetworkEndpointFactory NetworkFactory, IRouter Router);
