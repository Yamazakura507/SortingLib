namespace SortLib.Enums;

/// <summary>
/// Selects the comparison-based sorting algorithm.
/// </summary>
public enum SortKind
{
    /// <summary>
    /// Bubble sort; simple and quadratic.
    /// </summary>
    Bubble,

    /// <summary>
    /// Selection sort; quadratic and in-place.
    /// </summary>
    Selection,

    /// <summary>
    /// Insertion sort; efficient for small or nearly sorted inputs.
    /// </summary>
    Insertion,

    /// <summary>
    /// Quick sort; average O(n log n) time.
    /// </summary>
    Quick,

    /// <summary>
    /// Merge sort; stable O(n log n) time.
    /// </summary>
    Merge,

    /// <summary>
    /// Heap sort; O(n log n) time and in-place.
    /// </summary>
    Heap
}
