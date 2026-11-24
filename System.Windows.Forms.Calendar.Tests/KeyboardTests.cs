using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Keyboard handling and the time selection.
    /// </summary>
    [STATestClass]
    public class KeyboardTests : CalendarTestBase
    {
        private void SelectTenOClock()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            CalendarTimeScaleUnit unit = Cal.GetTimeUnit(Monday.AddHours(10));
            Cal.SetSelectionRange(unit, unit);
        }

        [TestMethod]
        [DataRow(Keys.Down)]
        [DataRow(Keys.Up)]
        [DataRow(Keys.Left)]
        [DataRow(Keys.Right)]
        [DataRow(Keys.PageDown)]
        [DataRow(Keys.PageUp)]
        public void ArrowKey_WithoutSelection_DoesNotThrow(Keys key)
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Cal.SetSelectionRange(null, null);

            Cal.PressKey(key);
        }

        [TestMethod]
        public void ArrowDown_WithSelection_MovesSelectionByOneUnit()
        {
            SelectTenOClock();

            Cal.PressKey(Keys.Down);

            Assert.AreEqual(Monday.AddHours(10).AddMinutes((int)Cal.TimeScale), Cal.SelectedElementEnd.Date);
        }

        [TestMethod]
        [DataRow('\r', DisplayName = "Enter")]
        [DataRow('\x1b', DisplayName = "Escape")]
        [DataRow('\b', DisplayName = "Backspace")]
        [DataRow('\x03', DisplayName = "Ctrl+C")]
        public void ControlCharacter_OnSelection_DoesNotCreateItem(char c)
        {
            SelectTenOClock();

            Cal.TypeChar(c);

            Assert.AreEqual(0, Cal.Items.Count);
        }

        [TestMethod]
        public void Letter_OnSelection_CreatesItemWithThatLetter()
        {
            SelectTenOClock();

            Cal.TypeChar('a');
            Cal.FinalizeEditMode(false);

            Assert.AreEqual(1, Cal.Items.Count);
            Assert.AreEqual("a", Cal.Items[0].Text);
        }

        [TestMethod]
        public void ChangingView_ClearsSelection()
        {
            SelectTenOClock();

            Cal.SetViewRange(Monday.AddDays(4), Monday.AddDays(4));

            Assert.IsNull(Cal.SelectedElementStart);
            Assert.IsNull(Cal.SelectedElementEnd);
        }

        [TestMethod]
        public void ShiftArrow_AfterViewChange_DoesNotHang()
        {
            // Select Friday 10:00 in a week view, switch to Friday alone, press Shift+Down.
            // The old selection pointed at the previous view's units, which made
            // UpdateSelectionElements loop forever.
            RunWithTimeout(() =>
            {
                Form form;
                TestCalendar cal = CreateCalendar(out form);
                try
                {
                    cal.SetViewRange(Monday, Monday.AddDays(6));
                    CalendarTimeScaleUnit friday = cal.GetTimeUnit(Monday.AddDays(4).AddHours(10));
                    cal.SetSelectionRange(friday, friday);

                    cal.SetViewRange(Monday.AddDays(4), Monday.AddDays(4));
                    cal.PressKey(Keys.Down | Keys.Shift);
                }
                finally
                {
                    form.Dispose();
                }
            });
        }
    }
}
