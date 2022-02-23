using TaininIPC.Data.Protocol;

namespace TaininIPC.Network.Interface;

public interface INetworkEndpoint : IListener {
    public event EventHandler<EndpointStatusChangedEventArgs> EndpointStatusChanged;
    public EndpointStatus Status { get; }
    public Task SendChunk(NetworkChunk chunk);
}
