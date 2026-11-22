using SortLib.Enums;
using System.ComponentModel;

namespace SortLib.Interfaces
{
    /// <summary>
    /// Common strongly typed contract consumed by <see cref="Services.SortEngine"/>.
    /// </summary>
    /// <typeparam name="TParameter">Enum identifying a property on the sorted object.</typeparam>
    public interface ISortRule<TParameter> : INotifyPropertyChanged where TParameter : struct, Enum
    {
        /// <summary>
        /// Gets the property selector associated with this rule.
        /// </summary>
        TParameter SortParametr { get; }

        /// <summary>
        /// Gets the direction used for this sort key.
        /// </summary>
        SortType SortType { get; }

        /// <summary>
        /// Gets the sorting algorithm used for this sort key.
        /// </summary>
        SortKind SortKind { get; }
    }
}
