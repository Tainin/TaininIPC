using TaininIPC.Client.Endpoints;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client.Interface;

public interface IRouter {
    public Task RouteFrame(MultiFrame frame, EndpointTableEntry origin);
}
