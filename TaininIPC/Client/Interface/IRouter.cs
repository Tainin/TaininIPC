using TaininIPC.Client.Endpoints;
using TaininIPC.Data.Frames;

namespace TaininIPC.Client.Interface;

/// <summary>
/// Provides a mechanism for routing <see cref="MultiFrame"/> instances to a handler.
/// </summary>
public interface IRouter {
    /// <summary>
    /// Route the given <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">The <see cref="MultiFrame"/> to route.</param>
    /// <param name="origin">The endpoint which the <paramref name="frame"/> arived through or <see langword="null"/> if it originated locally.</param>
    /// <returns>An asyncronouse task representing the operation.</returns>
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry? origin);
}
