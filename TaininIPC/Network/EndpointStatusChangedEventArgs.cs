namespace TaininIPC.Network;

public sealed class EndpointStatusChangedEventArgs : EventArgs {
    public EndpointStatus OldStatus { get; }
    public EndpointStatus NewStatus { get; }

    public EndpointStatusChangedEventArgs(EndpointStatus oldStatus, EndpointStatus newStatus) {
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
