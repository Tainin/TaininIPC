using TaininIPC.Data.Serialized;

namespace TaininIPC.Client;

public static class Protocol {
    public static ReadOnlyMemory<byte> ExtractRoutingKey(MultiFrame frame) => frame.Get(-1).Rotate();
}
