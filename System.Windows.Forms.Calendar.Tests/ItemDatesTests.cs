using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Changing an item's dates from code moves it on screen without an explicit layout call.
    /// </summary>
    [STATestClass]
    public class ItemDatesFromCodeTests : CalendarTestBase
    {
        private Rectangle UnitBounds(int hour) => Cal.GetTimeUnit(Monday.AddHours(hour)).Bounds;

        [TestMethod]
        public void ChangingDates_MovesItem()
        {
            Cal.SetViewRange(Monday, Monday);
            Cal.ScrollToTime(8);
            CalendarItem item = Timed(10, 11, "x");
            Cal.Items.Add(item);
            Assert.IsTrue(item.Bounds.IntersectsWith(UnitBounds(10)), "Test setup");

            item.StartDate = Monday.AddHours(14);
            item.EndDate = Monday.AddHours(15);

            Assert.IsTrue(item.Bounds.IntersectsWith(UnitBounds(14)), $"The item at {item.Bounds} is not at 14:00 {UnitBounds(14)}.");
            Assert.IsFalse(item.Bounds.IntersectsWith(UnitBounds(10)), "The item is still at 10:00.");
        }

        [TestMethod]
        public void ChangingDates_RequestsRepaint()
        {
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle);
            Cal.SetViewRange(Monday, Monday);
            CalendarItem item = Timed(10, 11, "x");
            Cal.Items.Add(item);
            bool repainted = false;
            Cal.Invalidated += (s, e) => repainted = true;

            item.EndDate = Monday.AddHours(12);

            Assert.IsTrue(repainted);
        }

        [TestMethod]
        public void ChangingTimedItemToAllDay_MovesItToDayTop()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(2));
            CalendarItem item = Timed(10, 11, "x");
            Cal.Items.Add(item);
            int minimum = Cal.Renderer.DayTopMinHeight;

            item.StartDate = Monday;
            item.EndDate = Monday.AddDays(2).AddSeconds(-1);

            Assert.IsTrue(Cal.Days[0].DayTop.Bounds.Contains(item.Bounds.Location), $"Item {item.Bounds} is not on the day top {Cal.Days[0].DayTop.Bounds}.");
            Assert.IsTrue(Cal.Renderer.DayTopHeight > minimum, "The day top did not grow.");
        }

        [TestMethod]
        public void ChangingManyItems_LaysOutOnceWhenNeeded()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            var items = new CalendarItem[50];
            for (int i = 0; i < items.Length; i++)
                items[i] = new CalendarItem(Cal, Monday.AddHours(8 + i % 10), Monday.AddHours(9 + i % 10), "x" + i);
            Cal.Items.AddRange(items);

            int layouts = 0;
            Cal.ItemsPositioned += (s, e) => layouts++;

            foreach (CalendarItem item in items)
            {
                item.StartDate = item.StartDate.AddDays(1);
                item.EndDate = item.EndDate.AddDays(1);
            }

            Assert.AreEqual(0, layouts, "Each date change laid the items out.");

            Rectangle unused = items[0].Bounds;

            Assert.AreEqual(1, layouts, "Reading positions should lay the items out once.");
            Assert.IsTrue(Cal.Days[1].Bounds.Contains(items[0].Bounds.Location), "The item did not move to the next day.");
        }

        [TestMethod]
        public void HitTest_AfterChangingDates_FindsItemAtNewPlace()
        {
            Cal.SetViewRange(Monday, Monday);
            Cal.ScrollToTime(8);
            CalendarItem item = Timed(10, 11, "x");
            Cal.Items.Add(item);

            item.StartDate = Monday.AddHours(14);
            item.EndDate = Monday.AddHours(15);
            Rectangle at14 = UnitBounds(14);

            Assert.AreSame(item, Cal.ItemAt(new Point(at14.Left + at14.Width / 2, at14.Top + at14.Height / 2)));
        }
    }

    /// <summary>
    /// The time shown on items follows the culture unless <see cref="Calendar.ItemsTimeFormat"/> is set.
    /// </summary>
    [STATestClass]
    public class ItemsTimeFormatTests : CalendarTestBase
    {
        private CultureInfo _oldCulture;

        [TestInitialize]
        public void RememberCulture() => _oldCulture = Thread.CurrentThread.CurrentCulture;

        [TestCleanup]
        public void RestoreCulture() => Thread.CurrentThread.CurrentCulture = _oldCulture;

        private CalendarItem ItemInMonthView()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            CalendarItem item = new CalendarItem(Cal, Monday.AddDays(2).AddHours(15), Monday.AddDays(2).AddHours(16), "x");
            Cal.Items.Add(item);
            return item;
        }

        [TestMethod]
        public void Default_IsEmpty()
        {
            Assert.IsNull(Cal.ItemsTimeFormat);
        }

        [TestMethod]
        [DataRow("pl-PL", "15:00")]
        [DataRow("de-DE", "15:00")]
        [DataRow("en-US", "3:00 PM")]
        public void WithoutFormat_UsesCultureShortTime(string culture, string expected)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            Assert.AreEqual(expected, ItemInMonthView().StartDateText);
        }

        [TestMethod]
        public void SetFormat_IsUsed()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");
            Cal.ItemsTimeFormat = "hh:mm tt";

            Assert.AreEqual(Monday.AddHours(15).ToString("hh:mm tt"), ItemInMonthView().StartDateText);
        }
    }
}
