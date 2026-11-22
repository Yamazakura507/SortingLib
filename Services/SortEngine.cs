using SortLib.Enums;
using SortLib.Interfaces;
using SortLib.Models;

namespace SortLib.Services;

/// <summary>
/// Executes single- or multi-key sorting with selectable algorithms. Multi-key rules are
/// applied from the least significant key to the primary key using stable tie handling.
/// </summary>
public static class SortEngine
{
    /// <summary>
    /// Sorts a sequence without modifying its source.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TParameter">The enum identifying sortable properties.</typeparam>
    /// <param name="source">Elements to sort.</param>
    /// <param name="rules">Sort rules ordered from primary to secondary.</param>
    /// <param name="keySelector">Returns the value represented by a rule for an element.</param>
    /// <param name="defaultParameter">The key used when no explicit rules are supplied.</param>
    /// <returns>A new list sorted according to all configured rules.</returns>
    /// <remarks>
    /// Each algorithm uses the existing order within equal keys, so multi-key sorting remains
    /// deterministic even for algorithms that are not inherently stable.
    /// </remarks>
    public static List<T> Sort<T, TParameter>(
        this IEnumerable<T> source,
        IEnumerable<ISortRule<TParameter>>? rules,
        Func<T, TParameter, object?> keySelector,
        TParameter defaultParameter) where TParameter : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        List<T> result = source.ToList();

        ISortRule<TParameter>[] configuredRules = rules?.ToArray() ?? [];

        if (configuredRules.Length == 0)
        {
            configuredRules = [ new SortRule<TParameter>(defaultParameter, SortType.Ascending, SortKind.Merge) ];
        }

        for (int ruleIndex = configuredRules.Length - 1; ruleIndex >= 0; ruleIndex--)
        {
            ISortRule<TParameter> rule = configuredRules[ruleIndex];
            List<SortEntry<T>> indexed = result
                .Select((item, index) => new SortEntry<T>(item, keySelector(item, rule.SortParametr), index))
                .ToList();

            Comparison<SortEntry<T>> comparison = (left, right) =>
            {
                int value = CompareKeys(left.Key, right.Key);

                if (rule.SortType == SortType.Descending) value = -Math.Sign(value);

                return value != 0 ? value : left.Index.CompareTo(right.Index);
            };

            indexed.SortEntries(comparison, rule.SortKind);

            result = indexed.Select(entry => entry.Item).ToList();
        }

        return result;
    }

    private static int CompareKeys(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return 0;
        else if (left is null) return -1;
        else if (right is null) return 1;
        else if (left is string leftText && right is string rightText)
            return StringComparer.CurrentCultureIgnoreCase.Compare(leftText, rightText);
        else return Comparer<object>.Default.Compare(left, right);
    }

    private static void SortEntries<T>(this List<SortEntry<T>> items, Comparison<SortEntry<T>> compare, SortKind kind)
    {
        switch (kind)
        {
            case SortKind.Bubble:
                items.BubbleSort(compare);
                break;
            case SortKind.Selection:
                items.SelectionSort(compare);
                break;
            case SortKind.Insertion:
                items.InsertionSort(compare);
                break;
            case SortKind.Quick:
                items.QuickSort(compare);
                break;
            case SortKind.Heap:
                items.HeapSort(compare);
                break;
            case SortKind.Merge:
            default:
                items.MergeSort(compare);
                break;
        }
    }

    private static void BubbleSort<T>(this List<T> items, Comparison<T> compare)
    {
        for (int end = items.Count - 1; end > 0; end--)
        {
            bool changed = false;

            for (int index = 0; index < end; index++)
            {
                if (compare(items[index], items[index + 1]) <= 0) continue;

                (items[index], items[index + 1]) = (items[index + 1], items[index]);
                changed = true;
            }

            if (!changed) break;
        }
    }

    private static void SelectionSort<T>(this List<T> items, Comparison<T> compare)
    {
        for (int start = 0; start < items.Count - 1; start++)
        {
            int minimum = start;

            for (int index = start + 1; index < items.Count; index++)
            {
                if (compare(items[index], items[minimum]) < 0) minimum = index;
            }

            if (minimum != start) (items[start], items[minimum]) = (items[minimum], items[start]);
        }
    }

    private static void InsertionSort<T>(this List<T> items, Comparison<T> compare)
    {
        for (int index = 1; index < items.Count; index++)
        {
            T value = items[index];
            int insertionIndex = index - 1;

            while (insertionIndex >= 0 && compare(items[insertionIndex], value) > 0)
            {
                items[insertionIndex + 1] = items[insertionIndex];

                insertionIndex--;
            }

            items[insertionIndex + 1] = value;
        }
    }

    private static void QuickSort<T>(this List<T> items, Comparison<T> compare)
    {
        if (items.Count < 2) return;

        Stack<(int Left, int Right)> ranges = new();

        ranges.Push((0, items.Count - 1));

        while (ranges.Count > 0)
        {
            (int left, int right) = ranges.Pop();
            int i = left;
            int j = right;
            T pivot = items[left + ((right - left) / 2)];

            while (i <= j)
            {
                while (compare(items[i], pivot) < 0) i++;
                while (compare(items[j], pivot) > 0) j--;

                if (i > j) continue;

                (items[i], items[j]) = (items[j], items[i]);

                i++;
                j--;
            }

            if (left < j) ranges.Push((left, j));
            if (i < right) ranges.Push((i, right));
        }
    }

    private static void MergeSort<T>(this List<T> items, Comparison<T> compare)
    {
        int count = items.Count;

        if (count < 2) return;

        T[] source = items.ToArray();
        T[] buffer = new T[count];

        for (int width = 1; width < count; width = width > count / 2 ? count : width * 2)
        {
            for (int start = 0; start < count; start += width * 2)
            {
                int middle = Math.Min(start + width, count);
                int end = Math.Min(start + (width * 2), count);
                int left = start;
                int right = middle;

                for (int target = start; target < end; target++)
                {
                    buffer[target] = source[left < middle && (right >= end || compare(source[left], source[right]) <= 0) ? left++ : right++];
                }
            }

            (source, buffer) = (buffer, source);
        }

        for (int index = 0; index < count; index++) items[index] = source[index];
    }

    private static void HeapSort<T>(this List<T> items, Comparison<T> compare)
    {
        int count = items.Count;

        for (int root = (count / 2) - 1; root >= 0; root--) items.SiftDown(root, count, compare);

        for (int end = count - 1; end > 0; end--)
        {
            (items[0], items[end]) = (items[end], items[0]);

            items.SiftDown(0, end, compare);
        }
    }

    private static void SiftDown<T>(this List<T> items, int root, int count, Comparison<T> compare)
    {
        while (true)
        {
            int child = (root * 2) + 1;

            if (child >= count) return;
            if (child + 1 < count && compare(items[child], items[child + 1]) < 0) child++;
            if (compare(items[root], items[child]) >= 0) return;

            (items[root], items[child]) = (items[child], items[root]);
            root = child;
        }
    }
}
