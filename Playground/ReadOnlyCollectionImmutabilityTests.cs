using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Xunit;

namespace Playground;

/// <summary>
/// Demonstrates immutability behaviour using ReadOnlyCollection&lt;T&gt;.
/// Compare with ArrayImmutabilityTests and ImmutableArrayImmutabilityTests to see the differences.
/// </summary>
public class ReadOnlyCollectionImmutabilityTests(ITestOutputHelper output)
{
    // -----------------------------------------------------------------------
    // 1. VALUE SEMANTICS VS REFERENCE SEMANTICS
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyCollection_OfClass_PassedToMethod_MutationIsVisibleToCaller()
    {
        var points = new ReadOnlyCollection<MutablePoint>([new MutablePoint { X = 1, Y = 2 }]);

        MutateFirstElement(points);

        output.WriteLine($"[Class in ReadOnlyCollection] After mutation via method: {points[0]}");
        Assert.Equal(99, points[0].X); // ReadOnlyCollection prevents replacing elements, not mutating class internals
    }

    [Fact]
    public void ReadOnlyCollection_OfRecord_WithExpression_OriginalIsUnaffected()
    {
        var points = new ReadOnlyCollection<ImmutablePoint>([new ImmutablePoint(1, 2)]);

        var modified = points[0] with { X = 99 };

        output.WriteLine($"[Record in ReadOnlyCollection] Original: {points[0]}, Modified copy: {modified}");
        Assert.Equal(1, points[0].X);  // with-expression leaves original untouched
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void ReadOnlyCollection_OfStruct_IndexerReturnsValueCopy_MutationDoesNotAffectCollection()
    {
        var points = new ReadOnlyCollection<MutableStruct>([new MutableStruct { X = 1, Y = 2 }]);

        var copy = points[0]; // struct is returned by value from indexer
        copy.X = 99;

        output.WriteLine($"[Struct in ReadOnlyCollection] Original: {points[0]}, Copy after mutation: {copy}");
        Assert.Equal(1, points[0].X); // collection element is unchanged
    }

    // -----------------------------------------------------------------------
    // 2. SHALLOW VS DEEP IMMUTABILITY
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyCollection_WrapsOriginalList_MutatingSourceListAffectsView()
    {
        var source = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };
        var readOnly = new ReadOnlyCollection<MutablePoint>(source);

        source.Add(new MutablePoint { X = 3, Y = 4 }); // mutate the backing list

        output.WriteLine($"[Shallow wrapper] ReadOnlyCollection reflects source list change. Count: {readOnly.Count}");
        Assert.Equal(2, readOnly.Count); // ReadOnlyCollection is a live view — not a snapshot
    }

    [Fact]
    public void ReadOnlyCollection_OfClass_ItemInternalsAreMutable()
    {
        var points = new ReadOnlyCollection<MutablePoint>([new MutablePoint { X = 1, Y = 2 }]);

        points[0].X = 99; // class internals are not protected

        output.WriteLine($"[Shallow immutability] ReadOnlyCollection prevents add/remove, not item mutation: {points[0]}");
        Assert.Equal(99, points[0].X);
    }

    [Fact]
    public void ReadOnlyCollection_CanBeCastBackToList_AndMutated()
    {
        var source = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };
        IReadOnlyList<MutablePoint> readOnly = source.AsReadOnly();

        var castBack = readOnly as List<MutablePoint>; // attempt to subvert the read-only contract

        output.WriteLine($"[IReadOnlyList bypass] Cast back to List<T>: {(castBack is null ? "null — blocked" : "succeeded — mutable access gained")}");
        Assert.Null(castBack); // ReadOnlyCollection wraps the list — direct cast to List<T> fails
    }

    // -----------------------------------------------------------------------
    // 3. SILENT COPY-ON-MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyCollection_HasNoAdd_MutationAttempt_WillNotCompile()
    {
        var points = new ReadOnlyCollection<ImmutablePoint>([new ImmutablePoint(1, 2)]);

        // points.Add(new ImmutablePoint(3, 4)); // won't compile — no Add() on ReadOnlyCollection
        // To add, you must go back to the source list or create a new collection
        output.WriteLine($"[No Add()] ReadOnlyCollection has no Add() — count stays {points.Count}");
        Assert.Single(points);
    }

    [Fact]
    public void ReadOnlyCollection_LinqSelect_DoesNotMutateOriginal()
    {
        var original = new ReadOnlyCollection<ImmutablePoint>(
            [new ImmutablePoint(1, 2), new ImmutablePoint(3, 4)]);

        var projected = original.Select(p => p with { X = p.X * 10 }).ToList();

        output.WriteLine($"[LINQ] Original[0]: {original[0]}, Projected[0]: {projected[0]}");
        Assert.Equal(1, original[0].X);   // original untouched
        Assert.Equal(10, projected[0].X);
    }

    [Fact]
    public void ReadOnlyCollection_BackingListMutated_ViewReflectsChange_NoSilentLoss()
    {
        var source = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };
        var readOnly = source.AsReadOnly();

        source[0] = new ImmutablePoint(99, 99); // mutate backing list directly

        output.WriteLine($"[Live view] ReadOnlyCollection reflects backing list change: {readOnly[0]}");
        Assert.Equal(99, readOnly[0].X); // ReadOnlyCollection is a live view, not a copy
    }

    // -----------------------------------------------------------------------
    // 4. PASS-BY-VALUE VS PASS-BY-REFERENCE
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyCollection_OfClass_ElementMutatedViaMethod_CallerSeesChange()
    {
        var points = new ReadOnlyCollection<MutablePoint>([new MutablePoint { X = 1, Y = 2 }]);

        MutateFirstElement(points);

        output.WriteLine($"[Class mutation via method] After mutation: {points[0]}");
        Assert.Equal(99, points[0].X); // class internals mutated through the read-only wrapper
    }

    [Fact]
    public void ReadOnlyCollection_OfStruct_ElementCopied_MutationDoesNotAffectCollection()
    {
        var points = new ReadOnlyCollection<MutableStruct>([new MutableStruct { X = 1, Y = 2 }]);

        var copy = points[0]; // value copy — detached from collection
        copy.X = 99;

        output.WriteLine($"[Struct copy] Collection element: {points[0]}, Detached copy: {copy}");
        Assert.Equal(1, points[0].X); // collection element unchanged
    }

    [Fact]
    public void ReadOnlyCollection_NoRefIndexer_StructCannotBeMutatedInPlace()
    {
        var source = new List<MutableStruct> { new MutableStruct { X = 1, Y = 2 } };
        var readOnly = source.AsReadOnly();

        // ref var refToStruct = ref readOnly[0]; // won't compile — no ref indexer on ReadOnlyCollection
        // Must go through the source list to mutate a struct in place
        source[0] = new MutableStruct { X = 99, Y = 99 };

        output.WriteLine($"[No ref indexer] Struct mutated via source list: {readOnly[0]}");
        Assert.Equal(99, readOnly[0].X); // change visible via view because source was modified
    }

    // -----------------------------------------------------------------------
    // 5. DEFENSIVE COPYING
    // -----------------------------------------------------------------------

    [Fact]
    public void ReturningReadOnlyCollection_CallerCannotAddOrRemove()
    {
        var store = new PointStoreWithReadOnly();
        var view = store.GetPointsReadOnly();

        // view.Add(new MutablePoint { X = 99 }); // won't compile
        output.WriteLine($"[ReadOnlyCollection from store] Caller cannot add/remove. Count: {view.Count}");
        Assert.Single(view);
    }

    [Fact]
    public void ReturningReadOnlyCollection_CallerCanStillMutateClassInternals()
    {
        var store = new PointStoreWithReadOnly();
        var view = store.GetPointsReadOnly();

        view[0].X = 99; // class item internals are still mutable

        output.WriteLine($"[ReadOnlyCollection shallow] Class item internal mutated: {view[0]}");
        Assert.Equal(99, view[0].X);
    }

    [Fact]
    public void ReturningReadOnlyCollection_BackingListStillMutable_InternalStateCanChange()
    {
        var store = new PointStoreWithReadOnly();
        var view = store.GetPointsReadOnly();

        store.AddPoint(new MutablePoint { X = 99, Y = 99 }); // store mutates its own backing list

        output.WriteLine($"[Live view risk] ReadOnlyCollection reflects internal list change. Count: {view.Count}");
        Assert.Equal(2, view.Count); // view grew because backing list changed — not truly isolated
    }

    // -----------------------------------------------------------------------
    // 6. RECORD MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyCollection_OfRecordStruct_WithExpression_ProducesIndependentCopy()
    {
        var points = new ReadOnlyCollection<ImmutableStructPoint>([new ImmutableStructPoint(1, 2)]);

        var modified = points[0] with { X = 99 };

        output.WriteLine($"[record struct] Original: {points[0]}, With-copy: {modified}");
        Assert.Equal(1, points[0].X);
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void ReadOnlyCollection_OfMutableRecord_PropertyCanStillBeMutated()
    {
        var points = new ReadOnlyCollection<MutableRecord>(
            [new MutableRecord { Name = "Alice", Age = 30 }]);

        points[0].Age = 99; // ReadOnlyCollection does not prevent mutation of record properties

        output.WriteLine($"[Mutable record in ReadOnlyCollection] After direct mutation: {points[0]}");
        Assert.Equal(99, points[0].Age);
    }

    // -----------------------------------------------------------------------
    // 7. COMPILER AND RUNTIME ENFORCEMENT
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyCollection_IndexerHasNoSetter_CompilerPreventsElementReplacement()
    {
        var points = new ReadOnlyCollection<ImmutablePoint>([new ImmutablePoint(1, 2)]);

        // points[0] = new ImmutablePoint(99, 99); // won't compile — no setter on indexer
        output.WriteLine($"[Compiler enforcement] Cannot replace element via indexer: {points[0]}");
        Assert.Equal(1, points[0].X);
    }

    [Fact]
    public void ReadOnlyCollection_VsImmutableArray_BackingListExposure_IsKeyDifference()
    {
        var source = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };
        var readOnly = source.AsReadOnly();
        var immutable = source.ToImmutableArray();

        source.Add(new ImmutablePoint(3, 4)); // mutate the source

        output.WriteLine($"[ReadOnly vs Immutable] ReadOnlyCollection count: {readOnly.Count}, ImmutableArray count: {immutable.Length}");
        Assert.Equal(2, readOnly.Count);   // ReadOnlyCollection reflects the change — it is a live view
        Assert.Single(immutable);          // ImmutableArray is a true snapshot — unaffected
    }

    [Fact]
    public void ReadOnlyCollection_WithInitOnlyItems_ItemsCannotBeMutatedAfterCreation()
    {
        var points = new ReadOnlyCollection<InitOnlyPoint>([new InitOnlyPoint { X = 1, Y = 2 }]);

        // points[0].X = 99; // compile error — init-only property
        output.WriteLine($"[init-only in ReadOnlyCollection] Item: {points[0]}, property cannot be set after init");
        Assert.Equal(1, points[0].X);
    }

    // Helpers
    private static void MutateFirstElement(ReadOnlyCollection<MutablePoint> points) => points[0].X = 99;
}

// -----------------------------------------------------------------------
// SUPPORTING TYPE FOR DEFENSIVE COPY TESTS
// -----------------------------------------------------------------------

public class PointStoreWithReadOnly
{
    private readonly List<MutablePoint> _points = [new MutablePoint { X = 1, Y = 2 }];

    public ReadOnlyCollection<MutablePoint> GetPointsReadOnly() => _points.AsReadOnly();
    public void AddPoint(MutablePoint point) => _points.Add(point);
}
