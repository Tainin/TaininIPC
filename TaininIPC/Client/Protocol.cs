using TaininIPC.Data.Serialized;

namespace TaininIPC.Client;

public static class Protocol {
    public static ReadOnlyMemory<byte> ExtractRoutingKey(MultiFrame frame) => frame.Get(-1).Rotate();

    public static ReadOnlyMemory<byte> GetResponseKey(MultiFrame frame) => frame.Get(-2).Get(0);
    public static void SetResponseKey(MultiFrame frame, ReadOnlyMemory<byte> key) {
        Frame responseInfoFrame = frame.Get(-2);
        if (responseInfoFrame.Length < 1) responseInfoFrame.Insert(key, 0);
        else responseInfoFrame.Swap(0, key);
    }
}
