using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client;

public delegate Task ChunkHandler(NetworkChunk chunk);
public delegate Task MultiFrameHandler(MultiFrame multiFrame);