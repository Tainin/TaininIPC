namespace TaininIPC.Network.Sockets;

/// <summary>
/// Record used to store the <see cref="TimeSpan"/> instances used to configure the keep alive service of <see cref="SocketNetworkEndpoint"/>
/// </summary>
/// <param name="Period">The interval at which the <see cref="SocketNetworkEndpoint"/> sends a keep alive chunk.</param>
/// <param name="Timeout">The maximum time between keep alive chunks the <see cref="SocketNetworkEndpoint"/> will stay alive for.</param>
public record TimeoutOptions(TimeSpan Period, TimeSpan Timeout);