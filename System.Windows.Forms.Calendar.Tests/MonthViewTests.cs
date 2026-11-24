using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// <see cref="MonthView"/>: Resize event, selection with a time of day, and colors.
    /// </summary>
    [STATestClass]
    public class MonthViewTests
    {
        private Form _form;
        private MonthView _view;

        [TestInitialize]
        public void CreateView()
        {
            _form = new Form { Size = new Size(300, 400) };
            _view = new MonthView { Dock = DockStyle.Fill };
            _form.Controls.Add(_view);
            _form.CreateControl();
            _view.CreateControl();
            _view.ViewStart = new DateTime(2026, 11, 1);
        }

        [TestCleanup]
        public void DisposeView() => _form?.Dispose();

        private MonthViewDay Day(int dayOfNovember)
        {
            foreach (MonthViewDay day in _view.Months[0].Days)
            {
                if (day.Date == new DateTime(2026, 11, dayOfNovember))
                    return day;
            }

            throw new AssertFailedException($"Test setup: 2026-11-{dayOfNovember} is not shown.");
        }

        private Bitmap Snapshot()
        {
            var bitmap = new Bitmap(_view.Width, _view.Height);
            _view.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            return bitmap;
        }

        private static Color ColorAt(Bitmap bitmap, Point p) => Color.FromArgb(bitmap.GetPixel(p.X, p.Y).ToArgb());

        [TestMethod]
        public void Resizing_RaisesResizeEvent()
        {
            int raised = 0;
            _view.Resize += (s, e) => raised++;

            _form.Size = new Size(600, 400);

            Assert.IsTrue(raised > 0, "Resize was not raised.");
        }

        [TestMethod]
        public void SelectionStartWithTimeOfDay_SelectsThatWholeDay()
        {
            _view.SelectionStart = new DateTime(2026, 11, 10, 15, 0, 0);
            _view.SelectionEnd = new DateTime(2026, 11, 12);

            Assert.AreEqual(new DateTime(2026, 11, 10), _view.SelectionStart);
            Assert.IsFalse(Day(9).Selected);
            Assert.IsTrue(Day(10).Selected, "The first day is not selected.");
            Assert.IsTrue(Day(12).Selected);
            Assert.IsFalse(Day(13).Selected);
        }

        [TestMethod]
        public void DaySelectedColor_IsTheTextColorOfOrdinaryDays()
        {
            _view.SelectionStart = _view.SelectionEnd = new DateTime(2026, 11, 20);
            int changedOrdinary = 0, changedSelected = 0;

            using (Bitmap before = Snapshot())
            {
                _view.DaySelectedColor = Color.Red;

                using (Bitmap after = Snapshot())
                {
                    Rectangle ordinary = Day(11).Bounds;
                    Rectangle selected = Day(20).Bounds;

                    for (int x = 0; x < after.Width; x++)
                    {
                        for (int y = 0; y < after.Height; y++)
                        {
                            if (before.GetPixel(x, y) == after.GetPixel(x, y)) continue;

                            if (ordinary.Contains(x, y)) changedOrdinary++;
                            if (selected.Contains(x, y)) changedSelected++;
                        }
                    }
                }
            }

            Assert.IsTrue(changedOrdinary > 0, "The text of an ordinary day did not change color.");
            Assert.AreEqual(0, changedSelected, "The selected day should keep DaySelectedTextColor.");
        }

        [TestMethod]
        public void BackColor_DefaultsToWindowAndIsPainted()
        {
            Assert.AreEqual(SystemColors.Window, _view.BackColor);

            _view.BackColor = Color.Yellow;

            using (Bitmap bitmap = Snapshot())
            {
                // A corner of the control outside the month grids.
                Assert.AreEqual(Color.Yellow.ToArgb(), ColorAt(bitmap, new Point(1, 1)).ToArgb());
            }
        }
    }
}
