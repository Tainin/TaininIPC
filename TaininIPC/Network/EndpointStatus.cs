namespace TaininIPC.Network;

/// <summary>
/// Specifies the status of a network endpoint.
/// </summary>
public enum EndpointStatus : int {
    /// <summary>
    /// The endpoint has not been started yet.
    /// </summary>
    Unstarted = 0,
    /// <summary>
    /// The endpoint is in the process of starting.
    /// </summary>
    Starting = 1,
    /// <summary>
    /// The endpoint is currently running.
    /// </summary>
    Running = 2,
    /// <summary>
    /// The endpoint has been stopped successfully.
    /// </summary>
    Stopped = 3,
    /// <summary>
    /// The endpoint has faulted and is no longer running.
    /// </summary>
    Faulted = 4,
}
