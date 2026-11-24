using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Controls refresh themselves after changes that affect what they show.
    /// </summary>
    [STATestClass]
    public class RepaintTests : CalendarTestBase
    {
        [TestMethod]
        [DataRow(false, DisplayName = "Edit confirmed")]
        [DataRow(true, DisplayName = "Edit cancelled")]
        public void FinishingTextEdit_RepaintsTheWholeItem(bool cancel)
        {
            // Invalidated is raised only for controls with a window handle; the test form is never
            // shown, so create the handle explicitly.
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle);
            Cal.SetViewRange(Monday, Monday);
            CalendarItem item = Timed(10, 11, "old text");
            Cal.Items.Add(item);
            Cal.ActivateEditMode(item);
            Cal.TextBox.Text = "new text";

            var invalidated = new List<Rectangle>();
            Cal.Invalidated += (s, e) => invalidated.Add(e.InvalidRect);

            Cal.FinalizeEditMode(cancel);

            Assert.AreEqual(cancel ? "old text" : "new text", item.Text);
            // The edit box is 2 px smaller than the item on each side, so removing it alone
            // does not repaint the item's edges; the item itself must be invalidated.
            Assert.IsTrue(invalidated.Exists(r => r.Contains(item.Bounds)),
                $"No repaint covered the item {item.Bounds}; repainted: {string.Join(", ", invalidated)}.");
        }

        [TestMethod]
        [DataRow(DayOfWeek.Monday)]
        [DataRow(DayOfWeek.Saturday)]
        public void MonthViewFirstDayOfWeek_RebuildsMonthsAndRepaints(DayOfWeek firstDay)
        {
            using (var form = new Form { Size = new Size(400, 400) })
            {
                var monthView = new MonthView { Dock = DockStyle.Fill };
                form.Controls.Add(monthView);
                form.CreateControl();
                monthView.CreateControl();
                Assert.AreNotEqual(IntPtr.Zero, monthView.Handle); // see FinishingTextEdit_RepaintsTheWholeItem
                Assert.AreEqual(DayOfWeek.Sunday, monthView.Months[0].Days[0].Date.DayOfWeek);

                bool repainted = false;
                monthView.Invalidated += (s, e) => repainted = true;

                monthView.FirstDayOfWeek = firstDay;

                Assert.AreEqual(firstDay, monthView.Months[0].Days[0].Date.DayOfWeek);
                Assert.IsTrue(repainted, "The month view was not repainted.");
            }
        }

        [TestMethod]
        public void DeselectingItemSpanningWeeks_RepaintsEveryWeek()
        {
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle); // see FinishingTextEdit_RepaintsTheWholeItem
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            CalendarItem item = AllDay(3, 11, "spans two weeks");
            Cal.Items.Add(item);
            Cal.SetItemSelected(item);

            var parts = new List<Rectangle>(item.GetAllBounds());
            Assert.IsTrue(parts.Count >= 2, "Test setup: the item should have one rectangle per week.");

            var invalidated = new List<Rectangle>();
            Cal.Invalidated += (s, e) => invalidated.Add(e.InvalidRect);

            Cal.SetItemSelected(null); // clears the selection

            foreach (Rectangle part in parts)
            {
                Assert.IsTrue(invalidated.Exists(r => r.Contains(part)),
                    $"The part {part} was not repainted; repainted: {string.Join(", ", invalidated)}.");
            }
        }

        private static MonthView CreateMonthView(Form form)
        {
            var monthView = new MonthView { Dock = DockStyle.Fill };
            form.Controls.Add(monthView);
            form.CreateControl();
            monthView.CreateControl();
            Assert.AreNotEqual(IntPtr.Zero, monthView.Handle); // see FinishingTextEdit_RepaintsTheWholeItem
            return monthView;
        }

        [TestMethod]
        public void MonthViewDayNamesFormat_RebuildsHeadersAndRepaints()
        {
            using (var form = new Form { Size = new Size(400, 400) })
            {
                MonthView monthView = CreateMonthView(form);

                bool repainted = false;
                monthView.Invalidated += (s, e) => repainted = true;

                // "dd" (day number) differs from the default "ddd" even within the default 2 characters.
                monthView.DayNamesFormat = "dd";

                MonthViewMonth month = monthView.Months[0];
                Assert.AreEqual(month.Days[0].Date.ToString("dd"), month.DayHeaders[0]);
                Assert.IsTrue(repainted, "The month view was not repainted.");
            }
        }

        [TestMethod]
        public void MonthViewDayNamesLength_LongerThanNames_ShowsWholeNames()
        {
            using (var form = new Form { Size = new Size(400, 400) })
            {
                MonthView monthView = CreateMonthView(form);

                // Longer than any "ddd" abbreviation; used to throw ArgumentOutOfRangeException in Substring.
                monthView.DayNamesLength = 20;

                MonthViewMonth month = monthView.Months[0];
                for (int i = 0; i < 7; i++)
                    Assert.AreEqual(month.Days[i].Date.ToString("ddd"), month.DayHeaders[i]);
            }
        }

        [TestMethod]
        public void MonthViewDayNamesLength_Negative_Throws()
        {
            var monthView = new MonthView();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => monthView.DayNamesLength = -1);
        }
    }
}
