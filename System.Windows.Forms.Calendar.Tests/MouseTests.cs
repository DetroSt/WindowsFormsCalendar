using System;
using System.Collections.Generic;
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

        /// <summary>Adds the grabbed item (10-12) and a second one (14-15), both selected.</summary>
        private CalendarItem AddTwoSelectedTimedItems(out CalendarItem other)
        {
            CalendarItem grabbed = AddTimedItemAndShowIt();
            other = Timed(14, 15, "other");
            Cal.Items.Add(other);
            grabbed.SetSelected(true);
            other.SetSelected(true);
            return grabbed;
        }

        [TestMethod]
        public void DraggingOneOfSeveralSelectedItems_MovesAllOfThem()
        {
            CalendarItem grabbed = AddTwoSelectedTimedItems(out CalendarItem other);
            var changed = new List<CalendarItem>();
            CalendarItemsEventArgs groupEvent = null;
            int groupEvents = 0;
            Cal.ItemDatesChanged += (s, e) => changed.Add(e.Item);
            Cal.ItemsDatesChanged += (s, e) => { groupEvent = e; groupEvents++; };

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(12));
            Cal.MouseUpAt(PointAt(12));

            Assert.AreEqual(Monday.AddHours(11), grabbed.StartDate);
            Assert.AreEqual(Monday.AddHours(13), grabbed.EndDate);
            Assert.AreEqual(Monday.AddHours(15), other.StartDate);
            Assert.AreEqual(Monday.AddHours(16), other.EndDate);
            CollectionAssert.AreEquivalent(new[] { grabbed, other }, changed);
            Assert.AreEqual(1, groupEvents);
            CollectionAssert.AreEquivalent(new[] { grabbed, other }, groupEvent.Items);
            Assert.IsTrue(grabbed.Selected && other.Selected, "The moved items should stay selected.");
            Assert.IsFalse(grabbed.IsDragging || other.IsDragging);
        }

        [TestMethod]
        public void DraggingSeveralSelectedAllDayItems_MovesThemByWholeDays()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            CalendarItem grabbed = AllDay(0, 0, "grabbed");
            CalendarItem other = AllDay(2, 3, "other");
            Cal.Items.Add(grabbed);
            Cal.Items.Add(other);
            grabbed.SetSelected(true);
            other.SetSelected(true);
            int y = Center(grabbed.Bounds).Y;

            Cal.MouseDownAt(new Point(Center(Cal.Days[0].Bounds).X, y));
            Cal.MouseMoveTo(new Point(Center(Cal.Days[1].Bounds).X, y));
            Cal.MouseUpAt(new Point(Center(Cal.Days[1].Bounds).X, y));

            Assert.AreEqual(Monday.AddDays(1), grabbed.StartDate);
            Assert.AreEqual(Monday.AddDays(3), other.StartDate);
            Assert.AreEqual(Monday.AddDays(5).AddSeconds(-1), other.EndDate);
        }

        [TestMethod]
        public void ClickWithoutMovingOnOneOfSeveralSelectedItems_SelectsOnlyThatItem()
        {
            CalendarItem grabbed = AddTwoSelectedTimedItems(out CalendarItem other);
            bool datesChanged = false;
            Cal.ItemDatesChanged += (s, e) => datesChanged = true;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseUpAt(PointAt(11));

            Assert.IsTrue(grabbed.Selected);
            Assert.IsFalse(other.Selected);
            Assert.AreEqual(Monday.AddHours(14), other.StartDate);
            Assert.IsFalse(datesChanged);
        }

        [TestMethod]
        public void DraggingAnUnselectedItem_MovesOnlyThatItem()
        {
            CalendarItem grabbed = AddTimedItemAndShowIt();
            CalendarItem other = Timed(14, 15, "other");
            Cal.Items.Add(other);
            other.SetSelected(true);

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(12));
            Cal.MouseUpAt(PointAt(12));

            Assert.AreEqual(Monday.AddHours(11), grabbed.StartDate);
            Assert.AreEqual(Monday.AddHours(14), other.StartDate);
            Assert.IsFalse(other.Selected);
        }

        [TestMethod]
        public void DroppingItemOntoAnotherItemOfAnotherDay_WeekView_MovesItToThatTime()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Cal.ScrollToTime(8);
            CalendarItem a = Timed(10, 11, "a");
            CalendarItem b = new CalendarItem(Cal, Monday.AddDays(2).AddHours(10), Monday.AddDays(2).AddHours(11), "b");
            Cal.Items.Add(a);
            Cal.Items.Add(b);
            Point grab = Center(a.Bounds);
            Point drop = Center(b.Bounds);
            Cal.MouseDownAt(grab);
            Cal.MouseMoveTo(new Point((grab.X + drop.X) / 2, grab.Y));
            Cal.MouseMoveTo(drop);
            Cal.MouseUpAt(drop);
            Assert.AreEqual(Monday.AddDays(2).AddHours(10), a.StartDate);
        }

        [TestMethod]
        public void DroppingItemOntoAnotherItemOfAnotherDay_MonthView_MovesItToThatDay()
        {
            Cal.MaximumFullDays = 1;
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            CalendarItem a = Timed(10, 11, "a");
            CalendarItem b = new CalendarItem(Cal, Monday.AddDays(2).AddHours(10), Monday.AddDays(2).AddHours(11), "b");
            Cal.Items.Add(a);
            Cal.Items.Add(b);
            Point grab = Center(a.Bounds);
            Point drop = Center(b.Bounds);
            Cal.MouseDownAt(grab);
            Cal.MouseMoveTo(new Point((grab.X + drop.X) / 2, grab.Y));
            Cal.MouseMoveTo(drop);
            Cal.MouseUpAt(drop);
            Assert.AreEqual(Monday.AddDays(2).AddHours(10), a.StartDate);
        }

        [TestMethod]
        public void ItemsRebuiltAndReselectedDuringDrag_DraggedItemFollowsTheMouseWithoutDrifting()
        {
            // An application that rebuilds its items (e.g. after saving the previous move or after a
            // database change notification) and reselects the current one with SetItemSelected while a
            // drag is in progress. SetItemSelected replaced the dragged item, so the shift was computed from
            // the new, unmoved copy and added to the dragged item again on every mouse move: the item
            // ended up on a random, far away date.
            CalendarItem grabbed = AddTimedItemAndShowIt();
            CalendarItem reported = null;
            Cal.ItemDatesChanged += (s, e) => reported = e.Item;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(12));

            Cal.Items.Clear();
            CalendarItem copy = Timed(10, 12, "x");
            Cal.Items.Add(copy);
            Cal.SetItemSelected(copy);

            for (int i = 0; i < 5; i++)
                Cal.MouseMoveTo(PointAt(13));
            Cal.MouseUpAt(PointAt(13));

            Assert.AreSame(grabbed, reported, "The reported item is not the one that was dragged.");
            Assert.AreEqual(Monday.AddHours(12), grabbed.StartDate);
            Assert.AreEqual(Monday.AddHours(14), grabbed.EndDate);
            Assert.AreEqual(Monday.AddHours(10), copy.StartDate, "The reselected copy must not move.");
        }

        [TestMethod]
        public void SetItemSelected_ThenClickOnEmptyArea_DoesNotRaiseItemClick()
        {
            CalendarItem item = AddTimedItemAndShowIt();
            bool clicked = false;
            Cal.ItemClick += (s, e) => clicked = true;

            Cal.SetItemSelected(item);
            Cal.MouseDownAt(PointAt(15));
            Cal.MouseUpAt(PointAt(15));

            Assert.IsFalse(clicked, "Selecting an item from code made the next click on an empty area report a click on it.");
        }

        [TestMethod]
        public void LosingMouseCaptureAfterMouseUp_KeepsMovedItem()
        {
            // WinForms releases the mouse capture after OnMouseUp: that must not undo an ordinary drag.
            CalendarItem item = AddTimedItemAndShowIt();
            int datesChanged = 0;
            Cal.ItemDatesChanged += (s, e) => datesChanged++;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(13));
            Cal.MouseUpAt(PointAt(13));
            Cal.LoseMouseCapture();

            Assert.AreEqual(Monday.AddHours(12), item.StartDate);
            Assert.AreEqual(1, datesChanged);
        }

        [TestMethod]
        public void RightClickOnItem_DoesNotStartDrag()
        {
            // A right-click opens the application's context menu, which takes the mouse-up: the item
            // used to stay "dragged" and follow the mouse until the next click saved it elsewhere.
            CalendarItem item = AddTimedItemAndShowIt();
            bool datesChanged = false;
            Cal.ItemDatesChanged += (s, e) => datesChanged = true;

            Cal.MouseDownAt(PointAt(11), MouseButtons.Right);
            Cal.MouseMoveTo(PointAt(15), MouseButtons.Right);
            Cal.HoverAt(PointAt(16));
            Cal.MouseDownAt(PointAt(16));
            Cal.MouseUpAt(PointAt(16));

            Assert.AreEqual(Monday.AddHours(10), item.StartDate);
            Assert.IsFalse(datesChanged);
        }

        [TestMethod]
        public void MouseMoveWithoutButton_DuringDrag_CancelsDragAndRestoresDates()
        {
            // The mouse-up went elsewhere (e.g. a message box appeared during the drag): moving the mouse
            // afterwards must not keep moving the item, and a later click must not save it somewhere else.
            CalendarItem item = AddTimedItemAndShowIt();
            bool datesChanged = false;
            Cal.ItemDatesChanged += (s, e) => datesChanged = true;
            Cal.ItemsDatesChanged += (s, e) => datesChanged = true;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(12));
            Cal.HoverAt(PointAt(15));
            Cal.MouseDownAt(PointAt(15));
            Cal.MouseUpAt(PointAt(15));

            Assert.AreEqual(Monday.AddHours(10), item.StartDate);
            Assert.AreEqual(Monday.AddHours(12), item.EndDate);
            Assert.IsFalse(item.IsDragging);
            Assert.IsFalse(datesChanged, "The cancelled drag was reported as a date change.");
        }

        [TestMethod]
        public void LosingMouseCapture_DuringDrag_CancelsDragAndRestoresDates()
        {
            CalendarItem grabbed = AddTwoSelectedTimedItems(out CalendarItem other);
            bool datesChanged = false;
            Cal.ItemsDatesChanged += (s, e) => datesChanged = true;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(13));
            Cal.LoseMouseCapture();
            Cal.MouseUpAt(PointAt(13));

            Assert.AreEqual(Monday.AddHours(10), grabbed.StartDate);
            Assert.AreEqual(Monday.AddHours(14), other.StartDate);
            Assert.AreEqual(Monday.AddHours(15), other.EndDate);
            Assert.IsFalse(datesChanged);
        }

        [TestMethod]
        public void Escape_DuringDrag_CancelsDragAndRestoresDates()
        {
            CalendarItem item = AddTimedItemAndShowIt();
            bool datesChanged = false;
            Cal.ItemDatesChanged += (s, e) => datesChanged = true;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(13));
            Cal.PressKey(Keys.Escape);
            Cal.MouseMoveTo(PointAt(14));
            Cal.MouseUpAt(PointAt(14));

            Assert.AreEqual(Monday.AddHours(10), item.StartDate);
            Assert.IsFalse(datesChanged);
            Assert.AreEqual(1, Cal.Items.Count, "Escape must not delete or create items.");
        }

        [TestMethod]
        public void DraggingSelectedItems_WithALockedOneSelected_MovesNothing()
        {
            CalendarItem grabbed = AddTwoSelectedTimedItems(out CalendarItem other);
            other.Locked = true;
            bool datesChanged = false;
            Cal.ItemDatesChanged += (s, e) => datesChanged = true;

            Cal.MouseDownAt(PointAt(11));
            Cal.MouseMoveTo(PointAt(12));
            Cal.MouseUpAt(PointAt(12));

            Assert.AreEqual(Monday.AddHours(10), grabbed.StartDate);
            Assert.AreEqual(Monday.AddHours(14), other.StartDate);
            Assert.IsFalse(datesChanged);
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
