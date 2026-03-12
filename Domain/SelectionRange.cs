namespace FlightApp.Domain;

public readonly record struct SelectionRange(int StartIndex, int EndIndex)
{
    public int FromIndex => Math.Min(StartIndex, EndIndex);
    public int ToIndex => Math.Max(StartIndex, EndIndex);

    public int PointCount => ToIndex - FromIndex + 1;

    public bool IsValid => FromIndex >= 0 && ToIndex >= FromIndex;
}