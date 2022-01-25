using System.Runtime.InteropServices;
using TaininIPC.Utils;

namespace TaininIPC.Data.CritBitTree;

public sealed class CritBitTree<T> {
    private interface INode { }

    [StructLayout(LayoutKind.Auto, Pack = 1)]
    private sealed class InternalNode : INode {
        public INode Left { get; set; }
        public INode Right { get; set; }

        public byte Index { get; }
        public byte Mask { get; }

        public InternalNode(INode left, INode right, byte index, byte mask) {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            Index = index;
            Mask = mask;
        }
    }
    [StructLayout(LayoutKind.Auto, Pack = 1)]
    private sealed class LeafNode : INode {
        public ReadOnlyMemory<byte> Key { get; }
        public T Value { get; set; }

        public LeafNode(ReadOnlyMemory<byte> key, T value) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Key = key;
        }
    }

    private INode? root;

    public CritBitTree() {

    }

    public bool TryGetValue(ReadOnlySpan<byte> key, out T? value) {
        if (root is null) return UtilityFunctions.DefaultAndFalse(out value);

        LeafNode leafNode = FindClosestMatch(key);

        if (!key.SequenceEqual(leafNode.Key.Span)) 
            return UtilityFunctions.DefaultAndFalse(out value);
        value = leafNode.Value;
        return true;
    }
    public bool ContainsKey(ReadOnlySpan<byte> key) {
        if (root == null) return false;
        LeafNode leafNode = FindClosestMatch(key);
        return key.SequenceEqual(leafNode.Key.Span);
    }
    public bool Add(ReadOnlyMemory<byte> key, T value) {
        if (root is null) {
            root = new LeafNode(key, value);
            return true;
        }
        ReadOnlySpan<byte> keySpan = key.Span;
        int keyLength = keySpan.Length;

        LeafNode leafNode = FindClosestMatch(keySpan);
        ReadOnlySpan<byte> leafKeySpan = leafNode.Key.Span;
        int leafKeyLength = leafKeySpan.Length;

        int branchIndex;
        uint newMask = 0;
        bool foundBranchPoint = false;

        for (branchIndex = 0; branchIndex < keyLength; branchIndex++) {
            if (branchIndex >= leafKeyLength) {
                newMask = keySpan[branchIndex];
                foundBranchPoint = true;
                break;
            }

            if (leafKeySpan[branchIndex] != keySpan[branchIndex]) {
                newMask = (uint) (leafKeySpan[branchIndex] ^ keySpan[branchIndex]);
                foundBranchPoint = true;
                break;
            }
        }

        if (!foundBranchPoint) return false;

        newMask |= newMask >> 1;
        newMask |= newMask >> 2;
        newMask |= newMask >> 4;
        newMask = (newMask & ~(newMask >> 1)) ^ 0xFF;

        byte criticalByte = branchIndex < leafKeyLength ? leafKeySpan[branchIndex] : (byte)0;
        uint newDirection = (1 + (newMask | criticalByte)) >> 8;

        INode curr = root;
        InternalNode? parent = null;
        int parentDirection = -1;

        while (curr is InternalNode currInternal) {
            if (currInternal.Index > branchIndex) break;
            if (currInternal.Index == branchIndex && currInternal.Mask > newMask) break;

            criticalByte = currInternal.Index < keyLength ? keySpan[currInternal.Index] : (byte)0;

            int direction = (1 + (currInternal.Mask | criticalByte)) >> 8;
            parent = currInternal;
            parentDirection = direction;
            curr = direction == 0 ? currInternal.Left : currInternal.Right;
        }
        LeafNode newLeaf = new(key, value);

        (INode left, INode right) = newDirection == 0 ? (curr, (INode)newLeaf) : (newLeaf, curr);
        InternalNode newNode = new(left, right, (byte)branchIndex, (byte)newMask);

        if (parent is null) {
            root = newNode;
            return true;
        }

        if (parentDirection == 0) parent.Left = newNode;
        else parent.Right = newNode;
        return true;
    }
    public bool Update(ReadOnlySpan<byte> key, T newValue) {
        if (root is null) return false;

        LeafNode leafNode = FindClosestMatch(key);
        if (!key.SequenceEqual(leafNode.Key.Span)) return false;

        leafNode.Value = newValue;
        return true;
    }
    public bool Pop(ReadOnlySpan<byte> key, out T? value) {
        if (root is null) return UtilityFunctions.DefaultAndFalse(out value);

        int keyLength = key.Length;
        (INode curr, INode? parent, INode? grandParent) = (root, null, null);
        (int parentDirection, int grandParentDirection) = (-1, -1);

        while (curr is InternalNode currInternal) {
            byte criticalByte = currInternal.Index < keyLength ? key[currInternal.Index] : (byte)0;
            int direction = (1 + (currInternal.Mask | criticalByte)) >> 8;

            (grandParent, parent) = (parent, curr);
            (grandParentDirection, parentDirection) = (parentDirection, direction);

            curr = direction == 0 ? currInternal.Left : currInternal.Right;
        }

        LeafNode leafNode = (LeafNode)curr;
        if (!key.SequenceEqual(leafNode.Key.Span))
            return UtilityFunctions.DefaultAndFalse(out value);

        value = leafNode.Value;

        if (parent is null) {
            root = null;
            return true;
        }

        INode sibling = parentDirection == 0 ? ((InternalNode)parent).Right : ((InternalNode)parent).Left;

        if (grandParent is null) {
            root = sibling;
            return true;
        }

        if (grandParentDirection == 0) ((InternalNode)grandParent).Left = sibling;
        else ((InternalNode)grandParent).Right = sibling;

        return true;
    }
    public bool Remove(ReadOnlySpan<byte> key) => Pop(key, out _);
    public void Clear() => root = null;

    private LeafNode FindClosestMatch(ReadOnlySpan<byte> key) {
        INode curr = root!;
        int keyLength = key.Length;

        while (curr is InternalNode currInternal) {
            byte criticalByte = currInternal.Index < keyLength ? key[currInternal.Index] : (byte)0;
            int direction = (1 + (currInternal.Mask | criticalByte)) >> 8;
            curr = direction == 0 ? currInternal.Left : currInternal.Right;
        }

        return (LeafNode)curr;
    }

    //TODO: REMOVE THIS
    public void PrintAll() {
        static string KeyToString(ReadOnlyMemory<byte> key) =>
            string.Join(' ', Enumerable.Range(0, key.Length).Select(i => Convert.ToString(key.Span[i], 2).PadLeft(8, '0')));



        if (root is null) {
            Console.Write("[Tree is empty]");
            return;
        }

        Stack<(INode node, int indent)> traversal = new();
        traversal.Push((root, 0));

        while (traversal.Count > 0) {
            (INode node, int indent) = traversal.Pop();

            Console.Write(string.Join("", Enumerable.Repeat("|  ", indent)));

            if (node is LeafNode leafNode) {
                Console.Write(KeyToString(leafNode.Key));
                Console.Write(' ');
                Console.WriteLine(leafNode.Value);
            }

            if (node is InternalNode internalNode) {
                Console.Write('[');
                Console.Write($"I: {internalNode.Index}");
                Console.Write(' ');
                Console.Write($"M: {Convert.ToString(internalNode.Mask, 2).PadLeft(8, '0')}");
                Console.WriteLine(']');

                if (internalNode.Right is not null) traversal.Push((internalNode.Right, indent + 1));
                if (internalNode.Left is not null) traversal.Push((internalNode.Left, indent + 1));
            }
        }
    }
}