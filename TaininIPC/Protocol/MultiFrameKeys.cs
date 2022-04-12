using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;

namespace TaininIPC.Protocol;

/// <summary>
/// Contains <see cref="MultiFrame"/> keys for accessing protocol related data.
/// </summary>
public static class MultiFrameKeys {
    /// <summary>
    /// The key which maps to the routing path of a <see cref="MultiFrame"/>.
    /// </summary>
    public static Int16Key ROUTING_PATH_KEY { get; } = new(-1);
    /// <summary>
    /// The key which maps to the return routing path of a <see cref="MultiFrame"/>.
    /// </summary>
    public static Int16Key RETURN_ROUTING_PATH_KEY { get; } = new(-2);
    /// <summary>
    /// The key which maps to the name path of a <see cref="MultiFrame"/>.
    /// </summary>
    public static Int16Key NAME_PATH_KEY { get; } = new(-3);
    /// <summary>
    /// The key which maps to the response identifier of a <see cref="MultiFrame"/>.
    /// </summary>
    public static Int16Key RESPONSE_IDENTIFIER_KEY { get; } = new(-4);
    /// <summary>
    /// The key which maps to the connection info stored in a <see cref="MultiFrame"/>.
    /// </summary>
    public static Int16Key CONNECTION_INFO_KEY { get; } = new(-5);
}
