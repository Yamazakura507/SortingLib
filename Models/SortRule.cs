using System.ComponentModel;
using System.Runtime.CompilerServices;
using SortLib.Enums;
using SortLib.Interfaces;

namespace SortLib.Models;

/// <summary>
/// Describes one key in a possibly multi-key sort. Rules are evaluated in collection order,
/// with the first rule being the primary key.
/// </summary>
/// <typeparam name="TParameter">Enum identifying the property used as the sort key.</typeparam>
public class SortRule<TParameter> : ISortRule<TParameter> where TParameter : struct, Enum
{
    private TParameter sortParametr;
    private SortType sortType = SortType.Ascending;
    private SortKind sortKind = SortKind.Merge;

    /// <summary>
    /// Gets or sets the enum value that identifies the object property used by this rule.
    /// The historical spelling <c>SortParametr</c> is retained as the public API name.
    /// </summary>
    public TParameter SortParametr
    {
        get => sortParametr;
        set => SetField(ref sortParametr, value);
    }

    /// <summary>
    /// Gets or sets the ordering direction for this key.
    /// </summary>
    public SortType SortType
    {
        get => sortType;
        set => SetField(ref sortType, value);
    }

    /// <summary>
    /// Gets or sets the algorithm used to sort this key.
    /// </summary>
    public SortKind SortKind
    {
        get => sortKind;
        set => SetField(ref sortKind, value);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a rule with its default values.
    /// </summary>
    public SortRule() { }

    /// <summary>
    /// Initializes a rule for the supplied key, direction and algorithm.
    /// </summary>
    public SortRule(TParameter sortParametr, SortType sortType = SortType.Ascending, SortKind sortKind = SortKind.Merge)
    {
        this.sortParametr = sortParametr;
        this.sortType = sortType;
        this.sortKind = sortKind;
    }

    private bool SetField<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value)) return false;

        field = value;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        return true;
    }
}


