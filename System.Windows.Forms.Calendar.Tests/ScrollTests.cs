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
            Math.Max(0, Cal.Days[0].TimeUnits.Length - Math.Max(1, Cal.Renderer.GetVisibleTimeUnits()));

        /// <summary>
        /// Makes the view short enough that even with 60 minute units the day doesn't fit and has to be scrolled.
        /// </summary>
        private void MakeViewShort()
        {
            Cal.FindForm().Height = 400;
            Assert.IsTrue(Cal.Renderer.GetVisibleTimeUnits() < 16, "Test setup: the view should show less than 16 units.");
        }

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

        private TimeSpan TopOfView()
        {
            return Cal.Days[0].TimeUnits[-Cal.TimeUnitsOffset].Date.TimeOfDay;
        }

        [TestMethod]
        [DataRow(CalendarTimeScale.SixtyMinutes)]
        [DataRow(CalendarTimeScale.FifteenMinutes)]
        [DataRow(CalendarTimeScale.FiveMinutes)]
        public void TimeScaleChange_KeepsTimeAtTopOfView(CalendarTimeScale scale)
        {
            MakeViewShort();
            Cal.ScrollToTime(8);

            Cal.TimeScale = scale;

            Assert.AreEqual(TimeSpan.FromHours(8), TopOfView());
        }

        [TestMethod]
        public void TimeScaleChange_ToCoarserScale_DoesNotScrollPastEndOfDay()
        {
            MakeViewShort();
            Cal.TimeScale = CalendarTimeScale.FiveMinutes;
            Cal.ScrollToTime(23);

            Cal.TimeScale = CalendarTimeScale.SixtyMinutes;

            // 23:00 can't be at the top: the view stops where the last unit reaches the bottom.
            Assert.AreEqual(-MaxUnitsFromMidnight, Cal.TimeUnitsOffset);
            Assert.IsTrue(MaxUnitsFromMidnight < 23, "Test setup: the view should be clamped.");
        }

        [TestMethod]
        public void TimeScaleChange_ClearsSelectionOfOldTimeUnits()
        {
            CalendarTimeScaleUnit unit = Cal.Days[0].TimeUnits[20];
            Cal.SetSelectionRange(unit, unit);

            Cal.TimeScale = CalendarTimeScale.FifteenMinutes;

            Assert.IsNull(Cal.SelectedElementStart);
            Assert.IsNull(Cal.SelectedElementEnd);
        }

        [TestMethod]
        public void KeyDown_AtBottomOfView_ScrollsSelectedUnitFullyIntoView()
        {
            int visible = Cal.Renderer.GetVisibleTimeUnits();
            CalendarTimeScaleUnit unit = Cal.Days[0].TimeUnits[visible - 2];
            Cal.SetSelectionRange(unit, unit);

            for (int i = 0; i < 4; i++)
            {
                Cal.PressKey(Keys.Down);

                var selected = (CalendarTimeScaleUnit)Cal.SelectedElementEnd;
                Assert.IsTrue(selected.Bounds.Bottom <= Cal.Days[0].BodyBounds.Bottom,
                    $"Step {i}: unit {selected.Date:HH:mm} at {selected.Bounds} is cut off by the bottom of the view {Cal.Days[0].BodyBounds}.");
            }
        }

        [TestMethod]
        public void KeyUp_AtTopOfView_ScrollsSelectedUnitIntoView()
        {
            Cal.ScrollToTime(8);
            CalendarTimeScaleUnit unit = Cal.Days[0].TimeUnits[-Cal.TimeUnitsOffset];
            Cal.SetSelectionRange(unit, unit);

            Cal.PressKey(Keys.Up);

            var selected = (CalendarTimeScaleUnit)Cal.SelectedElementEnd;
            Assert.AreEqual(new TimeSpan(7, 30, 0), selected.Date.TimeOfDay);
            Assert.AreEqual(selected.Date.TimeOfDay, TopOfView());
        }
    }
}
