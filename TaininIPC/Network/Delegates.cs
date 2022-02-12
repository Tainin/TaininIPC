using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Network;

public delegate Task ChunkHandler(NetworkChunk networkChunk);
public delegate Task MultiFrameHandler(MultiFrame multiFrame);