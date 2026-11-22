namespace SortLib.Models
{
    internal sealed record SortEntry<T>(T Item, object? Key, int Index);
}
