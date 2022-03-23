using System.Diagnostics.CodeAnalysis;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Serialized;
using TaininIPC.Utils;

namespace TaininIPC.Client;

public static class Protocol {

    private static readonly Int16Key KEY_ROUTING_KEY = new(-1);
    private static readonly Int16Key KEY_RESPONSE_KEY = new(-2);

    public static bool TryGetRoutingKey(MultiFrame frame, [NotNullWhen(true)]out Int32Key? routingKey) {
        if (!frame.TryGet(KEY_ROUTING_KEY, out Frame? subFrame)) return UtilityFunctions.DefaultAndFalse(out routingKey);
        routingKey = new(subFrame.Rotate());
        return true;
    }

    public static bool TryGetResponseKey(MultiFrame frame, [NotNullWhen(true)]out BasicKey? responseKey) {
        if (!frame.TryGet(KEY_RESPONSE_KEY, out Frame? subFrame)) return UtilityFunctions.DefaultAndFalse(out responseKey);
        responseKey = new(subFrame.Get(0));
        return true;
    }
    public static bool SetResponseKey(MultiFrame frame, BasicKey key) {
        if (!frame.TryGet(KEY_RESPONSE_KEY, out Frame? subFrame)) return false;

        if (subFrame.Length < 1) subFrame.Insert(key.Memory, 0);
        else subFrame.Swap(0, key.Memory);
        return true;
    }
}
