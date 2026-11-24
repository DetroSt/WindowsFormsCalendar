using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Placement of items in the layout matrices (CalendarRenderer.PerformItemsLayout).
    /// </summary>
    [STATestClass]
    public class ItemLayoutTests : CalendarTestBase
    {
        // Four all-day items that need 2 rows, but only fit if placed by start date:
        //   day 0  1  2  3
        //       A  .  .  B     A = day 0, B = day 3
        //       .  C  C  C     C = days 1-3
        //       D  D           D = days 0-1
        private static readonly (string Text, int First, int Last)[] GridExample =
        {
            ("A", 0, 0), ("B", 3, 3), ("C", 1, 3), ("D", 0, 1)
        };

        [TestMethod]
        public void MonthView_ItemsThatOnlyFitWhenSorted_AreAllPlaced()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            Assert.AreEqual(CalendarDaysMode.Short, Cal.DaysMode);

            // Short mode places items in reverse collection order; add them reversed
            // so the unsorted order is A, B, C, D.
            for (int i = GridExample.Length - 1; i >= 0; i--)
                Cal.Items.Add(AllDay(GridExample[i].First, GridExample[i].Last, GridExample[i].Text));
            Cal.Renderer.PerformItemsLayout();

            foreach (CalendarItem item in Cal.Items)
                Assert.IsFalse(item.Bounds.IsEmpty, $"Item {item.Text} was not placed.");
        }

        [TestMethod]
        public void WeekView_DayTopItemsThatOnlyFitWhenSorted_StayInsideDayTop()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Assert.AreEqual(CalendarDaysMode.Expanded, Cal.DaysMode);

            foreach (var g in GridExample)
                Cal.Items.Add(AllDay(g.First, g.Last, g.Text));
            Cal.Renderer.PerformItemsLayout();

            Rectangle dayTop = Cal.Days[0].DayTop.Bounds;
            foreach (CalendarItem item in Cal.Items)
            {
                Assert.IsTrue(item.Bounds.Top >= dayTop.Top && item.Bounds.Bottom <= dayTop.Bottom,
                    $"Item {item.Text} at {item.Bounds} is outside the day top {dayTop}.");
            }
        }

        [TestMethod]
        public void DayView_FiveMeetingsThatOverflowedTheMatrix_DoNotThrow()
        {
            Cal.SetViewRange(Monday, Monday);

            // This order used to throw IndexOutOfRangeException on the fifth Add.
            foreach (var (start, end) in new[] { (8, 11), (12, 14), (11, 15), (9, 13), (10, 12) })
                Cal.Items.Add(Timed(start, end, $"{start}-{end}"));

            foreach (CalendarItem item in Cal.Items)
                Assert.IsFalse(item.Bounds.IsEmpty, $"Item {item.Text} was not placed.");
        }

        [TestMethod]
        public void RemovedItem_IsNoLongerCountedInItsTimeUnit()
        {
            Cal.SetViewRange(Monday, Monday);
            CalendarItem removed = Timed(10, 11, "removed");
            Cal.Items.Add(removed);
            Cal.Items.Remove(removed);
            Cal.Items.Add(Timed(10, 11, "kept"));
            Cal.Renderer.PerformItemsLayout();

            CalendarTimeScaleUnit unit = Cal.GetTimeUnit(Monday.AddHours(10));
            Assert.AreEqual(1, unit.PassingItems.Count);
        }

        [TestMethod]
        public void RandomMeetings_NeverThrowOverlapOrGetLost()
        {
            var random = new Random(12345);

            for (int trial = 0; trial < 300; trial++)
            {
                // A fresh calendar per trial, so state from earlier trials cannot hide problems.
                Form form;
                TestCalendar cal = CreateCalendar(out form);
                try
                {
                    cal.SetViewRange(Monday, Monday);
                    var spans = new List<(CalendarItem Item, int Start, int End)>();

                    for (int k = random.Next(3, 9); k > 0; k--)
                    {
                        int start = 8 + random.Next(0, 10), end = start + random.Next(1, 5);
                        var item = new CalendarItem(cal, Monday.AddHours(start), Monday.AddHours(end), $"{start}-{end}");
                        cal.Items.Add(item);
                        spans.Add((item, start, end));
                    }

                    foreach (var a in spans)
                    {
                        Assert.IsFalse(a.Item.Bounds.IsEmpty, $"Trial {trial}: item {a.Item.Text} was not placed.");

                        foreach (var b in spans)
                        {
                            bool overlapInTime = a.Start < b.End && b.Start < a.End;
                            if (!ReferenceEquals(a.Item, b.Item) && overlapInTime)
                                Assert.IsFalse(a.Item.Bounds.IntersectsWith(b.Item.Bounds),
                                    $"Trial {trial}: items {a.Item.Text} and {b.Item.Text} overlap on screen.");
                        }
                    }
                }
                finally
                {
                    form.Dispose();
                }
            }
        }

        [TestMethod]
        public void RandomAllDayItems_InWeekView_StayInsideDayTop()
        {
            var random = new Random(23456);

            for (int trial = 0; trial < 200; trial++)
            {
                Cal.Items.Clear();
                Cal.SetViewRange(Monday, Monday.AddDays(6));

                for (int k = random.Next(3, 10); k > 0; k--)
                {
                    int first = random.Next(0, 7), last = Math.Min(6, first + random.Next(0, 4));
                    Cal.Items.Add(AllDay(first, last, k.ToString()));
                }
                Cal.Renderer.PerformItemsLayout();

                Rectangle dayTop = Cal.Days[0].DayTop.Bounds;
                foreach (CalendarItem item in Cal.Items)
                {
                    Assert.IsFalse(item.Bounds.IsEmpty, $"Trial {trial}: item was not placed.");
                    Assert.IsTrue(item.Bounds.Top >= dayTop.Top && item.Bounds.Bottom <= dayTop.Bottom,
                        $"Trial {trial}: item at {item.Bounds} is outside the day top {dayTop}.");
                }
            }
        }

        [TestMethod]
        public void RandomAllDayItems_InMonthView_AreShownOrMarkedAsOverflow()
        {
            var random = new Random(34567);
            Form form;
            TestCalendar cal = CreateCalendar(out form, height: 2000);

            try
            {
                for (int trial = 0; trial < 200; trial++)
                {
                    cal.Items.Clear();
                    cal.SetViewRange(Monday, Monday.AddDays(27));

                    for (int k = random.Next(3, 9); k > 0; k--)
                    {
                        int first = random.Next(0, 6), last = Math.Min(6, first + random.Next(0, 4));
                        cal.Items.Add(new CalendarItem(cal, Monday.AddDays(first), Monday.AddDays(last + 1).AddSeconds(-1), k.ToString()));
                    }
                    cal.Renderer.PerformItemsLayout();

                    foreach (CalendarItem item in cal.Items)
                    {
                        if (!item.Bounds.IsEmpty) continue;

                        bool overflowMarked = false;
                        for (int d = item.DayStart.Index; d <= item.DayEnd.Index; d++)
                            overflowMarked |= cal.Days[d].OverflowEnd;

                        Assert.IsTrue(overflowMarked, $"Trial {trial}: item disappeared without an overflow mark.");
                    }
                }
            }
            finally
            {
                form.Dispose();
            }
        }
    }
}
