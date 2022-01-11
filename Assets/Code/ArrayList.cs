public class ArrayList<T>
{
    public ArrayList(int capacity)
    {
        _data = new ArrayListNode<T>[capacity];
        _emptyIndices = new int[capacity];
        for (var i = 0; i < capacity; i++)
        {
            _emptyIndices[i] = i;
        }

        _currentEmptyIndex = 0;
        _tailIndex = -1;
        _headIndex = -1;
        Count = 0;
    }

    private readonly ArrayListNode<T>[] _data;
    private readonly int[] _emptyIndices;
    private int _currentEmptyIndex;
    private int _tailIndex;
    private int _headIndex;
    public int Count { get; private set; }

    public ref ArrayListNode<T> Tail() => ref _data[_tailIndex];
    public ref ArrayListNode<T> Head() => ref _data[_headIndex];
    public ref ArrayListNode<T> GetAt(int index) => ref _data[index];
    public ref ArrayListNode<T> Previous(ref ArrayListNode<T> node) => ref _data[node.PreviousIndex];
    public ref ArrayListNode<T> Next(ref ArrayListNode<T> node) => ref _data[node.NextIndex];

    public int Add(T value)
    {
        var index = _emptyIndices[_currentEmptyIndex];
        _currentEmptyIndex++;
        _data[index] = new ArrayListNode<T>
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

    public void Remove(ref ArrayListNode<T> node)
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

    public Iterator Iterate()
    {
        return new Iterator
        {
            List = this,
            CurrentIndex = -1
        };
    }

    public struct Iterator
    {
        public ArrayList<T> List;
        public int CurrentIndex;

        public ref ArrayListNode<T> Current()
        {
            return ref List.GetAt(CurrentIndex);
        }

        public bool Next()
        {
            if (CurrentIndex == -1)
            {
                if (List.Count > 0)
                {
                    CurrentIndex = List._tailIndex;
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