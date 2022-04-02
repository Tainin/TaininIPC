using TaininIPC.CritBitTree.Keys;

namespace TaininIPC.Protocol;

/// <summary>
/// Contains routing keys which have a known static value.
/// </summary>
public static class StaticRoutingKeys {
    /// <summary>
    /// The key which routes frames to a node's endpoint table.
    /// </summary>
    public static Int32Key ROUTE_TO_ENDPOINT_TABLE_KEY { get; } = new(-1);
    /// <summary>
    /// The key which routes frames to a node's routing table.
    /// </summary>
    public static Int32Key ROUTE_TO_ROUTING_TABLE_KEY { get; } = new(-2);
}
