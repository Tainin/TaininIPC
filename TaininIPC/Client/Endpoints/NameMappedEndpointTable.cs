using TaininIPC.Client.Abstract;

namespace TaininIPC.Client.Endpoints;

/// <summary>
/// A name mapped version of <see cref="EndpointTable"/>.
/// </summary>
public sealed class NameMappedEndpointTable : AbstractNameMappedTable<EndpointTable, EndpointTableEntryOptions, EndpointTableEntry> {
    /// <summary>
    /// Initialies a new <see cref="NameMappedEndpointTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of ids to reserve.</param>
    public NameMappedEndpointTable(int reservedCount) : base(new(reservedCount)) { }
}