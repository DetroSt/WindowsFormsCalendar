using System;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Scrolling the time units of the day / week view.
    /// </summary>
    [STATestClass]
    public class ScrollTests : CalendarTestBase
    {
        [TestInitialize]
        public void ShowOneWeek()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Assert.AreEqual(CalendarTimeScale.ThirtyMinutes, Cal.TimeScale);
        }

        private int UnitsPerHour => 60 / (int)Cal.TimeScale;

        private int MaxUnitsFromMidnight =>
            Cal.Days[0].TimeUnits.Length - Math.Max(1, Cal.Renderer.GetVisibleTimeUnits());

        [TestMethod]
        public void ScrollToTime_PutsThatTimeAtTheTop()
        {
            Cal.ScrollToTime(8);

            Assert.AreEqual(-8 * UnitsPerHour, Cal.TimeUnitsOffset);
        }

        [TestMethod]
        public void ScrollToTime_SameTimeTwice_GivesSamePosition()
        {
            Cal.ScrollToTime(8);
            int first = Cal.TimeUnitsOffset;

            Cal.ScrollToTime(8);

            Assert.AreEqual(first, Cal.TimeUnitsOffset);
        }

        [TestMethod]
        public void ScrollToTime_DoesNotDependOnCurrentPosition()
        {
            Cal.ScrollToTime(10);
            Cal.ScrollToTime(6.5f);

            Assert.AreEqual(-(int)(6.5 * UnitsPerHour), Cal.TimeUnitsOffset);
        }

        [TestMethod]
        public void ScrollToTime_Midnight_ScrollsToTop()
        {
            Cal.ScrollToTime(12);

            Cal.ScrollToTime(0);

            Assert.AreEqual(0, Cal.TimeUnitsOffset);
        }

        [TestMethod]
        public void ScrollToTime_LateEvening_StopsAtLastUnit()
        {
            Cal.ScrollToTime(23.5f);

            Assert.AreEqual(-MaxUnitsFromMidnight, Cal.TimeUnitsOffset);
        }

        [TestMethod]
        public void ScrollToTime_Negative_ScrollsToTop()
        {
            Cal.ScrollToTime(-3);

            Assert.AreEqual(0, Cal.TimeUnitsOffset);
        }
    }
}
