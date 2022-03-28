using TaininIPC.Network.Abstract;

namespace TaininIPC.Network;

/// <summary>
/// Provides data for the <see cref="AbstractNetworkEndpoint.EndpointStatusChanged"/> event.
/// </summary>
public sealed class EndpointStatusChangedEventArgs : EventArgs {
    /// <summary>
    /// Gets the status that the endpoint was in prior to the change which triggered the event.
    /// </summary>
    public EndpointStatus OldStatus { get; }
    /// <summary>
    /// Gets the status that the endpoint is in after the event which triggered the event.
    /// </summary>
    public EndpointStatus NewStatus { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointStatusChangedEventArgs"/> 
    /// class from the provided <see cref="EndpointStatus"/> instances.
    /// </summary>
    /// <param name="oldStatus">The status prior to the event.</param>
    /// <param name="newStatus">The status after the event.</param>
    public EndpointStatusChangedEventArgs(EndpointStatus oldStatus, EndpointStatus newStatus) {
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
