using TaininIPC.Client.Interface;
using TaininIPC.Network;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client.Endpoints;

public delegate INetworkEndpoint NetworkEndpointFactory(ChunkHandler chunkHandler);
public record EndpointTableEntryOptions(NetworkEndpointFactory NetworkFactory, IRouter Router);
