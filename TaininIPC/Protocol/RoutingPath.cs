using System.Diagnostics.CodeAnalysis;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Utils;

namespace TaininIPC.Protocol;

/// <summary>
/// Extension methods for accessing the routing paths of a <see cref="MultiFrame"/>.
/// </summary>
public static class RoutingPath {
    /// <summary>
    /// Attempts to get the next routing key from the routing path of the specified <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">The frame to get the routing key from.</param>
    /// <param name="routingKey">The next routing key of the <paramref name="frame"/> if it contained a routing path.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="frame"/> contained a routing path, <see langword="false"/> otherwise.</returns>
    public static bool TryGetNextRoutingKey(this MultiFrame frame, [NotNullWhen(true)] out Int32Key? routingKey) {
        if (!frame.TryGet(ProtocolMultiFrameKeys.ROUTING_PATH_KEY, out Frame? subFrame)) 
            return UtilityFunctions.DefaultAndFalse(out routingKey);
        routingKey = new(subFrame.Rotate());
        return true;
    }
}
