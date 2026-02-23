using System.Collections.Immutable;
using Xunit;

namespace Playground;

/// <summary>
/// Demonstrates immutability behaviour using ImmutableArray&lt;T&gt;.
/// Compare with ArrayImmutabilityTests to see the differences.
/// </summary>
public class ImmutableArrayImmutabilityTests(ITestOutputHelper output)
{
    // -----------------------------------------------------------------------
    // 1. VALUE SEMANTICS VS REFERENCE SEMANTICS
    // -----------------------------------------------------------------------

    [Fact]
    public void ImmutableArray_OfClass_PassedToMethod_ClassMutationIsStillVisibleToCaller()
    {
        var points = ImmutableArray.Create(new MutablePoint { X = 1, Y = 2 });

        MutateFirstElement(points);

        output.WriteLine($"[Class in ImmutableArray] After mutation via method: {points[0]}");
        Assert.Equal(99, points[0].X); // ImmutableArray prevents replacing elements, not mutating class internals
    }

    [Fact]
    public void ImmutableArray_OfRecord_WithExpression_OriginalIsUnaffected()
    {
        var points = ImmutableArray.Create(new ImmutablePoint(1, 2));

        var modified = points[0] with { X = 99 };

        output.WriteLine($"[Record in ImmutableArray] Original: {points[0]}, Modified copy: {modified}");
        Assert.Equal(1, points[0].X);  // with-expression leaves original untouched
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void ImmutableArray_OfStruct_AssignedToNewVariable_SharesUnderlyingData()
    {
        var points = ImmutableArray.Create(new MutableStruct { X = 1, Y = 2 });

        var alias = points; // ImmutableArray<T> is a struct wrapping an array — assignment is a shallow copy
        // alias[0].X = 99; // won't compile — ImmutableArray indexer returns a value, not a ref

        output.WriteLine($"[Struct in ImmutableArray] Original: {points[0]}, Alias shares same data: {alias[0]}");
        Assert.Equal(points[0].X, alias[0].X); // both point at the same underlying array
    }

    // -----------------------------------------------------------------------
    // 2. SHALLOW VS DEEP IMMUTABILITY
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadonlyField_HoldingImmutableArray_BothReferenceAndStructureAreFrozen()
    {
        var holder = new ReadonlyImmutableArrayHolder();

        // holder.Points = ImmutableArray<MutablePoint>.Empty; // compile error — readonly field
        // holder.Points[0] = new MutablePoint();              // compile error — ImmutableArray indexer has no setter
        holder.Points[0].X = 99; // still possible — class internals are not protected

        output.WriteLine($"[Shallow immutability] ImmutableArray structure is frozen, but class item internals are not: {holder.Points[0]}");
        Assert.Equal(99, holder.Points[0].X);
    }

    [Fact]
    public void ImmutableArray_OfRecord_WithInnerList_RecordImmutability_IsShallowOnly()
    {
        var record = new ImmutablePoint(1, 2);
        var arr = ImmutableArray.Create(record);

        var copy = arr[0] with { X = 99 };

        output.WriteLine($"[Shallow vs deep] Original: {arr[0]}, With-copy: {copy}");
        Assert.NotEqual(arr[0].X, copy.X);
    }

    [Fact]
    public void ImmutableArray_CastToIReadOnlyList_CannotBeCastBackToMutableArray()
    {
        ImmutableArray<MutablePoint> immutable = [new MutablePoint { X = 1, Y = 2 }];
        IReadOnlyList<MutablePoint> readOnly = immutable;

        var castAttempt = readOnly as MutablePoint[];

        output.WriteLine($"[ImmutableArray cast] Cast to mutable array: {(castAttempt is null ? "null — blocked" : "succeeded")}");
        Assert.Null(castAttempt); // ImmutableArray cannot be downcast to a mutable array
    }

    // -----------------------------------------------------------------------
    // 3. SILENT COPY-ON-MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void ImmutableArray_Add_WithoutCapture_NewItemSilentlyLost()
    {
        var arr = ImmutableArray.Create(new ImmutablePoint(1, 2));

        arr.Add(new ImmutablePoint(3, 4)); // return value discarded — this is the key pitfall

        output.WriteLine($"[Silent loss] ImmutableArray still has {arr.Length} element(s) — Add() result was discarded");
        Assert.Single(arr);
    }

    [Fact]
    public void ImmutableArray_Add_WithCapture_NewArrayContainsItem()
    {
        var arr = ImmutableArray.Create(new ImmutablePoint(1, 2));

        var newArr = arr.Add(new ImmutablePoint(3, 4)); // must capture the returned array

        output.WriteLine($"[Captured Add()] Original length: {arr.Length}, New array length: {newArr.Length}");
        Assert.Single(arr);
        Assert.Equal(2, newArr.Length);
    }

    [Fact]
    public void ImmutableArray_LinqSelect_DoesNotMutateOriginal()
    {
        var original = ImmutableArray.Create(new ImmutablePoint(1, 2), new ImmutablePoint(3, 4));

        var projected = original.Select(p => p with { X = p.X * 10 }).ToImmutableArray();

        output.WriteLine($"[LINQ] Original[0]: {original[0]}, Projected[0]: {projected[0]}");
        Assert.Equal(1, original[0].X);
        Assert.Equal(10, projected[0].X);
    }

    // -----------------------------------------------------------------------
    // 4. PASS-BY-VALUE VS PASS-BY-REFERENCE
    // -----------------------------------------------------------------------

    [Fact]
    public void ImmutableArray_OfClass_ElementCannotBeReplacedViaRef()
    {
        var points = ImmutableArray.Create(new MutablePoint { X = 1, Y = 2 });

        // ref var refToElement = ref points[0]; // won't compile — no ref indexer on ImmutableArray
        // Element replacement requires creating a new ImmutableArray via SetItem()
        var updated = points.SetItem(0, new MutablePoint { X = 99, Y = 99 });

        output.WriteLine($"[No ref indexer] Original: {points[0]}, Updated copy: {updated[0]}");
        Assert.Equal(1, points[0].X);   // original unchanged
        Assert.Equal(99, updated[0].X);
    }

    [Fact]
    public void ImmutableArray_SetItem_WithoutCapture_ChangeSilentlyLost()
    {
        var points = ImmutableArray.Create(new MutablePoint { X = 1, Y = 2 });

        points.SetItem(0, new MutablePoint { X = 99, Y = 99 }); // return value discarded

        output.WriteLine($"[Silent SetItem loss] Original still: {points[0]} — SetItem() result was discarded");
        Assert.Equal(1, points[0].X);
    }

    [Fact]
    public void ImmutableArray_OfStruct_ElementCopied_MutationDoesNotAffectArray()
    {
        var points = ImmutableArray.Create(new MutableStruct { X = 1, Y = 2 });

        var copy = points[0]; // value copy — struct is returned by value from indexer
        copy.X = 99;

        output.WriteLine($"[Struct copy] ImmutableArray element: {points[0]}, Detached copy: {copy}");
        Assert.Equal(1, points[0].X);
    }

    // -----------------------------------------------------------------------
    // 5. DEFENSIVE COPYING
    // -----------------------------------------------------------------------

    [Fact]
    public void ReturningImmutableArray_CallerCannotReplaceElements()
    {
        var store = new PointStore();
        var immutable = store.GetPointsImmutable();

        // immutable[0] = new MutablePoint { X = 99 }; // won't compile
        output.WriteLine($"[ImmutableArray from store] Caller cannot replace elements. Length: {immutable.Length}");
        Assert.Single(immutable);
    }

    [Fact]
    public void ReturningImmutableArray_CallerCanStillMutateClassInternals()
    {
        var store = new PointStore();
        var immutable = store.GetPointsImmutable();

        immutable[0].X = 99; // class fields inside are still mutable!

        output.WriteLine($"[ImmutableArray shallow] Class item internal state mutated: {immutable[0]}");
        Assert.Equal(99, immutable[0].X); // ImmutableArray only protects the structure, not item internals
    }

    [Fact]
    public void ReturningImmutableArray_NoDefensiveCopyNeeded_StructureIsAlwaysSafe()
    {
        var store = new PointStore();
        var a = store.GetPointsImmutable();
        var b = store.GetPointsImmutable();

        output.WriteLine($"[No defensive copy needed] Both views are structurally identical. a[0]: {a[0]}, b[0]: {b[0]}");
        Assert.Equal(a[0].X, b[0].X);
    }

    // -----------------------------------------------------------------------
    // 6. RECORD MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void ImmutableArray_OfRecordStruct_WithExpression_ProducesIndependentCopy()
    {
        var arr = ImmutableArray.Create(new ImmutableStructPoint(1, 2));

        var modified = arr[0] with { X = 99 };

        output.WriteLine($"[record struct] Original: {arr[0]}, With-copy: {modified}");
        Assert.Equal(1, arr[0].X);
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void ImmutableArray_OfMutableRecord_PropertyCanStillBeMutated()
    {
        var arr = ImmutableArray.Create(new MutableRecord { Name = "Alice", Age = 30 });

        arr[0].Age = 99; // ImmutableArray does not prevent mutation of class/record properties

        output.WriteLine($"[Mutable record in ImmutableArray] After direct mutation: {arr[0]}");
        Assert.Equal(99, arr[0].Age);
    }

    // -----------------------------------------------------------------------
    // 7. COMPILER AND RUNTIME ENFORCEMENT
    // -----------------------------------------------------------------------

    [Fact]
    public void ImmutableArray_IndexerHasNoSetter_CompilerPreventsElementReplacement()
    {
        var arr = ImmutableArray.Create(new ImmutablePoint(1, 2));

        // arr[0] = new ImmutablePoint(99, 99); // compile error — no setter on ImmutableArray indexer
        var updated = arr.SetItem(0, new ImmutablePoint(99, 99)); // must use SetItem and capture result

        output.WriteLine($"[Compiler enforcement] Original: {arr[0]}, Updated copy: {updated[0]}");
        Assert.Equal(1, arr[0].X);
        Assert.Equal(99, updated[0].X);
    }

    [Fact]
    public void ImmutableArray_WithInitOnlyItems_ItemsCannotBeMutatedAfterCreation()
    {
        var arr = ImmutableArray.Create(new InitOnlyPoint { X = 1, Y = 2 });

        // arr[0].X = 99; // compile error — init-only property
        output.WriteLine($"[init-only in ImmutableArray] Item: {arr[0]}, property cannot be set after init");
        Assert.Equal(1, arr[0].X);
    }

    // Helper
    private static void MutateFirstElement(ImmutableArray<MutablePoint> points) => points[0].X = 99;
}
