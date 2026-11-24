using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Renderer that records the labels of the time scale.
    /// </summary>
    internal class TimeScaleRecordingRenderer : CalendarProfessionalRenderer
    {
        public readonly List<string> Hours = new List<string>();
        public readonly List<string> Minutes = new List<string>();

        public TimeScaleRecordingRenderer(Calendar calendar) : base(calendar) { }

        public override void OnDrawTimeScaleHour(CalendarRendererBoxEventArgs e)
        {
            Hours.Add(e.Text);
            base.OnDrawTimeScaleHour(e);
        }

        public override void OnDrawTimeScaleMinutes(CalendarRendererBoxEventArgs e)
        {
            Minutes.Add(e.Text);
            base.OnDrawTimeScaleMinutes(e);
        }
    }

    /// <summary>
    /// The time scale labels follow the culture's clock.
    /// </summary>
    [STATestClass]
    public class TimeScaleLabelTests : CalendarTestBase
    {
        private TimeScaleRecordingRenderer DrawFromTenOClock(string culture)
        {
            CultureInfo old = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            try
            {
                var renderer = new TimeScaleRecordingRenderer(Cal);
                Cal.Renderer = renderer;
                Cal.SetViewRange(Monday, Monday);
                Cal.ScrollToTime(10);

                using (var bitmap = new Bitmap(Cal.Width, Cal.Height))
                    Cal.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

                Assert.IsTrue(renderer.Hours.Count >= 6, "Test setup: at least 10:00 to 15:00 should be visible.");
                return renderer;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = old;
            }
        }

        [TestMethod]
        public void TwentyFourHourCulture_ShowsHoursUpTo23WithoutAmPm()
        {
            TimeScaleRecordingRenderer r = DrawFromTenOClock("pl-PL");

            CollectionAssert.AreEqual(new[] { "10", "11", "12", "13", "14", "15" }, r.Hours.GetRange(0, 6), string.Join(",", r.Hours) + " | " + string.Join(",", r.Minutes));
            CollectionAssert.AreEqual(new[] { "00", "00", "00", "00", "00", "00" }, r.Minutes.GetRange(0, 6), string.Join(",", r.Hours) + " | " + string.Join(",", r.Minutes));
        }

        [TestMethod]
        public void TwelveHourCulture_ShowsTwelveHourClockWithAmPm()
        {
            TimeScaleRecordingRenderer r = DrawFromTenOClock("en-US");

            CollectionAssert.AreEqual(new[] { "10", "11", "12", "01", "02", "03" }, r.Hours.GetRange(0, 6), string.Join(",", r.Hours) + " | " + string.Join(",", r.Minutes));
            CollectionAssert.AreEqual(new[] { "AM", "00", "PM", "00", "00", "00" }, r.Minutes.GetRange(0, 6), string.Join(",", r.Hours) + " | " + string.Join(",", r.Minutes));
        }
    }

    /// <summary>
    /// Initialization of <see cref="CalendarProfessionalRenderer"/> on a shown calendar.
    /// </summary>
    [STATestClass]
    public class ProfessionalRendererTests
    {
        private Form _form;

        [TestCleanup]
        public void CloseForm() => _form?.Dispose();

        private Calendar ShowCalendar(Font font)
        {
            _form = new Form { Size = new Size(800, 600), ShowInTaskbar = false };
            var calendar = new Calendar { Dock = DockStyle.Fill };

            if (font != null)
                calendar.Font = font; // as InitializeComponent does

            _form.Controls.Add(calendar);
            _form.Show();
            Assert.IsTrue(calendar.Created, "Test setup: the calendar should be created.");
            return calendar;
        }

        [TestMethod]
        public void ReplacingRendererOnShownCalendar_Works()
        {
            Calendar calendar = ShowCalendar(null);

            calendar.Renderer = new CalendarProfessionalRenderer(calendar); // used to throw NullReferenceException

            Assert.IsFalse(calendar.Renderer.TimeScaleBounds.IsEmpty);
        }

        [TestMethod]
        public void FontSetByApplication_IsKept()
        {
            using (var arial = new Font("Arial", 14f))
            {
                Calendar calendar = ShowCalendar(arial);

                Assert.AreEqual("Arial", calendar.Font.Name);
                Assert.AreEqual(14f, calendar.Font.Size);

                calendar.Renderer = new CalendarProfessionalRenderer(calendar);
                Assert.AreEqual("Arial", calendar.Font.Name, "Replacing the renderer changed the font.");
            }
        }

        [TestMethod]
        public void WithoutFontSetByApplication_RendererFontIsUsed()
        {
            Calendar calendar = ShowCalendar(null);

            Assert.AreEqual(SystemFonts.CaptionFont.Name, calendar.Font.Name);
            Assert.AreEqual(SystemFonts.CaptionFont.Size, calendar.Font.Size);
        }
    }

    /// <summary>
    /// Every <see cref="CalendarRendererBoxEventArgs"/> constructor keeps the values it is given.
    /// </summary>
    [STATestClass]
    public class BoxEventArgsTests : CalendarTestBase
    {
        private CalendarRendererEventArgs Original() => new CalendarRendererEventArgs(Cal, null, Rectangle.Empty);

        [TestMethod]
        public void ConstructorWithTextColorAndFlags_KeepsTextColor()
        {
            var e = new CalendarRendererBoxEventArgs(Original(), new Rectangle(1, 2, 3, 4), "x", Color.Red, TextFormatFlags.Right);

            Assert.AreEqual(Color.Red, e.TextColor);
            Assert.IsTrue((e.Format & TextFormatFlags.Right) == TextFormatFlags.Right);
        }

        [TestMethod]
        public void ConstructorWithTextAndBackgroundColor_KeepsBothColors()
        {
            var e = new CalendarRendererBoxEventArgs(Original(), new Rectangle(1, 2, 3, 4), "x", Color.Red, Color.Blue);

            Assert.AreEqual(Color.Red, e.TextColor);
            Assert.AreEqual(Color.Blue, e.BackgroundColor);
        }

        [TestMethod]
        public void ConstructorWithTextColor_KeepsTextColor()
        {
            var e = new CalendarRendererBoxEventArgs(Original(), new Rectangle(1, 2, 3, 4), "x", Color.Red);

            Assert.AreEqual(Color.Red, e.TextColor);
        }
    }
}
