using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;
using TaininIPC.Network;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client.Endpoints;

public sealed class EndpointTableEntry { 
    public INetworkEndpoint NetworkEndpoint { get; private init; }
    public FrameEndpoint FrameEndpoint { get; private init; }
    public EndpointTable EndpointTable { get; private init; }
    public IRouter Router { get; private init; }
    public Int32Key Key { get; private init; }

    public EndpointTableEntry(Int32Key key, EndpointTable endpointTable, EndpointTableEntryOptions options) {
        NetworkEndpoint = options.NetworkFactory(HandleIncomingChunk);
        FrameEndpoint = new(HandleOutgoingChunk, HandleIncomingFrame);
        EndpointTable = endpointTable;
        Router = options.Router;
        Key = key;
    }

    private Task HandleOutgoingChunk(NetworkChunk chunk) => NetworkEndpoint.SendChunk(chunk);
    private Task HandleIncomingChunk(NetworkChunk chunk) => FrameEndpoint.ApplyChunk(chunk);
    private Task HandleIncomingFrame(MultiFrame multiFrame) => Router.RouteFrame(multiFrame, this);
}
