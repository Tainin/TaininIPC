using TaininIPC.Client.Connections;
using TaininIPC.Client.Connections.Interface;
using TaininIPC.Client.Routing;
using TaininIPC.Client.Routing.Endpoints;
using TaininIPC.Protocol;

namespace TaininIPC.Client;

/// <summary>
/// Represents a node in an inter-process connection graph.
/// </summary>
public sealed class Node : IConnectionHandler {

    private static readonly int ENDPOINT_TABLE_RESERVED_COUNT = 10;
    private static readonly int ROUTING_TABLE_RESERVED_COUNT = 10;
    private static readonly int CONNECTION_SOURCE_TABLE_RESERVED_COUNT = 10;

    private readonly EndpointTable endpointTable;
    private readonly RoutingTable routingTable;
    private readonly ConnectionSourceTable connectionSourceTable;

    private readonly NameMap endpointNameMap;
    private readonly NameMap routingNameMap;

    private Node() {
        endpointTable = new(ENDPOINT_TABLE_RESERVED_COUNT);
        routingTable = new(ROUTING_TABLE_RESERVED_COUNT);

        connectionSourceTable = new(CONNECTION_SOURCE_TABLE_RESERVED_COUNT);

        (endpointNameMap, routingNameMap) = (new(), new());
    }

    private async Task InitializeRoutes() {
        await routingTable.GetAddHandle(StaticRoutingKeys.ENDPOINT_TABLE_ROUTE_KEY)
            .Add(_ => endpointTable).ConfigureAwait(false);
        await routingTable.GetAddHandle(StaticRoutingKeys.CONNECTION_SOURCE_TABLE_ROUTE_KEY)
            .Add(_ => connectionSourceTable).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IConnectionHandler.HandleConnection(IConnection)"/>
    public async Task HandleConnection(IConnection connection) =>
        await endpointTable.GetAddHandle().Add(key => {
            EndpointTableEntryOptions entryOptions = new(connection, routingTable);
            return new EndpointTableEntry(key, entryOptions);
        });
}
