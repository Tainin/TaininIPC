using TaininIPC.CritBitTree.Keys;

namespace TaininIPC.Protocol;

/// <summary>
/// Contains routing keys which have a known static value.
/// </summary>
public static class StaticRoutingKeys {
    /// <summary>
    /// The key which routes frames to a node's endpoint table.
    /// </summary>
    public static Int32Key ENDPOINT_TABLE_ROUTE_KEY { get; } = new(-1);
    /// <summary>
    /// The key which routes frames to a node's connection source table.
    /// </summary>
    public static Int32Key CONNECTION_SOURCE_TABLE_ROUTE_KEY { get; } = new(-2);
}
