using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Xunit;

namespace Playground;

/// <summary>
/// Demonstrates immutability behaviour using List&lt;T&gt;.
/// Compare with ArrayImmutabilityTests, ImmutableArrayImmutabilityTests and ReadOnlyCollectionImmutabilityTests.
/// </summary>
public class ListImmutabilityTests(ITestOutputHelper output)
{
    // -----------------------------------------------------------------------
    // 1. VALUE SEMANTICS VS REFERENCE SEMANTICS
    // -----------------------------------------------------------------------

    [Fact]
    public void List_OfClass_PassedToMethod_MutationIsVisibleToCaller()
    {
        var points = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };

        output.WriteLine($"[Class in List] Before mutation via method: {points[0]}");

        MutateFirstElement(points);

        output.WriteLine($"[Class in List] After mutation via method: {points[0]}");
        Assert.Equal(99, points[0].X); // class is reference type — caller sees the change
    }

    [Fact]
    public void List_OfRecord_WithExpression_OriginalIsUnaffected()
    {
        var points = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };

        output.WriteLine($"[Record in List] Before mutation: Original: {points[0]}");

        var modified = points[0] with { X = 99 };

        output.WriteLine($"[Record in List] After mutation: Original: {points[0]}, Modified copy: {modified}");
        Assert.Equal(1, points[0].X);  // with-expression leaves original untouched
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void List_OfStruct_IndexerReturnsValueCopy_MutationDoesNotAffectList()
    {
        var points = new List<MutableStruct> { new MutableStruct { X = 1, Y = 2 } };
        output.WriteLine($"[Struct in List] Before mutation: Original: {points[0]}");

        var copy = points[0]; // struct is returned by value from indexer
        copy.X = 99;

        output.WriteLine($"[Struct in List] After mutation: Original: {points[0]}, Copy after mutation: {copy}");
        Assert.Equal(1, points[0].X); // list element is unchanged
    }

    [Fact]
    public void List_AssignedToNewVariable_BothVariablesShareSameReference()
    {
        var original = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };
        var alias = original; // List<T> is a class — assignment copies the reference
        output.WriteLine($"[Reference semantics] Before mutation: Both variables share same List. Original count: {original.Count} Alias count: {alias.Count}");

        alias.Add(new MutablePoint { X = 3, Y = 4 });

        output.WriteLine($"[Reference semantics] After mutation: Both variables share same List. Original count: {original.Count} Alias count: {alias.Count}");
        Assert.Equal(2, original.Count); // original reflects the change — same object
    }

    // -----------------------------------------------------------------------
    // 2. SHALLOW VS DEEP IMMUTABILITY
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadonlyField_HoldingList_ReferenceIsFrozen_ButContentsAreMutable()
    {
        var holder = new ReadonlyListHolder();
        output.WriteLine($"[Shallow immutability] Before mutation: readonly freezes the reference, not the list contents. Count: {holder.Points.Count}, Item0: {holder.Points[0]}");

        // holder.Points = new List<MutablePoint>(); // compile error — readonly field cannot be reassigned
        holder.Points[0].X = 99;     // item internals are mutable
        holder.Points.Add(new MutablePoint { X = 3, Y = 4 }); // list structure is also mutable

        output.WriteLine($"[Shallow immutability] After mutation: readonly freezes the reference, not the list contents. Count: {holder.Points.Count}, Item0: {holder.Points[0]}");
        Assert.Equal(99, holder.Points[0].X);
        Assert.Equal(2, holder.Points.Count);
    }

    [Fact]
    public void List_OfRecord_RecordImmutability_IsShallowOnly()
    {
        var list = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };
        output.WriteLine($"[Shallow vs deep] Before copy: Original: {list[0]}");

        var copy = list[0] with { X = 99 };

        output.WriteLine($"[Shallow vs deep] After copy: Original: {list[0]}, With-copy: {copy}");
        Assert.NotEqual(list[0].X, copy.X); // record protects its own fields, not the list structure
    }

    [Fact]
    public void IReadOnlyList_CanBeCastBackToList_AndFullyMutated()
    {
        var original = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };
        IReadOnlyList<MutablePoint> readOnly = original;
        output.WriteLine($"[IReadOnlyList bypass] Before mutation: Original Count: {original.Count}, First: {original[0]}");

        var castBack = (List<MutablePoint>)readOnly; // IReadOnlyList is a contract, not enforcement
        castBack.Add(new MutablePoint { X = 3, Y = 4 });
        castBack[0].X = 99;

        output.WriteLine($"[IReadOnlyList bypass] After mutation: Cast back and mutated. Original Count: {original.Count}, First: {original[0]}");
        Assert.Equal(2, original.Count);  // structure mutated — item added
        Assert.Equal(99, original[0].X); // item internals mutated
    }

    // -----------------------------------------------------------------------
    // 3. SILENT COPY-ON-MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void List_Add_MutatesInPlace_NoNewListCreated()
    {
        var list = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };
        output.WriteLine($"[In-place Add()] Before mutation: Count: {list.Count}");

        list.Add(new ImmutablePoint(3, 4)); // mutates the existing list directly — no return value

        output.WriteLine($"[In-place Add()] After mutation: List mutated directly. Count: {list.Count}");
        Assert.Equal(2, list.Count); // unlike ImmutableList/ImmutableArray, no silent loss risk here
    }

    [Fact]
    public void List_LinqSelect_DoesNotMutateOriginal()
    {
        var original = new List<ImmutablePoint> { new ImmutablePoint(1, 2), new ImmutablePoint(3, 4) };
        output.WriteLine($"[LINQ] Before projection: Original[0]: {original[0]}");

        var projected = original.Select(p => p with { X = p.X * 10 }).ToList();

        output.WriteLine($"[LINQ] After projection: Original[0]: {original[0]}, Projected[0]: {projected[0]}");
        Assert.Equal(1, original[0].X);   // original untouched
        Assert.Equal(10, projected[0].X);
    }

    [Fact]
    public void List_DirectElementReplacement_MutatesInPlace()
    {
        var list = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };
        output.WriteLine($"[In-place replacement] Before mutation: Item0: {list[0]}");

        list[0] = new ImmutablePoint(99, 99); // direct replacement — unlike ImmutableList, no capture needed
        output.WriteLine($"[In-place replacement] After mutation: Element replaced directly: Item0: {list[0]}");

        Assert.Equal(99, list[0].X);
    }

    // -----------------------------------------------------------------------
    // 4. PASS-BY-VALUE VS PASS-BY-REFERENCE
    // -----------------------------------------------------------------------

    [Fact]
    public void List_PassedToMethod_StructuralChangesVisibleToCaller()
    {
        var points = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };
        output.WriteLine($"[List passed to method] Before call: Count: {points.Count}");

        AddElementInMethod(points);

        output.WriteLine($"[List passed to method] After call: Caller sees structural change. Count: {points.Count}");
        Assert.Equal(2, points.Count); // List<T> is a class — passed by reference, structural changes visible
    }

    [Fact]
    public void List_OfStruct_ElementCopied_MutationDoesNotAffectList()
    {
        var points = new List<MutableStruct> { new MutableStruct { X = 1, Y = 2 } };
        output.WriteLine($"[Struct copy] Before mutation of element: List element: {points[0]}");

        var copy = points[0]; // value copy — detached from list
        copy.X = 99;

        output.WriteLine($"[Struct copy] After mutation of element: List element: {points[0]}, Detached copy: {copy}");
        Assert.Equal(1, points[0].X); // list element unchanged
    }

    [Fact]
    public void List_OfStruct_MutateViaIndexer_RequiresFullReplacement()
    {
        var points = new List<MutableStruct> { new MutableStruct { X = 1, Y = 2 } };
        output.WriteLine($"[Struct indexer replacement] Before mutation: Updated in list: {points[0]}");

        // points[0].X = 99; // compile error — cannot modify the return value of indexer
        var updated = points[0];
        updated.X = 99;
        points[0] = updated; // must replace the entire element

        output.WriteLine($"[Struct indexer replacement] After mutation: Updated in list: {points[0]}");
        Assert.Equal(99, points[0].X);
    }

    // -----------------------------------------------------------------------
    // 5. DEFENSIVE COPYING
    // -----------------------------------------------------------------------

    [Fact]
    public void ReturningListDirectly_AllowsCallerToCorruptInternalState()
    {
        var store = new PointStoreWithList();
        output.WriteLine($"[No defensive copy] Before mutation: Count: {store.GetPointsUnsafe().Count}, First: {store.GetPointsUnsafe()[0]}");

        var leaked = store.GetPointsUnsafe();

        leaked.Add(new MutablePoint { X = 99, Y = 99 });
        leaked[0].X = 42;

        output.WriteLine($"[No defensive copy] After mutation: Internal state corrupted. Count: {store.GetPointsUnsafe().Count}, First: {store.GetPointsUnsafe()[0]}");
        Assert.Equal(2, store.GetPointsUnsafe().Count); // structure corrupted
        Assert.Equal(42, store.GetPointsUnsafe()[0].X); // item internals corrupted
    }

    [Fact]
    public void ReturningListCopy_IsolatesStructure_ButNotItemInternals()
    {
        var store = new PointStoreWithList();
        output.WriteLine($"[Shallow defensive copy] Before mutation: isolated Count: {store.GetPointsUnsafe().Count}, isolated Item X: {store.GetPointsUnsafe()[0].X}");

        var safeCopy = store.GetPointsSafe();

        safeCopy.Add(new MutablePoint { X = 99, Y = 99 }); // structural change isolated
        safeCopy[0].X = 42;                                 // item internal change is NOT isolated

        output.WriteLine($"[Shallow defensive copy] After mutation: isolated Count: {store.GetPointsUnsafe().Count}, isolated Item X: {store.GetPointsUnsafe()[0].X}");
        output.WriteLine($"[Shallow defensive copy] After mutation: safeCopy Count: {safeCopy.Count}, safeCopy Item X: {safeCopy[0].X}");
        Assert.Single(store.GetPointsUnsafe());     // structural change did not leak back
        Assert.Equal(42, store.GetPointsUnsafe()[0].X); // but item mutation did — shallow copy only
    }

    [Fact]
    public void ReturningReadOnlyCollection_PreventsStructuralMutation_ButNotItemMutation()
    {
        var store = new PointStoreWithList();
        output.WriteLine($"[ReadOnlyCollection from list store] Before mutation: item: {store.GetPointsUnsafe()[0]}");

        var readOnly = store.GetPointsReadOnly();

        // readOnly.Add(new MutablePoint { X = 99 }); // won't compile — no Add() on ReadOnlyCollection
        readOnly[0].X = 99; // item internals still mutable

        output.WriteLine($"[ReadOnlyCollection from list store] After mutation: Structure protected, item mutated: {readOnly[0]}");
        Assert.Equal(99, readOnly[0].X);
    }

    // -----------------------------------------------------------------------
    // 6. RECORD MUTATION BEHAVIOUR
    // -----------------------------------------------------------------------

    [Fact]
    public void List_OfRecordStruct_WithExpression_ProducesIndependentCopy()
    {
        var list = new List<ImmutableStructPoint> { new ImmutableStructPoint(1, 2) };
        output.WriteLine($"[record struct] Before mutation: Original: {list[0]}");

        var modified = list[0] with { X = 99 };

        output.WriteLine($"[record struct] After mutation: Original: {list[0]}, With-copy: {modified}");
        Assert.Equal(1, list[0].X);
        Assert.Equal(99, modified.X);
    }

    [Fact]
    public void List_OfMutableRecord_PropertyCanBeMutatedDirectly()
    {
        var list = new List<MutableRecord> { new MutableRecord { Name = "Alice", Age = 30 } };
        output.WriteLine($"[Mutable record in List] Before direct mutation: Item: {list[0]}");

        list[0].Age = 99; // List<T> does not prevent mutation of record properties

        output.WriteLine($"[Mutable record in List] After direct mutation: Item: {list[0]}");
        Assert.Equal(99, list[0].Age);
    }

    // -----------------------------------------------------------------------
    // 7. COMPILER AND RUNTIME ENFORCEMENT
    // -----------------------------------------------------------------------

    [Fact]
    public void IReadOnlyList_CanBeSubvertedAtRuntime_UnlikeImmutableCollection()
    {
        var original = new List<MutablePoint> { new MutablePoint { X = 1, Y = 2 } };
        IReadOnlyList<MutablePoint> readOnly = original;

        var castBack = readOnly as List<MutablePoint>;

        output.WriteLine($"[Runtime enforcement] IReadOnlyList cast back to List<T>: {(castBack is null ? "null — blocked" : "succeeded — mutable access gained")}");
        Assert.NotNull(castBack); // cast succeeds — IReadOnlyList provides no real runtime protection
    }

    [Fact]
    public void List_WithInitOnlyItems_ItemsCannotBeMutatedAfterCreation()
    {
        var list = new List<InitOnlyPoint> { new InitOnlyPoint { X = 1, Y = 2 } };

        // list[0].X = 99; // compile error — init-only property
        output.WriteLine($"[init-only in List] Item: {list[0]}, property cannot be set after init");
        Assert.Equal(1, list[0].X);
    }

    [Fact]
    public void List_VsImmutableArray_AfterSnapshot_OriginalChangesDoNotPropagate()
    {
        var source = new List<ImmutablePoint> { new ImmutablePoint(1, 2) };
        output.WriteLine($"[List vs ImmutableArray] Before mutation: List count: {source.Count}");

        var snapshot = source.ToImmutableArray(); // snapshot taken here

        source.Add(new ImmutablePoint(3, 4)); // mutate original list after snapshot

        output.WriteLine($"[List vs ImmutableArray] After mutatation: List count: {source.Count}, ImmutableArray count: {snapshot.Length}");
        Assert.Equal(2, source.Count);    // list grew
        Assert.Single(snapshot);          // snapshot is unaffected
    }

    // Helpers
    private static void MutateFirstElement(List<MutablePoint> points) => points[0].X = 99;
    private static void AddElementInMethod(List<MutablePoint> points) =>
        points.Add(new MutablePoint { X = 3, Y = 4 });
}

// -----------------------------------------------------------------------
// SUPPORTING TYPES FOR LIST TESTS
// -----------------------------------------------------------------------

public class ReadonlyListHolder
{
    public readonly List<MutablePoint> Points = [new MutablePoint { X = 1, Y = 2 }];
}

public class PointStoreWithList
{
    private readonly List<MutablePoint> _points = [new MutablePoint { X = 1, Y = 2 }];

    public List<MutablePoint> GetPointsUnsafe() => _points;
    public List<MutablePoint> GetPointsSafe() => new List<MutablePoint>(_points);
    public ReadOnlyCollection<MutablePoint> GetPointsReadOnly() => _points.AsReadOnly();
}
