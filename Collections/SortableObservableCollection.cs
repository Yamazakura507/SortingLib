using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using SortLib.Interfaces;
using SortLib.Models;
using SortLib.Services;

namespace SortLib.Collections;

/// <summary>
/// An observable collection that keeps its items ordered by configurable, multi-key rules.
/// Derived collections provide a typed enum selector and a default sort key.
/// </summary>
/// <typeparam name="TItem">The reference type stored in the collection.</typeparam>
/// <typeparam name="TParameter">Enum identifying sortable properties of the item.</typeparam>
/// <typeparam name="TRule">The strongly typed sort rule exposed to consumers.</typeparam>
public abstract class SortableObservableCollection<TItem, TParameter, TRule> : ObservableCollection<TItem>, IDisposable
    where TItem : class, INotifyPropertyChanged
    where TParameter : struct, Enum
    where TRule : SortRule<TParameter>, new()
{
    private readonly Func<TItem, TParameter, object?> keySelector;
    private readonly TParameter defaultSortParametr;
    private readonly HashSet<TRule> observedRules = new(ReferenceEqualityComparer.Instance);
    private bool isApplyingSort;
    private bool isDisposed;

    /// <summary>
    /// Gets the ordered list of sort rules. The first rule is the primary key.
    /// </summary>
    public ObservableCollection<TRule> SortRules { get; } = [];

    /// <summary>
    /// Initializes an empty collection with the specified selector and default key.
    /// </summary>
    protected SortableObservableCollection(Func<TItem, TParameter, object?> keySelector, TParameter defaultSortParametr)
    {
        this.keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        this.defaultSortParametr = defaultSortParametr;

        SortRules.CollectionChanged += SortRulesOnCollectionChanged;
    }

    /// <summary>
    /// Initializes the collection from items, then applies the default sort.
    /// </summary>
    protected SortableObservableCollection(
        IEnumerable<TItem> items, Func<TItem, TParameter, object?> keySelector, TParameter defaultSortParametr) 
        : this(keySelector, defaultSortParametr)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (TItem item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            Items.Add(item);

            item.PropertyChanged += ItemOnPropertyChanged;
        }

        ApplySort();
    }

    /// <summary>
    /// Releases item and sort-rule event subscriptions owned by this collection.
    /// The collection remains readable but no longer reacts to changes.
    /// </summary>
    public virtual void Dispose()
    {
        if (isDisposed) return;

        isDisposed = true;
        SortRules.CollectionChanged -= SortRulesOnCollectionChanged;

        foreach (TRule rule in observedRules) rule.PropertyChanged -= SortRuleOnPropertyChanged;

        observedRules.Clear();

        foreach (TItem item in this) item.PropertyChanged -= ItemOnPropertyChanged;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reapplies sorting after a property used by a custom key selector changed indirectly.
    /// Item property changes and sort-rule changes are observed automatically.
    /// </summary>
    public void RefreshSort() => ApplySort(notifyReset: true);

    /// <inheritdoc />
    protected override void InsertItem(int index, TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!isDisposed) item.PropertyChanged += ItemOnPropertyChanged;

        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        this[index].PropertyChanged -= ItemOnPropertyChanged;
        if (!isDisposed) item.PropertyChanged += ItemOnPropertyChanged;

        base.SetItem(index, item);
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        this[index].PropertyChanged -= ItemOnPropertyChanged;

        base.RemoveItem(index);
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        foreach (TItem item in this) item.PropertyChanged -= ItemOnPropertyChanged;

        base.ClearItems();
    }

    /// <inheritdoc />
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (!isApplyingSort && !isDisposed) ApplySort();
    }

    private void SortRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (TRule rule in observedRules) rule.PropertyChanged -= SortRuleOnPropertyChanged;

        observedRules.Clear();

        foreach (TRule rule in SortRules)
        {
            if (observedRules.Add(rule)) rule.PropertyChanged += SortRuleOnPropertyChanged;
        }

        ApplySort();
        OnSortRulesChanged();
    }

    private void SortRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplySort();
        OnSortRulesChanged();
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isDisposed) return;

        if (IsSortPropertyChanged(e)) ApplySort(notifyReset: true);
        OnItemPropertyChanged(e);
    }

    /// <summary>
    /// Determines whether the changed item property is used by the active sort rules.
    /// By default the property name must match the sort-parameter enum member name.
    /// Override this method when a selector maps an enum member to a differently named or
    /// computed property. An empty property name conservatively triggers a full sort.
    /// </summary>
    protected virtual bool IsSortPropertyChanged(PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return true;

        IEnumerable<TParameter> parameters = SortRules.Count == 0
            ? [defaultSortParametr]
            : SortRules.Select(rule => rule.SortParametr);

        return parameters.Any(parameter =>
            string.Equals(parameter.ToString(), e.PropertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Indicates that the collection is currently emitting collection-change events
    /// caused by an internal sort operation.
    /// </summary>
    protected bool IsApplyingSort => isApplyingSort;

    /// <summary>
    /// Called once after an observed item changes, after its sort order has been updated.
    /// </summary>
    protected virtual void OnItemPropertyChanged(PropertyChangedEventArgs e) { }

    /// <summary>
    /// Called once after the sort-rule collection changes and the source order is updated.
    /// </summary>
    protected virtual void OnSortRulesChanged() { }

    private void ApplySort(bool notifyReset = false)
    {
        if (isDisposed || isApplyingSort) return;

        if (Count < 2)
        {
            if (notifyReset) base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            return;
        }

        List<TItem> sorted = this.Sort(SortRules.Cast<ISortRule<TParameter>>(), keySelector, defaultSortParametr);

        isApplyingSort = true;

        try
        {
            for (int targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
            {
                if (ReferenceEquals(this[targetIndex], sorted[targetIndex])) continue;

                int currentIndex = IndexOfReference(sorted[targetIndex], targetIndex + 1);

                if (currentIndex >= 0) Move(currentIndex, targetIndex);
            }
        }
        finally
        {
            isApplyingSort = false;
        }

        if (notifyReset) base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private int IndexOfReference(TItem item, int startIndex)
    {
        for (int index = startIndex; index < Count; index++)
        {
            if (ReferenceEquals(this[index], item)) return index;
        }

        return -1;
    }
}
