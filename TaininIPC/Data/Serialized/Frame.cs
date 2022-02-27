namespace TaininIPC.Data.Serialized;

public sealed class Frame {
    private sealed class Node { 
        public Node? Previous { get; set; }
        public Node? Next { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
        public Node(ReadOnlyMemory<byte> data) => Data = data;
    }

    private readonly Node preStart;
    private readonly Node postEnd;

    public Frame() {
        preStart = new(ReadOnlyMemory<byte>.Empty);
        postEnd = new(ReadOnlyMemory<byte>.Empty);
        (preStart.Next, postEnd.Previous) = (postEnd, preStart);
    }
    public IEnumerable<ReadOnlyMemory<byte>> Serialized {
        get {
            Node curr = preStart.Next!;
            while (!ReferenceEquals(curr, postEnd)) {
                yield return curr.Data;
                curr = curr.Next!;
            }
        }
    }
    public void Insert(ReadOnlyMemory<byte> data, Index index) {
        Node newNode = new(data);

        (Node prev, Func<Node, Node?> traverse) = index.IsFromEnd ?
            (postEnd, (Func<Node, Node?>)(node => node.Previous)) :
            (preStart, node => node.Next);

        int limit = index.Value;
        for (int i = 0; i < limit; i++)
            prev = traverse(prev) ?? throw new IndexOutOfRangeException();

        Node next = prev.Next ?? throw new IndexOutOfRangeException();

        (newNode.Next, newNode.Previous) = (next, prev);
        next.Previous = newNode;
        prev.Next = newNode;
    }
    public ReadOnlyMemory<byte> Get(Index index) => Find(index, remove: false).Data;
    public ReadOnlyMemory<byte> Pop(Index index) => Find(index, remove: true).Data;
    public ReadOnlyMemory<byte> Swap(Index index, ReadOnlyMemory<byte> data) { 
        Node node = Find(index, remove: false);
        ReadOnlyMemory<byte> oldData = node.Data;

        node.Data = data;
        return oldData;
    }
    public ReadOnlyMemory<byte> Rotate() {
        ReadOnlyMemory<byte> data = Pop(0);
        Insert(data, ^1);
        return data;
    }
    public void Remove(Index index) => Find(index, remove: true);
    public void Clear() => (preStart.Next, postEnd.Previous) = (postEnd, preStart);
    public bool IsEmpty() => ReferenceEquals(preStart.Next, postEnd) || ReferenceEquals(postEnd.Previous, preStart);

    private Node Find(Index index, bool remove) {
        (Node curr, Func<Node, Node?> traverse) = index.IsFromEnd ?
            (postEnd, (Func<Node, Node?>)(node => node.Previous)) :
            (preStart.Next!, node => node.Next);

        int limit = index.Value;
        for (int i = 0; i < limit; i++)
            curr = traverse(curr) ?? throw new IndexOutOfRangeException();

        if (curr.Next is null || curr.Previous is null)
            throw new IndexOutOfRangeException();

        if (!remove) return curr;

        (curr.Next.Previous, curr.Previous.Next) = (curr.Previous, curr.Next);
        (curr.Next, curr.Previous) = (null, null);

        return curr;
    }
}
