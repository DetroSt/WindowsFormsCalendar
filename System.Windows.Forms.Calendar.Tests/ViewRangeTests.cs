using System;
using System.Drawing;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// View range validation and the exceptions thrown by the library.
    /// </summary>
    [STATestClass]
    public class ViewRangeTests : CalendarTestBase
    {
        private (DateTime Start, DateTime End, int Days, int Items) Snapshot()
        {
            return (Cal.ViewStart, Cal.ViewEnd, Cal.Days.Length, Cal.Items.Count);
        }

        private void PrepareWeekWithOneItem()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Cal.Items.Add(Timed(10, 11, "meeting"));
        }

        [TestMethod]
        public void SetViewRange_LongerThanMaximum_ThrowsAndKeepsCurrentView()
        {
            PrepareWeekWithOneItem();
            var before = Snapshot();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.SetViewRange(Monday, Monday.AddDays(49)));

            Assert.AreEqual(before, Snapshot());
        }

        [TestMethod]
        public void ViewEnd_TooFar_ThrowsAndKeepsCurrentView()
        {
            PrepareWeekWithOneItem();
            var before = Snapshot();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.ViewEnd = Monday.AddDays(60));

            Assert.AreEqual(before, Snapshot());
        }

        [TestMethod]
        public void ViewStart_AfterViewEnd_ThrowsAndKeepsCurrentView()
        {
            PrepareWeekWithOneItem();
            var before = Snapshot();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.ViewStart = Monday.AddDays(10));

            Assert.AreEqual(before, Snapshot());
        }

        [TestMethod]
        public void SetViewRange_Valid_ChangesView()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(13));

            Assert.AreEqual(Monday, Cal.ViewStart);
            Assert.AreEqual(Monday.AddDays(13).Date, Cal.ViewEnd.Date);
        }

        [TestMethod]
        public void MaximumViewDays_NotMultipleOfSeven_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Cal.MaximumViewDays = 10);
        }

        [TestMethod]
        public void MaximumViewDays_MultipleOfSeven_IsAccepted()
        {
            Cal.MaximumViewDays = 42;

            Assert.AreEqual(42, Cal.MaximumViewDays);
        }

        [TestMethod]
        public void FromHex_ValidColor_IsParsed()
        {
            Assert.AreEqual(Color.FromArgb(0xC2, 0xDA, 0xFC), CalendarColorTable.FromHex("#C2DAFC"));
            Assert.AreEqual(Color.FromArgb(0xC2, 0xDA, 0xFC), CalendarColorTable.FromHex("C2DAFC"));
        }

        [TestMethod]
        [DataRow("#12345")]
        [DataRow("ZZZZZZ")]
        public void FromHex_InvalidColor_ThrowsFormatException(string hex)
        {
            Assert.ThrowsExactly<FormatException>(() => CalendarColorTable.FromHex(hex));
        }

        [TestMethod]
        public void FromHex_Null_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => CalendarColorTable.FromHex(null));
        }
    }
}
