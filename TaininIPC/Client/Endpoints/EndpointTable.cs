using TaininIPC.Client.Abstract;
using TaininIPC.CritBitTree.Keys;

namespace TaininIPC.Client.Endpoints;

/// <summary>
/// Represents a table of <see cref="Int32Key"/> keys mapped to <see cref="EndpointTableEntry"/> entries.
/// </summary>
public sealed class EndpointTable : AbstractTable<EndpointTableEntryOptions, EndpointTableEntry> {
    /// <summary>
    /// Initializes a new <see cref="EndpointTable"/> with the specified number of reserved ids.
    /// </summary>
    /// <param name="reservedCount">The number of reserved ids.</param>
    public EndpointTable(int reservedCount) : base(reservedCount) { }

    /// <summary>
    /// Initializes a new <see cref="EndpointTableEntry"/> from the given <paramref name="options"/> and adds it to the table.
    /// </summary>
    /// <param name="key">The keyt to map to the added <see cref="EndpointTableEntry"/>.</param>
    /// <param name="options">The options for constructing the new <see cref="EndpointTableEntry"/>.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    protected override async Task AddInternal(Int32Key key, EndpointTableEntryOptions options) =>
        await AddInternalBase(key, new(key, this, options)).ConfigureAwait(false);
}
