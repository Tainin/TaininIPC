using TaininIPC.Client.Abstract;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client.Routing;

/// <summary>
/// An <see cref="INameMappedTable{TInput, TStored}"/> wrapper around a <see cref="RoutingTable"/>.
/// </summary>
public sealed class NameMappedRoutingTable : AbstractNameMappedTable<RoutingTable, IRouter, IRouter>, IRouter {

    /// <summary>
    /// Initializes a new <see cref="NameMappedRoutingTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public NameMappedRoutingTable(int reservedCount) : base(new(reservedCount)) { }

    /// <inheritdoc cref="IRouter.RouteFrame(MultiFrame, EndpointTableEntry)"/>
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry? origin) => RoutingTable.RouteFrame(this, frame, origin);
}