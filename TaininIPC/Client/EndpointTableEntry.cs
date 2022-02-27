using TaininIPC.Client.Interface;
using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;
using TaininIPC.Network;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client;

public sealed class EndpointTableEntry { 
    public INetworkEndpoint NetworkEndpoint { get; private init; }
    public FrameEndpoint FrameEndpoint { get; private init; }
    public EndpointTable EndpointTable { get; private init; }
    public IRouter Router { get; private init; }
    public long Id { get; private init; }

    public EndpointTableEntry(long id, EndpointTable endpointTable, EndpointTableEntryOptions options) {
        NetworkEndpoint = options.NetworkFactory(HandleIncomingChunk);
        FrameEndpoint = new(HandleOutgoingChunk, HandleIncomingFrame);
        EndpointTable = endpointTable;
        Router = options.Router;
        Id = id;
    }

    private Task HandleOutgoingChunk(NetworkChunk chunk) => NetworkEndpoint.SendChunk(chunk);
    private Task HandleIncomingChunk(NetworkChunk chunk) => FrameEndpoint.ApplyChunk(chunk);
    private Task HandleIncomingFrame(MultiFrame multiFrame) => Router.RouteFrame(multiFrame, this);
}
