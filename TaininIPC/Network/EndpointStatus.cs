namespace TaininIPC.Network;

using TaininIPC.Network.Interface;

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
