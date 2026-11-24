using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace System.Windows.Forms.Calendar
{
    /// <summary>
    /// Items of a <see cref="Calendar"/>. Every change updates the layout and repaints the calendar.
    /// </summary>
    /// <remarks>
    /// The class derives from <see cref="List{T}"/>, whose methods can't be overridden, so the changing
    /// methods are redeclared with <c>new</c>. The interfaces below are declared again (interface
    /// re-implementation), so that changes made through <see cref="IList{T}"/>, <see cref="ICollection{T}"/>
    /// or <see cref="IList"/> (e.g. by data binding) also go through these methods instead of
    /// the base <see cref="List{T}"/> ones. Only code that casts the collection to <see cref="List{T}"/>
    /// itself still bypasses them.
    /// </remarks>
    public class CalendarItemCollection
        : List<CalendarItem>, IList<CalendarItem>, ICollection<CalendarItem>, IList
    {
        #region Events

        #endregion

        #region Fields
        private Calendar _calendar;
        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new collection that will belong to the specified calendar.
        /// </summary>
        /// <param name="c">Calendar this collection will belong to.</param>
        internal CalendarItemCollection(Calendar c)
        {
            _calendar = c;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the calendar this collection belongs to
        /// </summary>
        public Calendar Calendar
        {
            get { return _calendar; }
        }

        /// <summary>
        /// Gets or sets the item at the specified index. Setting it updates the layout.
        /// </summary>
        /// <param name="index">The zero-based index of the item.</param>
        public new CalendarItem this[int index]
        {
            get { return base[index]; }
            set { base[index] = value; CollectionChanged(); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an item to the end of the list
        /// </summary>
        /// <param name="item">The object to be added to the end of the collection. The value can be null for reference types.</param>
        public new void Add(CalendarItem item)
        {
            base.Add(item); CollectionChanged();
        }

        /// <summary>
        /// Adds the items of the specified collection to the end of the list.
        /// </summary>
        /// <param name="items">The items whose elements should be added to the end of the collection. The collection itself cannont be null, but it can contain elements that are null.</param>
        public new void AddRange(IEnumerable<CalendarItem> items)
        {
            base.AddRange(items); CollectionChanged();
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public new void Clear()
        {
            base.Clear(); CollectionChanged();
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert. The value can be null for reference types.</param>
        public new void Insert(int index, CalendarItem item)
        {
            base.Insert(index, item); CollectionChanged();
        }

        /// <summary>
        /// Inserts the items of a collection into this collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="items"></param>
        public new void InsertRange(int index, IEnumerable<CalendarItem> items)
        {
            base.InsertRange(index, items); CollectionChanged();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// <param name="item">The item to remove from the collection. The value can be null for reference types.</param>
        /// <returns><c>true</c> if item is successfully removed; otherwise, <c>false</c>. This method also returns false if item was not found in the collection.</returns>
        public new bool Remove(CalendarItem item)
        {
            bool result = base.Remove(item);

            CollectionChanged();

            return result;
        }

        /// <summary>
        /// Removes the item at the specified index of the collection
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <returns></returns>
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index); CollectionChanged();
        }

        /// <summary>
        /// Removes the all the items that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The Predicate delegate that defines the conditions of the items to remove.</param>
        /// <returns>The number of items removed from the collection.</returns>
        public new int RemoveAll(Predicate<CalendarItem> match)
        {
            int result = base.RemoveAll(match);

            CollectionChanged();

            return result;
        }

        /// <summary>
        /// Removes a range of elements from the collection
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of items to remove.</param>
        /// <param name="count">The number of items to remove</param>
        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count); CollectionChanged();
        }

        /// <summary>
        /// Reverses the order of the items and updates the layout (the order decides which items
        /// are drawn on top and how ties are placed).
        /// </summary>
        public new void Reverse()
        {
            base.Reverse(); CollectionChanged();
        }

        /// <summary>
        /// Reverses the order of the items in the specified range and updates the layout.
        /// </summary>
        public new void Reverse(int index, int count)
        {
            base.Reverse(index, count); CollectionChanged();
        }

        /// <summary>
        /// Sorts the items using the default comparer and updates the layout.
        /// </summary>
        public new void Sort()
        {
            base.Sort(); CollectionChanged();
        }

        /// <summary>
        /// Sorts the items using the specified comparison and updates the layout.
        /// </summary>
        public new void Sort(Comparison<CalendarItem> comparison)
        {
            base.Sort(comparison); CollectionChanged();
        }

        /// <summary>
        /// Sorts the items using the specified comparer and updates the layout.
        /// </summary>
        public new void Sort(IComparer<CalendarItem> comparer)
        {
            base.Sort(comparer); CollectionChanged();
        }

        /// <summary>
        /// Sorts the items in the specified range using the specified comparer and updates the layout.
        /// </summary>
        public new void Sort(int index, int count, IComparer<CalendarItem> comparer)
        {
            base.Sort(index, count, comparer); CollectionChanged();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Reverses the order without updating the layout. Used by the layout itself
        /// (<see cref="CalendarRenderer.PerformItemsLayout"/>), where <see cref="Reverse()"/> would recurse.
        /// </summary>
        internal void ReverseWithoutLayout()
        {
            base.Reverse();
        }

        #endregion

        #region Non-generic IList

        // List<T> implements these explicitly, so without these re-implementations they would
        // change the list without updating the layout.

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = ToItem(value); }
        }

        int IList.Add(object value)
        {
            Add(ToItem(value));
            return Count - 1;
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, ToItem(value));
        }

        void IList.Remove(object value)
        {
            CalendarItem item = value as CalendarItem;

            if (item != null)
                Remove(item);
        }

        /// <summary>
        /// Converts a value passed through <see cref="IList"/> to an item. Like <see cref="List{T}"/>,
        /// accepts <c>null</c> and throws <see cref="ArgumentException"/> for a value of another type.
        /// </summary>
        private static CalendarItem ToItem(object value)
        {
            if (value == null || value is CalendarItem)
                return (CalendarItem)value;

            throw new ArgumentException(
                $"The value \"{value}\" is not of type \"{typeof(CalendarItem)}\" and cannot be used in this generic collection.",
                nameof(value));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles what to do when this collection changes
        /// </summary>
        private void CollectionChanged()
        {
            Calendar.Renderer.PerformItemsLayout();
            Calendar.Invalidate();
        }

        #endregion
    }
}
