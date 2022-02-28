using TaininIPC.Data.Protocol;

namespace TaininIPC.Network;

public delegate Task ChunkHandler(NetworkChunk chunk);