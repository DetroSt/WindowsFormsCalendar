using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Keyboard navigation in the month view, where time units are hidden.
    /// </summary>
    [STATestClass]
    public class MonthViewKeyboardTests : CalendarTestBase
    {
        [TestInitialize]
        public void ShowFourWeeks()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            Assert.AreEqual(CalendarDaysMode.Short, Cal.DaysMode);
        }

        private CalendarDay Day(DateTime date) => Cal.FindDay(date);

        [TestMethod]
        public void Arrows_MoveByDayAndWeek()
        {
            Cal.SetSelectionRange(Day(Monday.AddDays(8)), Day(Monday.AddDays(8)));

            Cal.PressKey(Keys.Right);
            Assert.AreSame(Day(Monday.AddDays(9)), Cal.SelectedElementEnd, "Right");

            Cal.PressKey(Keys.Down);
            Assert.AreSame(Day(Monday.AddDays(16)), Cal.SelectedElementEnd, "Down");

            Cal.PressKey(Keys.Left);
            Assert.AreSame(Day(Monday.AddDays(15)), Cal.SelectedElementEnd, "Left");

            Cal.PressKey(Keys.Up);
            Assert.AreSame(Day(Monday.AddDays(8)), Cal.SelectedElementEnd, "Up");
            Assert.AreSame(Cal.SelectedElementEnd, Cal.SelectedElementStart);
        }

        [TestMethod]
        public void ShiftArrow_ExtendsSelection()
        {
            Cal.SetSelectionRange(Day(Monday.AddDays(8)), Day(Monday.AddDays(8)));

            Cal.PressKey(Keys.Right | Keys.Shift);
            Cal.PressKey(Keys.Right | Keys.Shift);

            Assert.AreEqual(Monday.AddDays(8), Cal.SelectionStart);
            Assert.AreEqual(Monday.AddDays(10).Add(new TimeSpan(23, 59, 59)), Cal.SelectionEnd);
        }

        [TestMethod]
        public void InsertAfterArrow_CreatesAllDayItem()
        {
            Cal.SetSelectionRange(Day(Monday.AddDays(8)), Day(Monday.AddDays(8)));

            Cal.PressKey(Keys.Down);
            Cal.PressKey(Keys.Insert);
            Cal.FinalizeEditMode(false);

            Assert.AreEqual(1, Cal.Items.Count);
            Assert.AreEqual(Monday.AddDays(15), Cal.Items[0].StartDate);
            Assert.AreEqual(Monday.AddDays(15).Add(new TimeSpan(23, 59, 59)), Cal.Items[0].EndDate);
        }

        [TestMethod]
        public void ArrowPastEndOfView_KeepsSelection()
        {
            CalendarDay last = Cal.Days[Cal.Days.Length - 1];
            Cal.SetSelectionRange(last, last);

            Cal.PressKey(Keys.Right);
            Cal.PressKey(Keys.Down | Keys.Shift);

            Assert.AreSame(last, Cal.SelectedElementStart);
            Assert.AreSame(last, Cal.SelectedElementEnd);
        }
    }

    /// <summary>
    /// Replacing the renderer, view limits and the selection dates.
    /// </summary>
    [STATestClass]
    public class CalendarPropertyTests : CalendarTestBase
    {
        [TestMethod]
        public void ShiftDownAtEndOfDay_KeepsSelection()
        {
            Cal.SetViewRange(Monday, Monday);
            CalendarTimeScaleUnit last = Cal.Days[0].TimeUnits[Cal.Days[0].TimeUnits.Length - 1];
            Cal.SetSelectionRange(last, last);

            Cal.PressKey(Keys.Down | Keys.Shift);

            Assert.AreSame(last, Cal.SelectedElementStart);
            Assert.AreSame(last, Cal.SelectedElementEnd);
        }

        [TestMethod]
        public void ReplacingRenderer_LaysOutAndRepaints()
        {
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle);
            Cal.SetViewRange(Monday, Monday.AddDays(2));
            CalendarItem item = Timed(10, 11, "x");
            Cal.Items.Add(item);
            bool repainted = false;
            Cal.Invalidated += (s, e) => repainted = true;

            Cal.Renderer = new CalendarSystemRenderer(Cal);

            Assert.IsFalse(Cal.Renderer.TimeScaleBounds.IsEmpty, "The new renderer has no layout.");
            Assert.IsFalse(item.Bounds.IsEmpty, "The item was not placed by the new renderer.");
            Assert.IsTrue(repainted, "The calendar was not repainted.");
        }

        [TestMethod]
        public void NullRenderer_Throws()
        {
            CalendarRenderer before = Cal.Renderer;

            Assert.ThrowsExactly<ArgumentNullException>(() => Cal.Renderer = null);
            Assert.AreSame(before, Cal.Renderer);
        }

        [TestMethod]
        public void MaximumViewDays_SmallerThanCurrentView_ThrowsAndKeepsCalendarUsable()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.MaximumViewDays = 7);

            Assert.AreEqual(35, Cal.MaximumViewDays);
            Cal.FirstDayOfWeek = DayOfWeek.Monday; // used to throw after the limit was lowered
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-7)]
        [DataRow(10)]
        public void MaximumViewDays_NotPositiveMultipleOfSeven_Throws(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.MaximumViewDays = value);
        }

        [TestMethod]
        public void MaximumViewDays_NotSmallerThanCurrentView_IsAccepted()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));

            Cal.MaximumViewDays = 7;
            Cal.MaximumViewDays = 42;

            Assert.AreEqual(42, Cal.MaximumViewDays);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void MaximumFullDays_LessThanOne_Throws(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.MaximumFullDays = value);
        }

        [TestMethod]
        public void MaximumFullDays_AppliesToCurrentView()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Assert.AreEqual(CalendarDaysMode.Expanded, Cal.DaysMode);

            Cal.MaximumFullDays = 3;
            Assert.AreEqual(CalendarDaysMode.Short, Cal.DaysMode);

            Cal.MaximumFullDays = 8;
            Assert.AreEqual(CalendarDaysMode.Expanded, Cal.DaysMode);
        }

        [TestMethod]
        public void SelectionDates_WithoutSelection_AreMinValue()
        {
            Cal.SetViewRange(Monday, Monday);

            Assert.AreEqual(DateTime.MinValue, Cal.SelectionStart);
            Assert.AreEqual(DateTime.MinValue, Cal.SelectionEnd);
        }

        [TestMethod]
        public void SelectionDates_ReflectSelectedTimeUnits_InEitherOrder()
        {
            Cal.SetViewRange(Monday, Monday);
            CalendarTimeScaleUnit ten = Cal.GetTimeUnit(Monday.AddHours(10));
            CalendarTimeScaleUnit eleven = Cal.GetTimeUnit(Monday.AddHours(11));

            Cal.SetSelectionRange(eleven, ten);

            Assert.AreEqual(Monday.AddHours(10), Cal.SelectionStart);
            Assert.AreEqual(Monday.AddHours(11.5), Cal.SelectionEnd);
        }

        [TestMethod]
        public void SettingSelectionDates_SelectsTimeUnits()
        {
            Cal.SetViewRange(Monday, Monday);

            Cal.SelectionStart = Monday.AddHours(9);
            Cal.SelectionEnd = Monday.AddHours(10.5);

            Assert.AreSame(Cal.GetTimeUnit(Monday.AddHours(9)), Cal.SelectedElementStart);
            Assert.AreSame(Cal.GetTimeUnit(Monday.AddHours(10)), Cal.SelectedElementEnd);
            Assert.AreEqual(Monday.AddHours(9), Cal.SelectionStart);
            Assert.AreEqual(Monday.AddHours(10.5), Cal.SelectionEnd);
            Assert.IsTrue(Cal.GetTimeUnit(Monday.AddHours(9.5)).Selected, "Units between start and end are not selected.");
        }

        [TestMethod]
        public void SettingSelectionDates_ThenInsert_CreatesItemForThatTime()
        {
            Cal.SetViewRange(Monday, Monday);
            Cal.SelectionStart = Monday.AddHours(9);
            Cal.SelectionEnd = Monday.AddHours(10.5);

            Cal.PressKey(Keys.Insert);
            Cal.FinalizeEditMode(false);

            Assert.AreEqual(Monday.AddHours(9), Cal.Items[0].StartDate);
            Assert.AreEqual(Monday.AddHours(10.5), Cal.Items[0].EndDate);
        }

        [TestMethod]
        public void SelectionDates_InMonthView_UseWholeDays()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));

            Cal.SelectionStart = Monday.AddDays(3).AddHours(15);
            Cal.SelectionEnd = Monday.AddDays(5).Add(new TimeSpan(23, 59, 59));

            Assert.IsInstanceOfType(Cal.SelectedElementStart, typeof(CalendarDay));
            Assert.AreEqual(Monday.AddDays(3), Cal.SelectionStart);
            Assert.AreEqual(Monday.AddDays(5).Add(new TimeSpan(23, 59, 59)), Cal.SelectionEnd);
        }

        [TestMethod]
        public void SettingSelectionDateOutsideView_Throws()
        {
            Cal.SetViewRange(Monday, Monday);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.SelectionStart = Monday.AddDays(5));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.SelectionEnd = Monday.AddDays(-5));
            Assert.IsNull(Cal.SelectedElementStart);
        }
    }

    /// <summary>
    /// <see cref="MonthView"/> properties that change the layout or the colors.
    /// </summary>
    [STATestClass]
    public class MonthViewPropertyTests
    {
        private Form _form;
        private MonthView _view;
        private bool _repainted;

        [TestInitialize]
        public void CreateView()
        {
            _form = new Form { Size = new Size(300, 400) };
            _view = new MonthView { Dock = DockStyle.Fill };
            _form.Controls.Add(_view);
            _form.CreateControl();
            _view.CreateControl();
            Assert.AreNotEqual(IntPtr.Zero, _view.Handle);
            _view.Invalidated += (s, e) => _repainted = true;
        }

        [TestCleanup]
        public void DisposeView() => _form?.Dispose();

        [TestMethod]
        public void HidingDayNames_ShrinksMonthAndMovesDaysUp()
        {
            Size before = _view.MonthSize;
            _repainted = false;

            _view.DayNamesVisible = false;

            MonthViewMonth month = _view.Months[0];
            Assert.AreEqual(before.Height - _view.DaySize.Height, _view.MonthSize.Height, "Month height");
            Assert.AreEqual(0, month.DayNamesBounds.Length, "Day name headers");
            Assert.AreEqual(month.Bounds.Top + _view.DaySize.Height, month.Days[0].Bounds.Top, "First week is not right below the title.");
            Assert.IsTrue(month.Bounds.Contains(month.Days[month.Days.Length - 1].Bounds), "The last week is outside the month.");
            Assert.IsTrue(_repainted, "Not repainted.");
        }

        [TestMethod]
        public void ShowingDayNamesAgain_RestoresLayout()
        {
            Size before = _view.MonthSize;

            _view.DayNamesVisible = false;
            _view.DayNamesVisible = true;

            Assert.AreEqual(before, _view.MonthSize);
            Assert.AreEqual(7, _view.Months[0].DayNamesBounds.Length);
        }

        [TestMethod]
        public void ChangingItemPadding_ResizesDays()
        {
            Size before = _view.DaySize;
            _repainted = false;

            _view.ItemPadding = new Padding(10);

            Assert.AreEqual(before.Width + 16, _view.DaySize.Width, "Default padding is 2 on each side.");
            Assert.AreEqual(_view.DaySize, _view.Months[0].Days[0].Bounds.Size, "Days were not laid out again.");
            Assert.IsTrue(_repainted, "Not repainted.");
        }

        [TestMethod]
        public void ChangingColor_Repaints()
        {
            _repainted = false;

            _view.DaySelectedBackgroundColor = Color.Red;

            Assert.IsTrue(_repainted);
        }
    }
}
