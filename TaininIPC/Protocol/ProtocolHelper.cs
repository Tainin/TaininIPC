using System.Diagnostics.CodeAnalysis;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Utils;

namespace TaininIPC.Protocol;

/// <summary>
/// Contains helper functions for reading and writing protocol related data to and from <see cref="Frame"/> and <see cref="MultiFrame"/> instances.
/// </summary>
public static class ProtocolHelper {

    private static readonly Int16Key KEY_ROUTING_KEY = new(-1);
    private static readonly Int16Key KEY_RESPONSE_KEY = new(-2);

    /// <summary>
    /// Retrieves, from the given <paramref name="frame"/>, the next routing key to use to route it.
    /// </summary>
    /// <param name="frame">The frame to get the routing key from.</param>
    /// <param name="routingKey">The retrieved routing key if found.</param>
    /// <returns><see langword="true"/> if the <paramref name="routingKey"/> was found, <see langword="false"/> otherwise.</returns>
    public static bool TryGetRoutingKey(MultiFrame frame, [NotNullWhen(true)]out Int32Key? routingKey) {
        if (!frame.TryGet(KEY_ROUTING_KEY, out Frame? subFrame)) return UtilityFunctions.DefaultAndFalse(out routingKey);
        routingKey = new(subFrame.Rotate());
        return true;
    }

    /// <summary>
    /// Retrieves, from the given <paramref name="frame"/>, the response key to use to complete an RPC call with it.
    /// </summary>
    /// <param name="frame">The frame to get the response key from.</param>
    /// <param name="responseKey">The retrieved response key if found.</param>
    /// <returns><see langword="true"/> if the <paramref name="responseKey"/> was found, <see langword="false"/> otherwise.</returns>
    public static bool TryGetResponseKey(MultiFrame frame, [NotNullWhen(true)]out BasicKey? responseKey) {
        if (!frame.TryGet(KEY_RESPONSE_KEY, out Frame? subFrame)) return UtilityFunctions.DefaultAndFalse(out responseKey);
        responseKey = new(subFrame.Get(0));
        return true;
    }

    /// <summary>
    /// Writes the given <paramref name="key"/> into the given <paramref name="frame"/> as a response key to use when completing 
    /// an RPC call with it.
    /// </summary>
    /// <param name="frame">The frame to write the <paramref name="key"/> into.</param>
    /// <param name="key">The key to write into the <paramref name="frame"/>.</param>
    public static void SetResponseKey(MultiFrame frame, BasicKey key) {
        if (!frame.TryGet(KEY_RESPONSE_KEY, out Frame? subFrame))
            frame.TryCreate(KEY_RESPONSE_KEY, out subFrame);

        if (subFrame.Length < 1) subFrame.Insert(key.Memory, 0);
        else subFrame.Swap(0, key.Memory);
    }
}
