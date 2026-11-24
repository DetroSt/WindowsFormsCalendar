using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Calendar subclass exposing the protected input handlers, so tests can simulate keys.
    /// </summary>
    internal class TestCalendar : Calendar
    {
        public void PressKey(Keys key) => OnKeyDown(new KeyEventArgs(key));

        public void TypeChar(char c) => OnKeyPress(new KeyPressEventArgs(c));

        public void MouseDownAt(Point p) => OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));

        public void MouseMoveTo(Point p) => OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, p.X, p.Y, 0));

        public void MouseUpAt(Point p) => OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));
    }

    /// <summary>
    /// Creates a real, laid-out calendar control (hosted in a hidden form) for each test.
    /// </summary>
    public abstract class CalendarTestBase
    {
        /// <summary>Monday, 23 November 2026.</summary>
        protected static readonly DateTime Monday = new DateTime(2026, 11, 23);

        private Form _form;

        internal TestCalendar Cal { get; private set; }

        [TestInitialize]
        public void CreateCalendar()
        {
            Cal = CreateCalendar(out _form);
        }

        [TestCleanup]
        public void DisposeCalendar()
        {
            _form?.Dispose();
        }

        internal static TestCalendar CreateCalendar(out Form form, int height = 700)
        {
            form = new Form { Size = new Size(1000, height) };
            var cal = new TestCalendar { Dock = DockStyle.Fill };
            form.Controls.Add(cal);
            form.CreateControl();
            cal.CreateControl();
            return cal;
        }

        protected CalendarItem AllDay(int firstDay, int lastDay, string text)
        {
            return new CalendarItem(Cal, Monday.AddDays(firstDay), Monday.AddDays(lastDay + 1).AddSeconds(-1), text);
        }

        protected CalendarItem Timed(int startHour, int endHour, string text)
        {
            return new CalendarItem(Cal, Monday.AddHours(startHour), Monday.AddHours(endHour), text);
        }

        /// <summary>
        /// Runs <paramref name="action"/> on its own STA thread and fails if it does not finish in time,
        /// so a regression into an endless loop fails the test instead of hanging the test run.
        /// </summary>
        protected static void RunWithTimeout(Action action, int milliseconds = 5000)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception e) { error = e; }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            Assert.IsTrue(thread.Join(milliseconds), $"The action did not finish within {milliseconds} ms (endless loop?).");

            if (error != null)
                throw new AssertFailedException("The action threw " + error.GetType().Name + ": " + error.Message, error);
        }
    }
}
