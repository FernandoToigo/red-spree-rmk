public class ArrayLinkedList<T>
{
    public ArrayLinkedList(int capacity)
    {
        _data = new ArrayLinkedListNode<T>[capacity];
        _emptyIndices = new int[capacity];

        Clear();
    }

    private readonly ArrayLinkedListNode<T>[] _data;
    private readonly int[] _emptyIndices;
    private int _currentEmptyIndex;
    private int _tailIndex;
    private int _headIndex;
    public int Count { get; private set; }

    public ref ArrayLinkedListNode<T> Tail() => ref _data[_tailIndex];
    public ref ArrayLinkedListNode<T> Head() => ref _data[_headIndex];
    public ref ArrayLinkedListNode<T> GetAt(int index) => ref _data[index];
    public ref ArrayLinkedListNode<T> Previous(ref ArrayLinkedListNode<T> node) => ref _data[node.PreviousIndex];
    public ref ArrayLinkedListNode<T> Next(ref ArrayLinkedListNode<T> node) => ref _data[node.NextIndex];

    public int Add(T value)
    {
        var index = _emptyIndices[_currentEmptyIndex];
        _currentEmptyIndex++;
        _data[index] = new ArrayLinkedListNode<T>
        {
            Value = value,
            Index = index,
            PreviousIndex = _headIndex,
            NextIndex = -1
        };
                
        if (_headIndex >= 0)
        {
            _data[_headIndex].NextIndex = index;
        }

        if (_tailIndex < 0)
        {
            _tailIndex = index;
        }

        _headIndex = index;
        Count++;
        return index;
    }

    public void Remove(ref ArrayLinkedListNode<T> node)
    {
        _currentEmptyIndex--;
        _emptyIndices[_currentEmptyIndex] = node.Index;
        if (node.HasPrevious)
        {
            _data[node.PreviousIndex].NextIndex = node.NextIndex;
        }

        if (node.HasNext)
        {
            _data[node.NextIndex].PreviousIndex = node.PreviousIndex;
        }

        if (node.Index == _tailIndex)
        {
            _tailIndex = node.NextIndex;
        }

        if (node.Index == _headIndex)
        {
            _headIndex = node.PreviousIndex;
        }
                
        Count--;
    }

    public void Clear()
    {
        for (var i = 0; i < _emptyIndices.Length; i++)
        {
            _emptyIndices[i] = i;
        }

        _currentEmptyIndex = 0;
        _tailIndex = -1;
        _headIndex = -1;
        Count = 0;
    }

    public Iterator Iterate()
    {
        return new Iterator
        {
            LinkedList = this,
            CurrentIndex = -1
        };
    }

    public struct Iterator
    {
        public ArrayLinkedList<T> LinkedList;
        public int CurrentIndex;

        public ref ArrayLinkedListNode<T> Current()
        {
            return ref LinkedList.GetAt(CurrentIndex);
        }

        public bool Next()
        {
            if (CurrentIndex == -1)
            {
                if (LinkedList.Count > 0)
                {
                    CurrentIndex = LinkedList._tailIndex;
                    return true;
                }
                
                return false;
            }
            
            ref var node = ref Current();
            if (node.HasNext)
            {
                CurrentIndex = node.NextIndex;
                return true;
            }

            return false;
        }
    }
}