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
    }
}
