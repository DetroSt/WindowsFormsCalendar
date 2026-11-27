using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Windows.Forms.Calendar
{
    /// <summary>
    /// Data for events related to several items at once, such as <see cref="Calendar.ItemsDatesChanged"/>.
    /// </summary>
    public class CalendarItemsEventArgs
        : EventArgs
    {
        private readonly ReadOnlyCollection<CalendarItem> _items;

        /// <summary>
        /// Creates a new <see cref="CalendarItemsEventArgs"/>
        /// </summary>
        /// <param name="items">Related items</param>
        public CalendarItemsEventArgs(IEnumerable<CalendarItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _items = new List<CalendarItem>(items).AsReadOnly();
        }

        /// <summary>
        /// Gets the items related to the event
        /// </summary>
        public ReadOnlyCollection<CalendarItem> Items
        {
            get { return _items; }
        }
    }
}
