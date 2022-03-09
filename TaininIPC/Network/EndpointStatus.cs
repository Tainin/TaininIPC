using TaininIPC.Network.Interface;

namespace TaininIPC.Network;

/// <summary>
/// Specifies the status of an <see cref="INetworkEndpoint"/>
/// </summary>
public enum EndpointStatus : int {
    Unstarted = 0,
    Starting = 1,
    Running = 2,
    Stopped = 3,
    Faulted = 4,
}
