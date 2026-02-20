using Xunit;

namespace Playground;

/// <summary>
/// Demonstrates immutability behaviour using plain T[] arrays.
/// Compare with ImmutableArrayImmutabilityTests to see the differences.
/// </summary>
public class ArrayImmutabilityTests(ITestOutputHelper output)
{
    // -----------------------------------------------------------------------
    // 1. VALUE SEMANTICS VS REFERENCE SEMANTICS
    // -----------------------------------------------------------------------

    [Fact]
    public void Array_OfClass_PassedToMethod_MutationIsVisibleToCaller()
    {
        var points = new[] { new MutablePoint { X = 1, Y = 2 } };
        output.WriteLine($"[Class in array] Before mutation via method: {points[0]}");

        MutateFirstElement(points);

        output.WriteLine($"[Class in array] After mutation via method: {points[0]}");
        Assert.Equal(99, points[0].X); // class is reference type — caller sees the change
    }

    [Fact]
    public void Array_OfRecord_WithExpression_OriginalIsUnaffected()
    {
        var points = new[] { new ImmutablePoint(1, 2) };
        output.WriteLine($"[Record in array] Before mutation: Original: {points[0]}");

        var modified = points[0] with { X = 99 };

        output.WriteLine($"[Record in array] After mutation: Original: {points[0]}, Modified copy: {modified}");
        Assert.Equal(1, points[0].X);  // with-expression leaves original untouched
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void Array_OfStruct_AssignedToNewVariable_IsIndependentCopy()
    {
        var points = new[] { new MutableStruct { X = 1, Y = 2 } };
        output.WriteLine($"[Struct in array] Before mutation: Original: {points[0]}");

        var copy = points[0]; // struct copied by value
        copy.X = 99;

        output.WriteLine($"[Struct in array] After mutation: Original: {points[0]}, Copy: {copy}");
        Assert.Equal(1, points[0].X); // array element is unchanged
    }

    // -----------------------------------------------------------------------
    // 2. SHALLOW VS DEEP IMMUTABILITY
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadonlyField_HoldingArray_ReferenceIsFrozen_ButContentsAreMutable()
    {
        var holder = new ReadonlyArrayHolder();
        output.WriteLine($"[Shallow immutability] Before mutation: Item: {holder.Points[0]}");


        // holder.Points = []; // compile error — readonly field cannot be reassigned
        holder.Points[0].X = 99; // contents are still mutable

        output.WriteLine($"[Shallow immutability] After mutation: readonly freezes the reference, not the contents: Item: {holder.Points[0]}");
        Assert.Equal(99, holder.Points[0].X);
    }

    [Fact]
    public void Array_OfRecord_WithInnerList_RecordImmutability_IsShallowOnly()
    {
        var record = new ImmutablePoint(1, 2);
        var arr = new[] { record };
        output.WriteLine($"[Shallow vs deep] Before mutation: Original: {arr[0]}");

        var copy = arr[0] with { X = 99 };

        output.WriteLine($"[Shallow vs deep] After mutation: Original: {arr[0]}, With-copy: {copy}");
        Assert.NotEqual(arr[0].X, copy.X); // record protects its own fields, not nested mutable state
    }

    [Fact]
    public void IReadOnlyList_CanBeCastBackToArray_AndMutated()
    {
        MutablePoint[] original = [new MutablePoint { X = 1, Y = 2 }];
        output.WriteLine($"[IReadOnlyList bypass] Before mutation: Original item: {original[0]}");

        IReadOnlyList<MutablePoint> readOnly = original;

        var castBack = (MutablePoint[])readOnly; // IReadOnlyList is a contract, not enforcement
        castBack[0].X = 99;

        output.WriteLine($"[IReadOnlyList bypass] After mutation: Cast back and mutated: Original item: {original[0]}");
        Assert.Equal(99, original[0].X); // read-only contract was bypassed
    }

    // -----------------------------------------------------------------------
    // 3. SILENT COPY-ON-MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void Array_Add_IsNotPossible_MustResizeManually()
    {
        var arr = new[] { new ImmutablePoint(1, 2) };
        output.WriteLine($"[No Add()] Before mutation: Original length: {arr.Length}");

        // Arrays have fixed size — no Add() method exists
        // To "add", you must create a new array
        var newArr = new ImmutablePoint[arr.Length + 1];
        Array.Copy(arr, newArr, arr.Length);
        newArr[^1] = new ImmutablePoint(3, 4);

        output.WriteLine($"[No Add()] After mutation: Original length: {arr.Length}, New array length: {newArr.Length}");
        Assert.Single(arr);         // original unchanged
        Assert.Equal(2, newArr.Length);
    }

    [Fact]
    public void Array_LinqSelect_DoesNotMutateOriginal()
    {
        var original = new[] { new ImmutablePoint(1, 2), new ImmutablePoint(3, 4) };
        output.WriteLine($"[LINQ] Original[0]: Before projection: {original[0]}");

        var projected = original.Select(p => p with { X = p.X * 10 }).ToArray();

        output.WriteLine($"[LINQ] Original[0]: After projection: {original[0]}, Projected[0]: {projected[0]}");
        Assert.Equal(1, original[0].X);   // original untouched
        Assert.Equal(10, projected[0].X);
    }

    [Fact]
    public void Array_DirectElementMutation_IsReflectedInPlace()
    {
        // Unlike ImmutableArray, mutating an element in a plain array has no "silent loss" risk
        var arr = new[] { new ImmutablePoint(1, 2) };
        output.WriteLine($"[In-place mutation] Before mutation: arr Item0: {arr[0]}");

        arr[0] = new ImmutablePoint(99, 99); // direct replacement — no new array needed

        output.WriteLine($"[In-place mutation] After mutation: Element replaced directly: arr Item0: {arr[0]}");
        Assert.Equal(99, arr[0].X); // mutation is visible immediately
    }

    // -----------------------------------------------------------------------
    // 4. PASS-BY-VALUE VS PASS-BY-REFERENCE
    // -----------------------------------------------------------------------

    [Fact]
    public void Array_OfClass_ElementMutatedViaRef_CallerSeesChange()
    {
        var points = new[] { new MutablePoint { X = 1, Y = 2 } };
        output.WriteLine($"[ref to class element] Before ref replacement: {points[0]}");

        ref var refToElement = ref points[0];
        refToElement = new MutablePoint { X = 99, Y = 99 };

        output.WriteLine($"[ref to class element] After ref replacement: {points[0]}");
        Assert.Equal(99, points[0].X);
    }

    [Fact]
    public void Array_OfStruct_ElementMutatedViaRef_CallerSeesChange()
    {
        var points = new[] { new MutableStruct { X = 1, Y = 2 } };
        output.WriteLine($"[ref to struct in array] Before mutation: points0: {points[0]}");

        ref var refToStruct = ref points[0];
        refToStruct.X = 99; // ref gives direct access to the array slot

        output.WriteLine($"[ref to struct in array] After mutation: Mutated in place via ref: points0: {points[0]}");
        Assert.Equal(99, points[0].X);
    }

    [Fact]
    public void Array_OfStruct_ElementCopied_MutationDoesNotAffectArray()
    {
        var points = new[] { new MutableStruct { X = 1, Y = 2 } };
        output.WriteLine($"[Struct copy] Before mutation: Array element: {points[0]}");

        var copy = points[0]; // value copy — detached from array
        copy.X = 99;

        output.WriteLine($"[Struct copy] After mutation: Array element: {points[0]}, Detached copy: {copy}");
        Assert.Equal(1, points[0].X); // array element unchanged
    }

    // -----------------------------------------------------------------------
    // 5. DEFENSIVE COPYING
    // -----------------------------------------------------------------------

    [Fact]
    public void ReturningArrayDirectly_AllowsCallerToCorruptInternalState()
    {
        var store = new PointStore();
        var leaked = store.GetPointsUnsafe();
        output.WriteLine($"[No defensive copy] Before mutation: {store.GetPointsUnsafe()[0]}");

        leaked[0] = new MutablePoint { X = 99, Y = 99 };

        output.WriteLine($"[No defensive copy] After mutation: Internal state corrupted: {store.GetPointsUnsafe()[0]}");
        Assert.Equal(99, store.GetPointsUnsafe()[0].X); // internal array was silently mutated
    }

    [Fact]
    public void ReturningArrayCopy_IsolatesInternalState()
    {
        var store = new PointStore();
        output.WriteLine($"[Defensive copy] Before mutation: {store.GetPointsUnsafe()[0]}");

        var safeCopy = store.GetPointsSafe();

        safeCopy[0] = new MutablePoint { X = 99, Y = 99 };

        output.WriteLine($"[Defensive copy] After mutation: Internal state protected: {store.GetPointsUnsafe()[0]}");
        Assert.Equal(1, store.GetPointsUnsafe()[0].X); // internal array unchanged
    }

    [Fact]
    public void ReturningImmutableArray_RequiresNoDefensiveCopy()
    {
        var store = new PointStore();
        output.WriteLine($"[ImmutableArray from store] Before mutation: length: {store.GetPointsSafe().Length}");

        var immutable = store.GetPointsImmutable();

        // immutable[0] = new MutablePoint { X = 99 }; // won't compile
        output.WriteLine($"[ImmutableArray from store] After mutation: Safe by design, length: {immutable.Length}");
        Assert.Single(immutable);
    }

    // -----------------------------------------------------------------------
    // 6. RECORD MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void Array_OfRecordStruct_WithExpression_ProducesIndependentCopy()
    {
        var arr = new[] { new ImmutableStructPoint(1, 2) };
        output.WriteLine($"[record struct] Before mutation: riginal: {arr[0]}");

        var modified = arr[0] with { X = 99 };

        output.WriteLine($"[record struct] After mutation: Original: {arr[0]}, With-copy: {modified}");
        Assert.Equal(1, arr[0].X);
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void Array_OfMutableRecord_CanBeMutatedDirectly()
    {
        var arr = new[] { new MutableRecord { Name = "Alice", Age = 30 } };
        output.WriteLine($"[Mutable record in array] Before direct mutation: {arr[0]}");

        arr[0].Age = 99; // records CAN expose mutable properties

        output.WriteLine($"[Mutable record in array] After direct mutation: {arr[0]}");
        Assert.Equal(99, arr[0].Age);
    }

    // -----------------------------------------------------------------------
    // 7. COMPILER AND RUNTIME ENFORCEMENT
    // -----------------------------------------------------------------------

    [Fact]
    public void IReadOnlyList_CanBeSubvertedAtRuntime_UnlikeImmutableArray()
    {
        MutablePoint[] original = [new MutablePoint { X = 1, Y = 2 }];
        IReadOnlyList<MutablePoint> readOnly = original;

        var castBack = readOnly as MutablePoint[];

        output.WriteLine($"[Runtime enforcement] IReadOnlyList cast back to array: {(castBack is null ? "null — blocked" : "succeeded — mutable access gained")}");
        Assert.NotNull(castBack); // cast succeeds — IReadOnlyList provides no real protection
    }

    [Fact]
    public void Array_WithInitOnlyItems_ItemsCannotBeMutatedAfterCreation()
    {
        var arr = new[] { new InitOnlyPoint { X = 1, Y = 2 } };

        // arr[0].X = 99; // compile error — init-only property
        output.WriteLine($"[init-only] Item in array: {arr[0]}, property cannot be set after init");
        Assert.Equal(1, arr[0].X);
    }

    // Helper
    private static void MutateFirstElement(MutablePoint[] points) => points[0].X = 99;
}
