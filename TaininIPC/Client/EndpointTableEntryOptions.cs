﻿using TaininIPC.Client.Interface;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client;

public delegate INetworkEndpoint NetworkEndpointFactory(ChunkHandler chunkHandler);
public record EndpointTableEntryOptions(NetworkEndpointFactory NetworkFactory, IRouter Router);