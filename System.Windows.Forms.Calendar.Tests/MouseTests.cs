using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Dragging items on the <see cref="Calendar"/> with the mouse.
    /// </summary>
    [STATestClass]
    public class CalendarMouseTests : CalendarTestBase
    {
        private static Point Center(Rectangle r) => new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);

        private Point PointAt(int hour) => Center(Cal.GetTimeUnit(Monday.AddHours(hour)).Bounds);

        private CalendarItem AddTimedItemAndShowIt()
        {
            Cal.SetViewRange(Monday, Monday);
            Cal.ScrollToTime(8);
            CalendarItem item = Timed(10, 12, "x");
            Cal.Items.Add(item);
            return item;
        }

        [TestMethod]
        public void ClickWithSmallMouseMovement_DoesNotMoveItem()
        {
            CalendarItem item = AddTimedItemAndShowIt();
            bool datesChanged = false;
            Cal.ItemDatesChanged += (s, e) => datesChanged = true;
            Point grab = PointAt(11); // in the middle of the item

            Cal.MouseDownAt(grab);
            Cal.MouseMoveTo(new Point(grab.X + 1, grab.Y));
            Cal.MouseUpAt(new Point(grab.X + 1, grab.Y));

            Assert.AreEqual(Monday.AddHours(10), item.StartDate);
            Assert.AreEqual(Monday.AddHours(12), item.EndDate);
            Assert.IsFalse(datesChanged, "ItemDatesChanged was raised although the item did not move.");
        }

        [TestMethod]
        public void DraggingOneHourDown_MovesItemByOneHour()
        {
            CalendarItem item = AddTimedItemAndShowIt();

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(12));
            Cal.MouseUpAt(PointAt(12));

            Assert.AreEqual(Monday.AddHours(11), item.StartDate);
            Assert.AreEqual(Monday.AddHours(13), item.EndDate);
        }

        [TestMethod]
        public void DraggingAllDayItemGrabbedOnItsLastDay_MovesItByOneDay()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            CalendarItem item = AllDay(0, 2, "three days");
            Cal.Items.Add(item);
            int y = Center(item.Bounds).Y;
            var grab = new Point(Center(Cal.Days[2].Bounds).X, y);
            var drop = new Point(Center(Cal.Days[3].Bounds).X, y);

            Cal.MouseDownAt(grab);
            Cal.MouseMoveTo(drop);
            Cal.MouseUpAt(drop);

            Assert.AreEqual(Monday.AddDays(1), item.StartDate);
            Assert.AreEqual(Monday.AddDays(4).AddSeconds(-1), item.EndDate);
        }
    }

    /// <summary>
    /// <see cref="MonthView"/> subclass exposing the protected mouse handlers.
    /// </summary>
    internal class TestMonthView : MonthView
    {
        public void MouseDownAt(Point p) => OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));

        public void MouseMoveTo(Point p) => OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, p.X, p.Y, 0));

        public void MouseUpAt(Point p) => OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));
    }

    /// <summary>
    /// Selecting days and navigating on the <see cref="MonthView"/> with the mouse.
    /// </summary>
    [STATestClass]
    public class MonthViewMouseTests
    {
        private Form _form;
        private TestMonthView _view;

        private void CreateView(int width)
        {
            _form = new Form { Size = new Size(width, 300) };
            _view = new TestMonthView { Dock = DockStyle.Fill };
            _form.Controls.Add(_view);
            _form.CreateControl();
            _view.CreateControl();
            _view.ViewStart = new DateTime(2026, 11, 1);
        }

        [TestInitialize]
        public void CreateDefaultView() => CreateView(300);

        [TestCleanup]
        public void DisposeView() => _form?.Dispose();

        private Point DayPoint(int dayOfNovember)
        {
            foreach (MonthViewDay day in _view.Months[0].Days)
            {
                if (day.Date == new DateTime(2026, 11, dayOfNovember))
                    return new Point(day.Bounds.Left + day.Bounds.Width / 2, day.Bounds.Top + day.Bounds.Height / 2);
            }

            throw new AssertFailedException($"Test setup: 2026-11-{dayOfNovember} is not shown.");
        }

        private void Drag(params int[] days)
        {
            _view.MouseDownAt(DayPoint(days[0]));
            for (int i = 1; i < days.Length; i++)
                _view.MouseMoveTo(DayPoint(days[i]));
            _view.MouseUpAt(DayPoint(days[days.Length - 1]));
        }

        private void AssertSelection(int firstDay, int lastDay)
        {
            Assert.AreEqual(new DateTime(2026, 11, firstDay), _view.SelectionStart, "Selection start");
            Assert.AreEqual(new DateTime(2026, 11, lastDay, 23, 59, 59), _view.SelectionEnd, "Selection end");
        }

        [TestMethod]
        public void ForwardButton_IsOnLastMonthOfFirstRow()
        {
            _form.Dispose();
            CreateView(900);
            Assert.IsTrue(_view.Months.Length > 1, "Test setup: several months should be shown side by side.");

            MonthViewMonth lastInRow = _view.Months[0];
            foreach (MonthViewMonth month in _view.Months)
            {
                if (month.Bounds.Top == _view.Months[0].Bounds.Top && month.Bounds.Left > lastInRow.Bounds.Left)
                    lastInRow = month;
            }

            Assert.IsTrue(lastInRow.Bounds.Contains(_view.ForwardButtonBounds),
                $"Forward button {_view.ForwardButtonBounds} is not on the last month {lastInRow.Bounds}.");
            Assert.IsTrue(_view.Months[0].Bounds.Contains(_view.BackwardButtonBounds));
        }

        [TestMethod]
        public void ManualDrag_BackwardsThenForwards_KeepsTheDayWhereDragStarted()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.Manual;

            Drag(10, 5, 12);

            AssertSelection(10, 12);
        }

        [TestMethod]
        public void ManualDrag_Backwards_SelectsUpToTheDayWhereDragStarted()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.Manual;

            Drag(10, 5);

            AssertSelection(5, 10);
        }

        [TestMethod]
        public void SecondDrag_EndingOnSameDayAsFirst_UpdatesSelection()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.Manual;

            Drag(10, 12);
            Drag(3, 12);

            AssertSelection(3, 12);
        }

        [TestMethod]
        public void ManualDrag_WithMaxSelectionCount_SelectsAtMostThatManyDaysFromStartDay()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.Manual;
            _view.MaxSelectionCount = 3;

            Drag(10, 20);
            AssertSelection(10, 12);

            Drag(10, 2);
            AssertSelection(8, 10);
        }

        [TestMethod]
        public void WeekSelection_WithMaxSelectionCount_StaysWithinLimitAndKeepsClickedDay()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.Week;
            _view.MaxSelectionCount = 3;

            Drag(19);

            Assert.AreEqual(3, (_view.SelectionEnd.Date - _view.SelectionStart).Days + 1, "Selected days");
            Assert.IsTrue(_view.SelectionStart <= new DateTime(2026, 11, 19) && new DateTime(2026, 11, 19) <= _view.SelectionEnd,
                $"The clicked day is not selected ({_view.SelectionStart:d} - {_view.SelectionEnd:d}).");
        }

        [TestMethod]
        public void WeekSelection_WithoutLimit_SelectsSevenDays()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.Week;

            Drag(19);

            Assert.AreEqual(7, (_view.SelectionEnd.Date - _view.SelectionStart).Days + 1);
        }

        [TestMethod]
        public void WorkWeekSelection_WorkWeekWrappingPastSaturday_HasRightLength()
        {
            _view.SelectionMode = MonthView.MonthViewSelection.WorkWeek;
            _view.WorkWeekStart = DayOfWeek.Saturday;
            _view.WorkWeekEnd = DayOfWeek.Wednesday;

            Drag(18); // Wednesday

            AssertSelection(14, 18); // Saturday to Wednesday
        }
    }
}
