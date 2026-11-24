using System;

namespace System.Windows.Forms.Calendar
{
    /// <summary>
    /// A holiday on one day, as listed by <see cref="CalendarHolidays.GetHolidays"/>.
    /// </summary>
    public sealed class CalendarHoliday
    {
        /// <summary>
        /// Creates a holiday on the specified day.
        /// </summary>
        /// <param name="date">Day of the holiday (the time of day is ignored).</param>
        /// <param name="name">Name of the holiday.</param>
        public CalendarHoliday(DateTime date, string name)
        {
            Date = date.Date;
            Name = name ?? string.Empty;
        }

        /// <summary>
        /// Gets the day of the holiday.
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Gets the name of the holiday.
        /// </summary>
        public string Name { get; private set; }

        public override string ToString()
        {
            return Date.ToString("d") + " " + Name;
        }
    }
}
