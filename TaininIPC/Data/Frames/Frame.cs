﻿namespace TaininIPC.Data.Frames;

/// <summary>
/// Represents a doubly linked list of buffers for data serialization, 
/// which, compared to a single large buffer allows variable sized data and partial deserialization and updates.
/// </summary>
public sealed class Frame {

    /// <summary>
    /// Represents a single buffer in a <see cref="Frame"/>.
    /// </summary>
    private sealed class Node { 
        public Node? Previous { get; set; }
        public Node? Next { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
        public Node(ReadOnlyMemory<byte> data) => Data = data;
    }

    // The endpoints of the list. Neither will ever contain data.
    private readonly Node preStart;
    private readonly Node postEnd;

    /// <summary>
    /// Gets the number of buffers in the <see cref="Frame"/>.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Initializes an empty <see cref="Frame"/>
    /// </summary>
    public Frame() {
        preStart = new(ReadOnlyMemory<byte>.Empty);
        postEnd = new(ReadOnlyMemory<byte>.Empty);
        (preStart.Next, postEnd.Previous) = (postEnd, preStart);
        Length = 0;
    }
    /// <summary>
    /// Initializes a <see cref="Frame"/> with the provided buffers.
    /// </summary>
    /// <param name="data">An <see cref="IEnumerable{T}"/> over the buffers to include in the new frame.</param>
    public Frame(IEnumerable<ReadOnlyMemory<byte>> data) : this() {
        foreach (ReadOnlyMemory<byte> buffer in data) Insert(buffer, ^1);
    }

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> over all the buffers in the <see cref="Frame"/>.
    /// </summary>
    public IEnumerable<ReadOnlyMemory<byte>> AllBuffers {
        get {
            Node curr = preStart.Next!;
            while (!ReferenceEquals(curr, postEnd)) {
                yield return curr.Data;
                curr = curr.Next!;
            }
        }
    }

    /// <summary>
    /// Inserts the specified buffer into the <see cref="Frame"/> at the specified index.
    /// </summary>
    /// <param name="data">The buffer to insert.</param>
    /// <param name="index">The index to insert the buffer at.</param>
    /// <exception cref="IndexOutOfRangeException">If the provided index describes a location outside the range
    /// of the <see cref="Frame"/></exception>
    public void Insert(ReadOnlyMemory<byte> data, Index index) {
        // Initialize the new node
        Node newNode = new(data);

        // Get the node to start at and the traversal function based on the direction of the index
        (Node prev, Func<Node, Node?> traverse) = index.IsFromEnd ?
            (postEnd, (Func<Node, Node?>)(node => node.Previous)) :
            (preStart, node => node.Next);

        // Traverse the prev node to the position before the insert location
        int limit = index.Value;
        for (int i = 0; i < limit; i++)
            prev = traverse(prev) ?? 
                throw new IndexOutOfRangeException($"Attempted to insert outside the range of the {nameof(Frame)}");

        // Get the node after the insert location
        Node next = prev.Next ??
            throw new IndexOutOfRangeException($"Attempted to insert outside the range of the {nameof(Frame)}");

        // Update the length
        Length++;

        // Wire the new node into the linked list structure
        (newNode.Next, newNode.Previous) = (next, prev);
        next.Previous = newNode;
        prev.Next = newNode;
    }
    /// <summary>
    /// Gets the buffer at the specified index in the <see cref="Frame"/>.
    /// </summary>
    /// <param name="index">The index of the buffer to get.</param>
    /// <returns>The buffer at the specifed index.</returns>
    public ReadOnlyMemory<byte> Get(Index index) => Find(index, remove: false).Data;
    /// <summary>
    /// Gets the buffer at the specified index in the <see cref="Frame"/> and removes it.
    /// </summary>
    /// <param name="index">The index of the buffer to get and remove.</param>
    /// <returns>The buffer which was at the specifed index.</returns>
    public ReadOnlyMemory<byte> Pop(Index index) => Find(index, remove: true).Data;
    /// <summary>
    /// Gets the buffer at the specified index in the <see cref="Frame"/> and replaces it with the provided one.
    /// </summary>
    /// <param name="index">The index of the buffer to get and replace.</param>
    /// <param name="data">Replaces the buffer at the specified index.</param>
    /// <returns>The buffer which was at the specifed index.</returns>
    public ReadOnlyMemory<byte> Swap(Index index, ReadOnlyMemory<byte> data) { 
        Node node = Find(index, remove: false);
        ReadOnlyMemory<byte> oldData = node.Data;

        node.Data = data;
        return oldData;
    }
    /// <summary>
    /// Gets the buffer at the first index of the <see cref="Frame"/> and moves it to the last.
    /// </summary>
    /// <returns>The buffer which was at the first index.</returns>
    public ReadOnlyMemory<byte> Rotate() {
        ReadOnlyMemory<byte> data = Pop(0);
        Insert(data, ^1);
        return data;
    }
    /// <summary>
    /// Removes the buffer at the specifed index in the <see cref="Frame"/>.
    /// </summary>
    /// <param name="index">The index of the buffer to remove.</param>
    public void Remove(Index index) => Find(index, remove: true);
    /// <summary>
    /// Removes all buffers from the <see cref="Frame"/>.
    /// </summary>
    public void Clear() => (preStart.Next, postEnd.Previous, Length) = (postEnd, preStart, 0);
    /// <summary>
    /// Checks whether the <see cref="Frame"/> is empty or not.
    /// </summary>
    /// <returns><see langword="true"/> if there are no buffers in the <see cref="Frame"/>, <see langword="false"/> otherwise.</returns>
    public bool IsEmpty() => ReferenceEquals(preStart.Next, postEnd) || ReferenceEquals(postEnd.Previous, preStart);

    /// <summary>
    /// Helper method to find the node at a given index in the <see cref="Frame"/> and possibly remove it.
    /// </summary>
    /// <param name="index">The index of the node to find.</param>
    /// <param name="remove">A flag indicating that the found node should be removed.</param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">If the provided index describes a location outside the range
    /// of the <see cref="Frame"/></exception>
    private Node Find(Index index, bool remove) {
        // Get the node to start at and the traversal function based on the direction of the index
        (Node curr, Func<Node, Node?> traverse) = index.IsFromEnd ?
            (postEnd, (Func<Node, Node?>)(node => node.Previous)) :
            (preStart.Next!, node => node.Next);

        // Traverse to the node specified by the index
        int limit = index.Value;
        for (int i = 0; i < limit; i++)
            curr = traverse(curr) ??
                throw new IndexOutOfRangeException($"Attempted to find a node outside the range of the {nameof(Frame)}");

        // If the Next or Previous node is null then curr is preStart or postEnd which cannot be removed
        if (curr.Next is null || curr.Previous is null)
            throw new IndexOutOfRangeException($"Attempted to find a node outside the range of the {nameof(Frame)}");

        // If the remove flag is not set return early
        if (!remove) return curr;

        // Update the length
        Length--;

        // Unlink the current node from the linked list structure.
        (curr.Next.Previous, curr.Previous.Next) = (curr.Previous, curr.Next);
        (curr.Next, curr.Previous) = (null, null);

        // Return the found node.
        return curr;
    }
}