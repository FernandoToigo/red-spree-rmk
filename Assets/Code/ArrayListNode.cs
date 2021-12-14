public struct ArrayListNode<T>
{
    public T Value;
    public int PreviousIndex;
    public int Index;
    public int NextIndex;

    public bool HasPrevious => PreviousIndex >= 0;
    public bool HasNext => NextIndex >= 0;
}