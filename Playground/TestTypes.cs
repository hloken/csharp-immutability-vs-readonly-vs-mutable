using System.Collections.Immutable;

namespace Playground;

public class MutablePoint
{
    public int X { get; set; }
    public int Y { get; set; }
    public override string ToString() => $"MutablePoint({X}, {Y})";
}

public record ImmutablePoint(int X, int Y);

public record struct ImmutableStructPoint(int X, int Y);

public struct MutableStruct
{
    public int X { get; set; }
    public int Y { get; set; }
    public override string ToString() => $"MutableStruct({X}, {Y})";
}

public record MutableRecord
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public override string ToString() => $"MutableRecord({Name}, Age={Age})";
}

public class InitOnlyPoint
{
    public int X { get; init; }
    public int Y { get; init; }
    public override string ToString() => $"InitOnlyPoint({X}, {Y})";
}

public class ReadonlyArrayHolder
{
    public readonly MutablePoint[] Points = [new MutablePoint { X = 1, Y = 2 }];
}

public class ReadonlyImmutableArrayHolder
{
    public readonly ImmutableArray<MutablePoint> Points = [new MutablePoint { X = 1, Y = 2 }];
}

public class PointStore
{
    private readonly MutablePoint[] _points = [new MutablePoint { X = 1, Y = 2 }];

    public MutablePoint[] GetPointsUnsafe() => _points;
    public MutablePoint[] GetPointsSafe() => (MutablePoint[])_points.Clone();
    public ImmutableArray<MutablePoint> GetPointsImmutable() => [.. _points];
}