public struct ReusableArray<T>
{
    public ReusableArray(int capacity)
        : this(new T[capacity])
    {
    }

    public ReusableArray(T[] array)
    {
        Data = array;
        UsableLength = 0;
    }

    public readonly T[] Data;
    public int UsableLength;

    public int Capacity => Data.Length;

    public void Add(T value)
    {
        Data[UsableLength] = value;
        UsableLength++;
    }

    public void Clear()
    {
        UsableLength = 0;
    }

    public static implicit operator ReusableArray<T>(T[] array)
    {
        return new ReusableArray<T>(array);
    }
}