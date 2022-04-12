using TaininIPC.Client.Routing.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Client.Routing.Endpoints;

/// <summary>
/// Represents a table of <see cref="Int32Key"/> keys mapped to <see cref="EndpointTableEntry"/> entries.
/// </summary>
public sealed class EndpointTable : Table<EndpointTableEntry>, IRouter {
    private class AddHandle : ITableAddHandle<EndpointTableEntry> {

        private readonly ITableAddHandle<EndpointTableEntry> innerAddHandle;

        public AddHandle(ITableAddHandle<EndpointTableEntry> innerAddHandle) => this.innerAddHandle = innerAddHandle;

        public Int32Key Key => innerAddHandle.Key;

        public Task Add(Func<Int32Key, EndpointTableEntry> entryFactory) => Add(entryFactory(Key));
        public async Task Add(EndpointTableEntry entry) {
            await innerAddHandle.Add(entry).ConfigureAwait(false);
            entry.StartEndpoint();
        }
    }

    /// <summary>
    /// Initializes a new <see cref="EndpointTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public EndpointTable(int reservedCount) : base(reservedCount) { }

    /// <inheritdoc cref="Table{T}.GetAddHandle()"/>
    /// <remarks>The <see cref="EndpointTableEntry"/> added using the returned add handle will be started.</remarks>
    public sealed override ITableAddHandle<EndpointTableEntry> GetAddHandle() => 
        new AddHandle(base.GetAddHandle());
    /// <inheritdoc cref="Table{T}.GetAddHandle(Int32Key)"/>
    /// <remarks>The <see cref="EndpointTableEntry"/> added using the returned add handle will be started.</remarks>
    public sealed override ITableAddHandle<EndpointTableEntry> GetAddHandle(Int32Key reservedKey) => 
        new AddHandle(base.GetAddHandle(reservedKey));
    /// <inheritdoc cref="Table{T}.TryPop(Int32Key)"/>
    /// <remarks>The <see cref="EndpointTableEntry.NetworkEndpoint"/> of the removed entry will be stopped.</remarks>
    public sealed override async Task<Attempt<EndpointTableEntry>> TryPop(Int32Key key) {
        Attempt<EndpointTableEntry> attempt = await base.TryPop(key).ConfigureAwait(false);

        if (attempt.TryResult(out EndpointTableEntry? entry))
            entry.NetworkEndpoint.Stop();

        return attempt;
    }

    /// <summary>
    /// Routes the specified <paramref name="frame"/> by sending it through the endpoint specified by it's next routing key.
    /// </summary>
    /// <param name="frame">The frame to route.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {

        if (!frame.TryGetNextRoutingKey(out Int32Key? routingKey)) return;

        Attempt<EndpointTableEntry> attempt = await TryGet(routingKey).ConfigureAwait(false);
        if (attempt.TryResult(out EndpointTableEntry? entry))
            await entry.NetworkEndpoint.SendMultiFrame(frame).ConfigureAwait(false);
    }
}
