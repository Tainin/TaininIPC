namespace TaininIPC.Network;

public enum EndpointStatus : int {
    Unstarted = 0,
    Starting = 1,
    Running = 2,
    Stopped = 3,
    Faulted = 4,
}
