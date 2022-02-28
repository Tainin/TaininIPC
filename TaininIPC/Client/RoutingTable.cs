﻿using TaininIPC.Client.Abstract;
using TaininIPC.Client.Interface;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client;

public sealed class RoutingTable : AbstractTable<IRouter, IRouter>,  IRouter {

    public delegate ReadOnlyMemory<byte> KeyExtractor(MultiFrame frame);

    private readonly KeyExtractor keyExtractor;

    public RoutingTable(int reservedCount, KeyExtractor keyExtractor) : base(reservedCount) => this.keyExtractor = keyExtractor;
    protected override Task<int> AddInternal(IRouter input, int id) => AddInternalBase(input, id);
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry origin) {
        (IRouter? router, bool got) = await GetInternal(keyExtractor(frame)).ConfigureAwait(false);
        if (got && router is not null) await router.RouteFrame(frame, origin).ConfigureAwait(false);
    }
}
