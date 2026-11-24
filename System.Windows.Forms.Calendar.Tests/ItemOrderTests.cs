using System;
using System.Collections.Generic;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Items that share a place (the same day, the same day-top range) are laid out in time order.
    /// </summary>
    [STATestClass]
    public class ItemOrderTests : CalendarTestBase
    {
        private List<CalendarItem> EightItemsOnWednesday()
        {
            var items = new List<CalendarItem>();
            DateTime wednesday = Monday.AddDays(2);

            for (int hour = 8; hour < 16; hour++)
                items.Add(new CalendarItem(Cal, wednesday.AddHours(hour), wednesday.AddHours(hour + 1), $"{hour}:00"));

            return items;
        }

        private void AssertEarliestShownInTimeOrder(List<CalendarItem> byTime)
        {
            int shown = byTime.FindAll(i => !i.Bounds.IsEmpty).Count;
            Assert.IsTrue(shown > 0 && shown < byTime.Count, $"Test setup: only some of the items should fit in the day ({shown}).");

            for (int i = 0; i < byTime.Count; i++)
            {
                if (i < shown)
                    Assert.IsFalse(byTime[i].Bounds.IsEmpty, $"Item {byTime[i].Text} is hidden although a later item is shown.");
                else
                    Assert.IsTrue(byTime[i].Bounds.IsEmpty, $"Item {byTime[i].Text} is shown although an earlier item is hidden.");

                if (i > 0 && i < shown)
                    Assert.IsTrue(byTime[i - 1].Bounds.Top < byTime[i].Bounds.Top,
                        $"Item {byTime[i - 1].Text} is not above {byTime[i].Text}.");
            }
        }

        [TestMethod]
        public void MonthView_ItemsAddedInTimeOrder_EarliestAreShownInTimeOrder()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            List<CalendarItem> items = EightItemsOnWednesday();

            Cal.Items.AddRange(items);

            AssertEarliestShownInTimeOrder(items);
        }

        [TestMethod]
        public void MonthView_ItemsAddedInMixedOrder_EarliestAreShownInTimeOrder()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            List<CalendarItem> items = EightItemsOnWednesday();

            foreach (int i in new[] { 5, 0, 7, 2, 6, 1, 4, 3 })
                Cal.Items.Add(items[i]);

            AssertEarliestShownInTimeOrder(items);
        }

        [TestMethod]
        public void WeekView_DayTopItemsOverSameDays_AreInTimeOrder()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            var later = new CalendarItem(Cal, Monday.AddHours(10), Monday.AddDays(1).AddHours(12), "later");
            var earlier = new CalendarItem(Cal, Monday.AddHours(8), Monday.AddDays(1).AddHours(12), "earlier");

            Cal.Items.Add(later);
            Cal.Items.Add(earlier);

            Assert.IsTrue(earlier.Bounds.Top < later.Bounds.Top, $"earlier {earlier.Bounds} is not above later {later.Bounds}.");
        }

        [TestMethod]
        public void DayView_ItemsStartingInSameTimeUnit_AreInTimeOrder()
        {
            Cal.SetViewRange(Monday, Monday);
            var later = new CalendarItem(Cal, Monday.AddHours(10).AddMinutes(20), Monday.AddHours(11), "10:20");
            var earlier = new CalendarItem(Cal, Monday.AddHours(10).AddMinutes(5), Monday.AddHours(11), "10:05");

            Cal.Items.Add(later);
            Cal.Items.Add(earlier);

            Assert.IsTrue(earlier.Bounds.Left < later.Bounds.Left, $"10:05 {earlier.Bounds} is not left of 10:20 {later.Bounds}.");
        }
    }

    /// <summary>
    /// The text edit box when the edited item disappears from the calendar.
    /// </summary>
    [STATestClass]
    public class EditModeTests : CalendarTestBase
    {
        private CalendarItem _item;
        private bool _editEventRaised;

        [TestInitialize]
        public void StartEditing()
        {
            Cal.SetViewRange(Monday, Monday);
            Cal.ScrollToTime(8);
            _item = Timed(10, 11, "x");
            Cal.Items.Add(_item);
            Cal.ItemTextEdited += (s, e) => _editEventRaised = true;
            Cal.ItemCreated += (s, e) => _editEventRaised = true;

            Cal.ActivateEditMode(_item);
            Assert.IsTrue(Cal.EditMode, "Test setup: the item should be in edit mode.");
        }

        private void AssertEditBoxClosedWithoutEvents()
        {
            Assert.IsFalse(Cal.EditMode, "The edit box is still open.");
            Assert.AreEqual(0, Cal.Controls.Count, "The edit box is still on the calendar.");
            Assert.AreEqual(Calendar.CalendarState.Idle, Cal.State);
            Assert.IsNull(Cal.EditModeItem);
            Assert.IsFalse(_editEventRaised, "An edit event was raised for an item that is no longer in the calendar.");
        }

        [TestMethod]
        public void RemovingEditedItem_ClosesEditBox()
        {
            Cal.Items.Remove(_item);

            AssertEditBoxClosedWithoutEvents();
        }

        [TestMethod]
        public void ChangingView_ClosesEditBox()
        {
            Cal.SetViewRange(Monday.AddDays(7), Monday.AddDays(7));

            AssertEditBoxClosedWithoutEvents();
        }

        [TestMethod]
        public void RemovingNewItemBeingCreated_ClosesEditBoxWithoutItemCreated()
        {
            Cal.FinalizeEditMode(true);
            _editEventRaised = false;
            CalendarTimeScaleUnit unit = Cal.GetTimeUnit(Monday.AddHours(14));
            Cal.SetSelectionRange(unit, unit);
            Cal.TypeChar('a');
            CalendarItem created = Cal.EditModeItem;
            Assert.IsNotNull(created, "Test setup: typing should create an item in edit mode.");

            Cal.Items.Remove(created);

            AssertEditBoxClosedWithoutEvents();
        }

        [TestMethod]
        public void RemovingAnotherItem_KeepsEditing()
        {
            CalendarItem other = Timed(14, 15, "other");
            Cal.Items.Add(other);

            Cal.Items.Remove(other);

            Assert.IsTrue(Cal.EditMode);
            Assert.AreSame(_item, Cal.EditModeItem);
        }

        [TestMethod]
        public void AfterEditBoxIsClosed_ItemsCanBeEditedAgain()
        {
            Cal.Items.Remove(_item);
            CalendarItem other = Timed(14, 15, "other");
            Cal.Items.Add(other);

            Cal.ActivateEditMode(other);

            Assert.IsTrue(Cal.EditMode);
            Assert.AreSame(other, Cal.EditModeItem);
        }
    }
}
