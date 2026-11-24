using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Every way of changing Calendar.Items updates the layout and repaints the calendar,
    /// including access through IList / ICollection (e.g. data binding).
    /// </summary>
    [STATestClass]
    public class ItemCollectionTests : CalendarTestBase
    {
        private bool _repainted;

        [TestInitialize]
        public void ShowOneDay()
        {
            // Invalidated is raised only for controls with a window handle (the test form is hidden).
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle);
            Cal.SetViewRange(Monday, Monday);
            Cal.Invalidated += (s, e) => _repainted = true;
        }

        private void AssertPlacedAndRepainted(CalendarItem item)
        {
            Assert.IsFalse(item.Bounds.IsEmpty, "The item has no position (layout was not updated).");
            Assert.IsTrue(_repainted, "The calendar was not repainted.");
        }

        [TestMethod]
        public void Indexer_ReplacingItem_PlacesNewItem()
        {
            Cal.Items.Add(Timed(10, 11, "old"));
            CalendarItem replacement = Timed(13, 14, "new");
            _repainted = false;

            Cal.Items[0] = replacement;

            AssertPlacedAndRepainted(replacement);
        }

        [TestMethod]
        public void GenericIListAdd_PlacesItem()
        {
            CalendarItem item = Timed(10, 11, "x");
            _repainted = false;

            ((IList<CalendarItem>)Cal.Items).Add(item);

            AssertPlacedAndRepainted(item);
        }

        [TestMethod]
        public void GenericIListIndexer_ReplacingItem_PlacesNewItem()
        {
            Cal.Items.Add(Timed(10, 11, "old"));
            CalendarItem replacement = Timed(13, 14, "new");
            _repainted = false;

            ((IList<CalendarItem>)Cal.Items)[0] = replacement;

            AssertPlacedAndRepainted(replacement);
        }

        [TestMethod]
        public void NonGenericIListAdd_PlacesItem()
        {
            CalendarItem item = Timed(10, 11, "x");
            _repainted = false;

            int index = ((IList)Cal.Items).Add(item);

            Assert.AreEqual(0, index);
            AssertPlacedAndRepainted(item);
        }

        [TestMethod]
        public void NonGenericIListInsertAndIndexer_PlaceItems()
        {
            CalendarItem first = Timed(10, 11, "first");
            CalendarItem replacement = Timed(13, 14, "replacement");
            var list = (IList)Cal.Items;

            list.Insert(0, first);
            AssertPlacedAndRepainted(first);

            _repainted = false;
            list[0] = replacement;
            AssertPlacedAndRepainted(replacement);
        }

        [TestMethod]
        public void RemoveThroughInterfaces_GivesNeighbourFullWidthAgain()
        {
            foreach (string way in new[] { "ICollection<T>", "IList" })
            {
                Cal.Items.Clear();
                CalendarItem a = Timed(10, 11, "a");
                CalendarItem b = Timed(10, 11, "b");
                Cal.Items.Add(a);
                Cal.Items.Add(b);
                int halfWidth = b.Bounds.Width;

                if (way == "IList")
                    ((IList)Cal.Items).Remove(a);
                else
                    ((ICollection<CalendarItem>)Cal.Items).Remove(a);

                Assert.IsTrue(b.Bounds.Width > halfWidth,
                    $"{way}.Remove: the remaining item kept its half width ({halfWidth} -> {b.Bounds.Width}).");
            }
        }

        [TestMethod]
        public void ClearThroughInterface_Repaints()
        {
            Cal.Items.Add(Timed(10, 11, "x"));
            _repainted = false;

            ((ICollection<CalendarItem>)Cal.Items).Clear();

            Assert.AreEqual(0, Cal.Items.Count);
            Assert.IsTrue(_repainted, "The calendar was not repainted.");
        }

        [TestMethod]
        public void ReverseAndSort_UpdateLayout()
        {
            Cal.Items.Add(Timed(10, 11, "a"));
            Cal.Items.Add(Timed(12, 13, "b"));

            _repainted = false;
            Cal.Items.Reverse();
            Assert.IsTrue(_repainted, "Reverse did not repaint.");
            Assert.AreEqual("b", Cal.Items[0].Text);

            _repainted = false;
            Cal.Items.Sort((x, y) => string.CompareOrdinal(x.Text, y.Text));
            Assert.IsTrue(_repainted, "Sort did not repaint.");
            Assert.AreEqual("a", Cal.Items[0].Text);
        }

        [TestMethod]
        public void MonthViewLayout_StillWorksWithReverseInsideLayout()
        {
            // The month view layout reverses the item order internally; with Reverse() now updating
            // the layout, it must use the internal variant, or it would recurse forever.
            Cal.SetViewRange(Monday, Monday.AddDays(27));

            CalendarItem item = AllDay(2, 4, "x");
            Cal.Items.Add(item);

            Assert.IsFalse(item.Bounds.IsEmpty);
        }

        [TestMethod]
        public void ListMethods_AreStillAvailable()
        {
            CalendarItem a = Timed(10, 11, "a");
            CalendarItem b = Timed(12, 13, "b");
            Cal.Items.AddRange(new[] { a, b });

            // A sample of List<T> members that applications may use; this only has to compile and work.
            Assert.AreSame(b, Cal.Items.Find(i => i.Text == "b"));
            Assert.AreEqual(1, Cal.Items.FindAll(i => i.Text == "a").Count);
            Assert.AreEqual(1, Cal.Items.IndexOf(b));
            Assert.IsTrue(Cal.Items.Exists(i => i.Text == "a"));
            Assert.AreEqual(2, Cal.Items.ToArray().Length);

            List<CalendarItem> asList = Cal.Items;
            Assert.AreEqual(2, asList.Count);
        }
    }
}
