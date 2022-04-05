using TaininIPC.Client.Connections.Interface;
using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;

namespace TaininIPC.Client.Routing.Endpoints;

/// <summary>
/// Represents a set of options for initializing <see cref="EndpointTableEntry"/> instances.
/// </summary>
/// <param name="Connection">The connection to create the endpoint from.</param>
/// <param name="Router">The <see cref="IRouter"/> the entry should use to route received <see cref="MultiFrame"/> instances.</param>
public record EndpointTableEntryOptions(IConnection Connection, IRouter Router);