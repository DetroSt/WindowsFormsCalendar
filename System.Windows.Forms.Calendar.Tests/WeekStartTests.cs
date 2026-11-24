using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Week layouts for every possible first day of the week (e.g. Monday in Poland).
    /// </summary>
    [STATestClass]
    public class WeekStartTests : CalendarTestBase
    {
        public static IEnumerable<object[]> AllFirstDays()
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                yield return new object[] { day };
        }

        [TestMethod]
        [DynamicData(nameof(AllFirstDays))]
        public void CalendarMonthView_AlwaysShowsViewStart_AndStartsOnFirstDayOfWeek(DayOfWeek firstDay)
        {
            Cal.FirstDayOfWeek = firstDay;

            // Every weekday as the view start.
            for (int offset = 0; offset < 7; offset++)
            {
                DateTime start = Monday.AddDays(offset);
                Cal.SetViewRange(start, start.AddDays(20));

                Assert.AreEqual(CalendarDaysMode.Short, Cal.DaysMode);
                Assert.IsNotNull(Cal.FindDay(start), $"View start {start:ddd dd.MM} is not shown (first day {firstDay}).");
                Assert.AreEqual(firstDay, Cal.Days[0].Date.DayOfWeek);
            }
        }

        [TestMethod]
        [DynamicData(nameof(AllFirstDays))]
        public void MonthViewGrid_ContainsFirstOfMonth_AndStartsOnFirstDayOfWeek(DayOfWeek firstDay)
        {
            var monthView = new MonthView { FirstDayOfWeek = firstDay };

            // Every month of 2026, so the 1st falls on each weekday at least once.
            for (int month = 1; month <= 12; month++)
            {
                DateTime first = new DateTime(2026, month, 1);
                var grid = new MonthViewMonth(monthView, first);

                Assert.AreEqual(firstDay, grid.Days[0].Date.DayOfWeek);
                Assert.IsTrue(Array.Exists(grid.Days, d => d.Date == first),
                    $"Grid for {first:MMMM yyyy} does not contain the 1st (first day {firstDay}).");
            }
        }

        [TestMethod]
        [DynamicData(nameof(AllFirstDays))]
        public void MonthViewSelectWeek_SelectsTheWeekContainingTheClickedDay(DayOfWeek firstDay)
        {
            var monthView = new MonthView { FirstDayOfWeek = firstDay };
            MethodInfo selectWeek = typeof(MonthView).GetMethod("SelectWeek", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int offset = 0; offset < 7; offset++)
            {
                DateTime clicked = Monday.AddDays(offset);
                selectWeek.Invoke(monthView, new object[] { clicked });

                Assert.AreEqual(firstDay, monthView.SelectionStart.DayOfWeek);
                Assert.IsTrue(monthView.SelectionStart <= clicked && clicked <= monthView.SelectionEnd,
                    $"Clicked {clicked:ddd dd.MM}, selected {monthView.SelectionStart:dd.MM}-{monthView.SelectionEnd:dd.MM} (first day {firstDay}).");
            }
        }
    }
}
