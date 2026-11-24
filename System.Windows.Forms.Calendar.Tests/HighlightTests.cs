using System;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Highlighted (work) time set through <see cref="Calendar.HighlightRanges"/>.
    /// </summary>
    [STATestClass]
    public class HighlightTests : CalendarTestBase
    {
        private bool _repainted;

        [TestInitialize]
        public void ShowMonday()
        {
            // Invalidated is raised only for controls with a window handle (the test form is hidden).
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle);
            Cal.SetViewRange(Monday, Monday);
            Cal.Invalidated += (s, e) => _repainted = true;
        }

        private bool IsHighlighted(int hour)
        {
            return Cal.GetTimeUnit(Monday.AddHours(hour)).Highlighted;
        }

        [TestMethod]
        public void DefaultRanges_HighlightWorkHours()
        {
            Assert.IsTrue(IsHighlighted(9));
            Assert.IsFalse(IsHighlighted(18));
        }

        [TestMethod]
        public void ChangingTimesOfExistingRange_UpdatesHighlightsAndRepaints()
        {
            CalendarHighlightRange monday = Array.Find(Cal.HighlightRanges, r => r.DayOfWeek == DayOfWeek.Monday);
            _repainted = false;

            monday.EndTime = new TimeSpan(20, 0, 0);

            Assert.IsTrue(IsHighlighted(18));
            Assert.IsTrue(_repainted, "The calendar was not repainted.");
        }

        [TestMethod]
        public void AssigningNewRanges_Repaints()
        {
            _repainted = false;

            Cal.HighlightRanges = new[] { new CalendarHighlightRange(DayOfWeek.Monday, new TimeSpan(18, 0, 0), new TimeSpan(19, 0, 0)) };

            Assert.IsTrue(IsHighlighted(18));
            Assert.IsFalse(IsHighlighted(9));
            Assert.IsTrue(_repainted, "The calendar was not repainted.");
        }

        [TestMethod]
        public void RangeRemovedFromCalendar_NoLongerAffectsIt()
        {
            CalendarHighlightRange old = Cal.HighlightRanges[0];
            Cal.HighlightRanges = new CalendarHighlightRange[0];

            Assert.IsNull(old.Calendar);
        }

        [TestMethod]
        public void NullRanges_MeanNoHighlights()
        {
            Cal.HighlightRanges = null;

            Assert.IsNotNull(Cal.HighlightRanges);
            Assert.IsFalse(IsHighlighted(9));
        }

        [TestMethod]
        public void NullElementInRanges_IsIgnored()
        {
            Cal.HighlightRanges = new[] { null, new CalendarHighlightRange(DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) };

            Assert.IsTrue(IsHighlighted(9));
        }
    }
}
