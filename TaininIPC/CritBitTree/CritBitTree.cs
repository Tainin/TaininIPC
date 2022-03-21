using System.Runtime.InteropServices;
using TaininIPC.CritBitTree.Interface;
using TaininIPC.Utils;

namespace TaininIPC.CritBitTree;

/// <summary>
/// Represents an associative array backed internally by a critical bit binary tree.
/// </summary>
/// <typeparam name="TKey">The type of key to use in the tree.</typeparam>
/// <typeparam name="TValue">The type of data to be stored in the tree.</typeparam>
public sealed class CritBitTree<TKey, TValue> : ICritBitTree<TKey, TValue> where TKey : ICritBitKey {
    /// <summary>
    /// Marker interface to give node types a common parent type.
    /// This is probably not a best practive however it does a good job of providing discriminated union
    /// like semantics and since all types are private nested members I'm allowing it.
    /// </summary>
    private interface INode { }

    /// <summary>
    /// Represents an internal (non-leaf) node of a <see cref="CritBitTree{T}"/>
    /// </summary>
    [StructLayout(LayoutKind.Auto, Pack = 1)]
    private sealed class InternalNode : INode {
        /// <summary>
        /// The left child of the node.
        /// </summary>
        public INode Left { get; set; }
        /// <summary>
        /// The right child of the node.
        /// </summary>
        public INode Right { get; set; }

        /// <summary>
        /// The critical index at which the keys of the children of the node differ.
        /// </summary>
        public byte Index { get; }
        /// <summary>
        /// The mask for the critical byte of the key.
        /// The bit which differs between the children's keys are unset -- all other bits are set.
        /// </summary>
        public byte Mask { get; }

        public InternalNode(INode left, INode right, byte index, byte mask) {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            Index = index;
            Mask = mask;
        }
    }

    /// <summary>
    /// Represents a leaf (value containing) node of a <see cref="CritBitTree{T}"/>
    /// </summary>
    [StructLayout(LayoutKind.Auto, Pack = 1)]
    private sealed class LeafNode : INode {
        /// <summary>
        /// The full key associated with the node.
        /// </summary>
        public TKey Key { get; }
        /// <summary>
        /// The value associated with the node.
        /// </summary>
        public TValue Value { get; set; }

        public LeafNode(TKey key, TValue value) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Key = key;
        }
    }

    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.Keys"/>
    public IEnumerable<TKey> Keys => GetLeafNodes(root).Select(t => t.Key);
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.Pairs"/>
    public IEnumerable<(TKey Key, TValue Value)> Pairs => GetLeafNodes(root).Select(t => (t.Key, t.Value));
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.Values"/>
    public IEnumerable<TValue> Values => GetLeafNodes(root).Select(t => t.Value);

    private INode? root;

    /// <summary>
    /// Initializes a new <see cref="CritBitTree{TKey, TValue}"/>
    /// </summary>
    public CritBitTree() { }

    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.Clear"/>
    public void Clear() => root = null;
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.ContainsKey(TKey)"/>
    public bool ContainsKey(TKey key) {
        if (root is null) return false;
        //get the closest match for the given key
        LeafNode leafNode = FindClosestMatch(key.Span, root);
        //if the keys match return true. false otherwise
        return key.Span.SequenceEqual(leafNode.Key.Span);
    }
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.TryAdd(TKey, TValue)"/>
    public bool TryAdd(TKey key, TValue value) {
        //if root is null insert the new key and value as root
        if (root is null) {
            root = new LeafNode(key, value);
            return true;
        }

        //initialize variables from the given key
        ReadOnlySpan<byte> keySpan = key.Span;
        int keyLength = keySpan.Length;

        //find closest match for the given key
        LeafNode leafNode = FindClosestMatch(keySpan, root);
        ReadOnlySpan<byte> leafKeySpan = leafNode.Key.Span;
        int leafKeyLength = leafKeySpan.Length;

        int branchIndex;
        uint newMask = 0;
        bool foundBranchPoint = false;

        //loop through the bytes of the given key
        for (branchIndex = 0; branchIndex < keyLength; branchIndex++) {
            //if the new key is longer than it's closest match the branch index is the first byte past the end of the leaf key
            if (branchIndex >= leafKeyLength) {
                newMask = keySpan[branchIndex];
                foundBranchPoint = true;
                break;
            }

            //otherwise the branch index that of the first byte which is not equal between the two keys
            if (leafKeySpan[branchIndex] != keySpan[branchIndex]) {
                newMask = (uint)(leafKeySpan[branchIndex] ^ keySpan[branchIndex]);
                foundBranchPoint = true;
                break;
            }
        }

        //if no branch point could be found the key is already present
        if (!foundBranchPoint) return false;

        //set all bits at and to the right of left most set bit
        newMask |= newMask >> 1;
        newMask |= newMask >> 2;
        newMask |= newMask >> 4;

        //unset left most set bit and set all others
        newMask = (newMask & ~(newMask >> 1)) ^ 0xFF;

        //get critical byte from key
        byte criticalByte = keySpan[branchIndex];
        //the direction of the new node can be extracted from the critical byte using the new nodes mask
        uint newDirection = (1 + (newMask | criticalByte)) >> 8;

        //initialize loop variables
        INode curr = root;
        InternalNode? parent = null;
        int parentDirection = -1;

        //loop until any of the following occur
        //  1. curr is a LeafNode
        //  2. currInternal.Index is greater than branchIndex
        //  3. currInternal.Index and branchIndex are equal but currInternal has a higher mask value 
        while (curr is InternalNode currInternal) {
            if (currInternal.Index > branchIndex) break;
            if (currInternal.Index == branchIndex && currInternal.Mask > newMask) break;

            //get the byte of the key at the index specified in the current node. 0 if index out of range.
            criticalByte = currInternal.Index < keyLength ? keySpan[currInternal.Index] : (byte)0;
            //the direction can be extracted from the critical byte using the mask of the current node.
            int direction = (1 + (currInternal.Mask | criticalByte)) >> 8;

            //shift parent and parentDirection down
            (parent, parentDirection) = (currInternal, direction);

            //move down the tree based on the direction
            curr = direction == 0 ? currInternal.Left : currInternal.Right;
        }

        //create new LeafNode with the specified key and value
        LeafNode newLeaf = new(key, value);

        //determine the new siblings positions based on newDirection
        (INode left, INode right) = newDirection == 0 ? ((INode)newLeaf, curr) : (curr, newLeaf);

        //create the critical parent for the new siblings
        InternalNode newNode = new(left, right, (byte)branchIndex, (byte)newMask);

        //if parent is null the new critical parent is the root
        if (parent is null) {
            root = newNode;
            return true;
        }

        //determine the critical parents position based on parentDirection
        if (parentDirection == 0) parent.Left = newNode;
        else parent.Right = newNode;
        return true;
    }
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.TryGet(TKey, out TValue?)"/>
    public bool TryGet(TKey key, out TValue? value) {
        if (root is null) return UtilityFunctions.DefaultAndFalse(out value);

        //get the closest match for the given key
        LeafNode leafNode = FindClosestMatch(key.Span, root);

        //if the keys don't match set the out value to default and return false
        if (!key.Span.SequenceEqual(leafNode.Key.Span))
            return UtilityFunctions.DefaultAndFalse(out value);
        value = leafNode.Value; //set the out value to the found node's value
        return true;
    }
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.TryPop(TKey, out TValue?)"/>
    public bool TryPop(TKey key, out TValue? value) {
        //if root is null the given key does not exist in the tree
        if (root is null) return UtilityFunctions.DefaultAndFalse(out value);


        //initialize variables
        ReadOnlySpan<byte> keySpan = key.Span;
        int keyLength = keySpan.Length;
        (INode curr, INode? parent, INode? grandParent) = (root, null, null);
        (int parentDirection, int grandParentDirection) = (-1, -1);

        while (curr is InternalNode currInternal) { //loop until leaf node is reached
            //get the byte of the key at the index specified in the current node. 0 if index out of range.
            byte criticalByte = currInternal.Index < keyLength ? keySpan[currInternal.Index] : (byte)0;
            //the direction can be extracted from the critical byte using the mask of the current node.
            int direction = (1 + (currInternal.Mask | criticalByte)) >> 8;

            //shift parent and grandparent down
            (grandParent, parent) = (parent, curr);
            (grandParentDirection, parentDirection) = (parentDirection, direction);

            //move down the tree based on the direction
            curr = direction == 0 ? currInternal.Left : currInternal.Right;
        }

        LeafNode leafNode = (LeafNode)curr; //get terminal node

        //return false if the passed key does not match the key that was passed in as there is no match anywhere in the tree
        if (!keySpan.SequenceEqual(leafNode.Key.Span))
            return UtilityFunctions.DefaultAndFalse(out value);

        value = leafNode.Value; //set out parameter

        //if parent is null there is only one element in the tree. Simply setting root to null will remove it.
        if (parent is null) {
            root = null;
            return true;
        }

        //get the sibling of curr
        INode sibling = parentDirection == 0 ? ((InternalNode)parent).Right : ((InternalNode)parent).Left;

        //if grandParent is null, delete curr by promoting it's sibling to root
        if (grandParent is null) {
            root = sibling;
            return true;
        }

        //delete curr by promoting it's sibling to a child of it's grandparent
        if (grandParentDirection == 0) ((InternalNode)grandParent).Left = sibling;
        else ((InternalNode)grandParent).Right = sibling;

        return true;
    }
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.TryRemove(TKey)"/>
    public bool TryRemove(TKey key) => TryPop(key, out _);
    /// <inheritdoc cref="ICritBitTree{TKey, TValue}.TryUpdate(TKey, TValue)"/>
    public bool TryUpdate(TKey key, TValue newValue) {
        if (root is null) return false;

        ReadOnlySpan<byte> keySpan = key.Span;

        //get the closest match for the given key
        LeafNode leafNode = FindClosestMatch(keySpan, root);
        //if the keys don't match there is no match anywhere in the tree
        if (!keySpan.SequenceEqual(leafNode.Key.Span)) return false;

        leafNode.Value = newValue; //update the value
        return true;
    }

    /// <summary>
    /// Locates the <see cref="LeafNode"/> which is closest to where <paramref name="keySpan"/> would appear in the tree.
    /// If <paramref name="keySpan"/> is present in the tree it's associated <see cref="LeafNode"/> is the located node.
    /// </summary>
    /// <param name="keySpan">The key to use search for.</param>
    /// <param name="node">The <see cref="INode"/> to start at.</param>
    /// <returns>The located <see cref="LeafNode"/>.</returns>
    private static LeafNode FindClosestMatch(ReadOnlySpan<byte> keySpan, INode node) {
        int keyLength = keySpan.Length;

        while (node is InternalNode currInternal) { //Loop until leaf node is reached
            //get the byte of the key at the index specified in the current node. 0 if index out of range.
            byte criticalByte = currInternal.Index < keyLength ? keySpan[currInternal.Index] : (byte)0;
            //the direction can be extracted from the critical byte using the mask of the current node.
            int direction = (1 + (currInternal.Mask | criticalByte)) >> 8;
            //move down the tree based on the direction
            node = direction == 0 ? currInternal.Left : currInternal.Right;
        }

        return (LeafNode)node; //return the terminal node 
    }
    /// <summary>
    /// Gets an enumerable over all of the leaf nodes in the tree in left to right depth first order.
    /// </summary>
    /// <param name="start">The root of the traverse.</param>
    /// <returns>An enumerable over all of the leaf nodes.</returns>
    private static IEnumerable<LeafNode> GetLeafNodes(INode? start) {
        if (start is null) yield break;

        Stack<INode> traversal = new();
        traversal.Push(start);

        while (traversal.Count > 0) {
            INode node = traversal.Pop();

            if (node is LeafNode leafNode) yield return leafNode;

            if (node is InternalNode internalNode) {
                if (internalNode.Right is not null) traversal.Push(internalNode.Right);
                if (internalNode.Left is not null) traversal.Push(internalNode.Left);
            }
        }
    }

    //TODO: REMOVE THIS
    /// <summary>
    /// Debugging tool. Not for final version.
    /// </summary>
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
                Console.Write(KeyToString(leafNode.Key.Memory));
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