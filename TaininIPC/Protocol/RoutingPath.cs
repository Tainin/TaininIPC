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
        if (!frame.TryGet(MultiFrameKeys.ROUTING_PATH_KEY, out Frame? subFrame))
            return UtilityFunctions.DefaultAndFalse(out routingKey);
        routingKey = new(subFrame.Pop(0));
        return true;
    }
    /// <summary>
    /// If the specified <paramref name="frame"/> contains a return path,
    /// prepends the specified <paramref name="keys"/> array to it, otherwise no-op.
    /// </summary>
    /// <param name="frame">The frame to prepend to.</param>
    /// <param name="keys">The routing keys to prepend to the <paramref name="frame"/>'s return path.</param>
    /// <remarks>
    /// The <paramref name="keys"/> array is prepended one at a time in reverse order such that the first key in the array
    /// becomes the new first key of the return path.
    /// </remarks>
    public static void PrependReturnPathIfPresent(this MultiFrame frame, params Int32Key[] keys) {
        if (!frame.TryGet(MultiFrameKeys.RETURN_ROUTING_PATH_KEY, out Frame? subFrame)) return;

        for (int i = keys.Length - 1; i >= 0; i--)
            subFrame.Prepend(keys[i].Memory);
    }
    /// <summary>
    /// If the specified <paramref name="frame"/> contains a routing path
    /// appends the specified <paramref name="keys"/> array to it, otherwise no-op.
    /// </summary>
    /// <param name="frame">The frame to append to.</param>
    /// <param name="keys">The routing keys to append to the <paramref name="frame"/>'s routing path.</param>
    public static void AppendRoutingPathIfPresent(this MultiFrame frame, params Int32Key[] keys) {
        if (!frame.TryGet(MultiFrameKeys.ROUTING_PATH_KEY, out Frame? subFrame)) return;

        int length = keys.Length;
        for (int i = 0; i < length; i++)
            subFrame.Append(keys[i].Memory);
    }
}
