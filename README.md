# SortLib

A small .NET sorting library with selectable algorithms and stable multi-key ordering.

## Supported algorithms

- `Bubble`
- `Selection`
- `Insertion`
- `Quick`
- `Merge`
- `Heap`

Each sort rule defines a strongly typed enum key (`SortParametr`), `SortType` (`Ascending` or `Descending`) and `SortKind`. Rules are supplied from primary to secondary. The engine applies them in reverse order and preserves existing order for equal keys, so even algorithms that are normally unstable can participate in deterministic multi-key sorting.

The library is UI- and model-independent. Consumers provide a selector that maps an item and its enum key to a comparable value.

## Example

```csharp
public enum PersonSortParametr
{
    LastName,
    FirstName,
    Age
}

var rules = new[]
{
    new SortRule<PersonSortParametr>(PersonSortParametr.LastName, SortType.Ascending, SortKind.Quick),
    new SortRule<PersonSortParametr>(PersonSortParametr.FirstName, SortType.Ascending, SortKind.Merge),
    new SortRule<PersonSortParametr>(PersonSortParametr.Age, SortType.Descending, SortKind.Heap)
};

List<Person> sorted = people.Sort(
    rules,
    (person, parameter) => parameter switch
    {
        PersonSortParametr.LastName => person.LastName,
        PersonSortParametr.FirstName => person.FirstName,
        PersonSortParametr.Age => person.Age,
        _ => null
    },
    PersonSortParametr.LastName);
```

When no rules are supplied, the consumer-provided default key is sorted ascending with merge sort.
