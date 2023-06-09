using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace System.Windows.Forms.Calendar
{
    /// <summary>
    /// Represents a week displayed on the <see cref="Calendar"/>
    /// </summary>
    public class CalendarWeek
    {
        #region Events

        #endregion

        #region Fields
        private Rectangle _bounds;
        private Calendar _calendar;
        private DateTime _firstDay;
        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new week for the specified calendar
        /// </summary>
        /// <param name="calendar">Calendar this week belongs to</param>
        /// <param name="sunday">Start day of the week</param>
        internal CalendarWeek(Calendar calendar, DateTime firstDay)
        {
            _calendar = calendar;
            _firstDay = firstDay;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the bounds of the week
        /// </summary>
        public Rectangle Bounds
        {
            get { return _bounds; }
        }

        /// <summary>
        /// Gets the calendar this week belongs to
        /// </summary>
        public Calendar Calendar
        {
            get { return _calendar; }
        }

        /// <summary>
        /// Gets the bounds of the week header
        /// </summary>
        public Rectangle HeaderBounds
        {
            get 
            {
                return new Rectangle(
                    Bounds.Left, 
                    Bounds.Top + Calendar.Renderer.DayHeaderHeight, 
                    Calendar.Renderer.WeekHeaderWidth, 
                    Bounds.Height - Calendar.Renderer.DayHeaderHeight);
            }
        }

        /// <summary>
        /// Gets the sunday that starts the week
        /// </summary>
        public DateTime StartDate
        {
            get { return _firstDay; }
        }


        #endregion

        #region Public Methods
        /// <summary>
        /// Returns week number
        /// </summary>
        /// <returns></returns>
        public string ToStringWeekNumber()
        {
            System.Globalization.CultureInfo CUI = System.Globalization.CultureInfo.CurrentCulture;
            int iWeekNumber = CUI.Calendar.GetWeekOfYear(StartDate, CUI.DateTimeFormat.CalendarWeekRule, CUI.DateTimeFormat.FirstDayOfWeek);

            if (CUI.TwoLetterISOLanguageName == "de")
                return "KW " + iWeekNumber.ToString();
            else
                return "Week " + iWeekNumber.ToString();
        }

        /// <summary>
        /// Gets the short version of week's string representation
        /// </summary>
        /// <returns></returns>
        public string ToStringShort()
        {
            DateTime saturday = StartDate.AddDays(6);

            if (saturday.Month != StartDate.Month)
            {
                return string.Format("{0} - {1}",
                    StartDate.ToString("d/M"),
                    saturday.ToString("d/M")
                    );
            }
            else
            {
                return string.Format("{0} - {1}",
                    StartDate.Day,
                    saturday.ToString("d/M")
                    );
            }
        }

        /// <summary>
        /// Gets the large version of string representation
        /// </summary>
        /// <returns>The week in a string format</returns>
        public string ToStringLarge()
        {
            DateTime saturday = StartDate.AddDays(6);

            if (saturday.Month != StartDate.Month)
            {
                return string.Format("{0} - {1}",
                    StartDate.ToString("d MMM"),
                    saturday.ToString("d MMM")
                    );
            }
            else
            {
                return string.Format("{0} - {1}",
                    StartDate.Day,
                    saturday.ToString("d MMM")
                    );
            }
        }

        /// <summary>
        /// Returns a string representation of the week
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToStringLarge();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the value of the <see cref="Bounds"/> property
        /// </summary>
        /// <param name="r"></param>
        internal void SetBounds(Rectangle r)
        {
            _bounds = r;
        }

        #endregion
    }
}
