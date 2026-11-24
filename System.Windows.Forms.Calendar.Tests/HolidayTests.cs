using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Reading holidays from iCalendar (.ics) text.
    /// </summary>
    [TestClass]
    public class HolidayICalendarTests
    {
        private static CalendarHolidays Load(params string[] lines)
        {
            return CalendarHolidays.LoadICalendar(new StringReader(string.Join("\r\n", lines)));
        }

        private static string[] Event(params string[] properties)
        {
            var lines = new List<string> { "BEGIN:VEVENT" };
            lines.AddRange(properties);
            lines.Add("END:VEVENT");
            return lines.ToArray();
        }

        private static CalendarHolidays LoadEvents(params string[][] events)
        {
            var lines = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0" };
            foreach (string[] e in events) lines.AddRange(e);
            lines.Add("END:VCALENDAR");
            return Load(lines.ToArray());
        }

        [TestMethod]
        public void SingleAllDayEvent_IsOneDay()
        {
            CalendarHolidays h = LoadEvents(Event("DTSTART;VALUE=DATE:20261225", "DTEND;VALUE=DATE:20261226", "SUMMARY:Boże Narodzenie"));

            Assert.AreEqual("Boże Narodzenie", h.GetName(new DateTime(2026, 12, 25)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 12, 24)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 12, 26)), "DTEND is exclusive.");
            Assert.IsFalse(h.IsHoliday(new DateTime(2027, 12, 25)), "Without RRULE it does not repeat.");
        }

        [TestMethod]
        public void EventWithoutEnd_IsOneDay()
        {
            CalendarHolidays h = LoadEvents(Event("DTSTART;VALUE=DATE:20260101", "SUMMARY:Nowy Rok"));

            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 1, 1, 15, 30, 0)), "The time of day is ignored.");
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 1, 2)));
        }

        [TestMethod]
        public void MultiDayEvent_CoversAllDays()
        {
            CalendarHolidays h = LoadEvents(Event("DTSTART;VALUE=DATE:20261224", "DTEND;VALUE=DATE:20261227", "SUMMARY:Święta"));

            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 12, 24)));
            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 12, 26)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 12, 27)));
        }

        [TestMethod]
        public void DateTimeValues_UseTheDate()
        {
            CalendarHolidays h = LoadEvents(
                Event("DTSTART:20260501T000000", "DTEND:20260502T000000", "SUMMARY:A"),
                Event("DTSTART;TZID=Europe/Warsaw:20260503T080000", "DTEND;TZID=Europe/Warsaw:20260503T160000", "SUMMARY:B"));

            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 5, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 5, 2)), "An end at midnight is exclusive.");
            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 5, 3)));
        }

        [TestMethod]
        public void YearlyRule_RepeatsFromStartYear()
        {
            CalendarHolidays h = LoadEvents(Event("DTSTART;VALUE=DATE:20251224", "RRULE:FREQ=YEARLY", "SUMMARY:Wigilia"));

            Assert.IsFalse(h.IsHoliday(new DateTime(2024, 12, 24)), "Before DTSTART.");
            Assert.IsTrue(h.IsHoliday(new DateTime(2025, 12, 24)));
            Assert.IsTrue(h.IsHoliday(new DateTime(2040, 12, 24)));
        }

        [TestMethod]
        public void YearlyRule_WithIntervalCountUntilAndExdate()
        {
            CalendarHolidays h = LoadEvents(
                Event("DTSTART;VALUE=DATE:20200301", "RRULE:FREQ=YEARLY;INTERVAL=2", "SUMMARY:Every 2 years"),
                Event("DTSTART;VALUE=DATE:20200401", "RRULE:FREQ=YEARLY;COUNT=2", "SUMMARY:Twice"),
                Event("DTSTART;VALUE=DATE:20200501", "RRULE:FREQ=YEARLY;UNTIL=20211231T235959Z", "SUMMARY:Until 2021"),
                Event("DTSTART;VALUE=DATE:20200601", "RRULE:FREQ=YEARLY;BYMONTH=6;BYMONTHDAY=1", "EXDATE;VALUE=DATE:20210601,20230601", "SUMMARY:Not 2021 and 2023"));

            Assert.IsTrue(h.IsHoliday(new DateTime(2022, 3, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2023, 3, 1)));

            Assert.IsTrue(h.IsHoliday(new DateTime(2021, 4, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2022, 4, 1)));

            Assert.IsTrue(h.IsHoliday(new DateTime(2021, 5, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2022, 5, 1)));

            Assert.IsTrue(h.IsHoliday(new DateTime(2022, 6, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2021, 6, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2023, 6, 1)));
        }

        [TestMethod]
        public void YearlyMultiDayEvent_AcrossNewYear()
        {
            CalendarHolidays h = LoadEvents(Event("DTSTART;VALUE=DATE:20201231", "DTEND;VALUE=DATE:20210102", "RRULE:FREQ=YEARLY", "SUMMARY:Sylwester i Nowy Rok"));

            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 12, 31)));
            Assert.IsTrue(h.IsHoliday(new DateTime(2027, 1, 1)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2027, 1, 2)));
        }

        [TestMethod]
        public void LeapDay_RepeatsOnlyInLeapYears()
        {
            CalendarHolidays h = LoadEvents(Event("DTSTART;VALUE=DATE:20240229", "RRULE:FREQ=YEARLY", "SUMMARY:Leap"));

            Assert.IsTrue(h.IsHoliday(new DateTime(2028, 2, 29)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2027, 2, 28)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2027, 3, 1)));
        }

        [TestMethod]
        public void FoldedLinesEscapesAndParameters_AreRead()
        {
            CalendarHolidays h = Load(
                "BEGIN:VCALENDAR",
                "BEGIN:VEVENT",
                "DTSTART;VALUE=DATE:20260815",
                "SUMMARY;LANGUAGE=pl:Wniebowzięcie Najświętszej",
                "  Maryi Panny\\, święto\\; dzień wolny",
                "END:VEVENT",
                "END:VCALENDAR");

            Assert.AreEqual("Wniebowzięcie Najświętszej Maryi Panny, święto; dzień wolny", h.GetName(new DateTime(2026, 8, 15)));
        }

        [TestMethod]
        public void PropertiesOfNestedComponents_AreIgnored()
        {
            CalendarHolidays h = LoadEvents(Event(
                "DTSTART;VALUE=DATE:20261111",
                "SUMMARY:Niepodległość",
                "BEGIN:VALARM",
                "SUMMARY:Reminder",
                "TRIGGER:-PT15M",
                "END:VALARM"));

            Assert.AreEqual("Niepodległość", h.GetName(new DateTime(2026, 11, 11)));
        }

        [TestMethod]
        public void CancelledEventsAndOtherComponents_AreSkipped()
        {
            CalendarHolidays h = Load(
                "BEGIN:VCALENDAR",
                "BEGIN:VTIMEZONE", "TZID:Europe/Warsaw", "BEGIN:STANDARD", "DTSTART:19701025T030000", "END:STANDARD", "END:VTIMEZONE",
                "BEGIN:VEVENT", "DTSTART;VALUE=DATE:20260102", "STATUS:CANCELLED", "SUMMARY:X", "END:VEVENT",
                "END:VCALENDAR");

            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 1, 2)));
            Assert.IsFalse(h.IsHoliday(new DateTime(1970, 10, 25)));
        }

        [TestMethod]
        public void SeveralHolidaysOnOneDay_NamesAreJoined()
        {
            CalendarHolidays h = LoadEvents(
                Event("DTSTART;VALUE=DATE:20260405", "SUMMARY:Wielkanoc"),
                Event("DTSTART;VALUE=DATE:20260405", "SUMMARY:Prima Aprilis po terminie"));

            Assert.AreEqual("Wielkanoc, Prima Aprilis po terminie", h.GetName(new DateTime(2026, 4, 5)));
        }

        [TestMethod]
        [DataRow("RRULE:FREQ=MONTHLY", DisplayName = "Monthly")]
        [DataRow("RRULE:FREQ=YEARLY;BYMONTH=5;BYDAY=2SU", DisplayName = "Second Sunday of May")]
        [DataRow("RRULE:FREQ=YEARLY;BYMONTHDAY=2", DisplayName = "Other day than DTSTART")]
        public void UnsupportedRule_ThrowsWithLineNumber(string rule)
        {
            var ex = Assert.ThrowsExactly<FormatException>(() =>
                LoadEvents(Event("DTSTART;VALUE=DATE:20260501", rule, "SUMMARY:X")));

            StringAssert.Contains(ex.Message, "line 5");
        }

        [TestMethod]
        public void InvalidFiles_ThrowWithLineNumber()
        {
            StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() =>
                LoadEvents(Event("SUMMARY:No start"))).Message, "line 3");

            StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() =>
                LoadEvents(Event("DTSTART;VALUE=DATE:2026-05-01"))).Message, "line 4");

            StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() =>
                Load("BEGIN:VCALENDAR", "BEGIN:VEVENT", "DTSTART;VALUE=DATE:20260501", "END:VCALENDAR")).Message, "line 4");

            StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() =>
                Load("BEGIN:VCALENDAR", "no colon here", "END:VCALENDAR")).Message, "line 2");
        }

        [TestMethod]
        public void LoadFromFile_ReadsUtf8WithBom()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ics");

            try
            {
                File.WriteAllText(path,
                    "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nDTSTART;VALUE=DATE:20261101\r\nSUMMARY:Wszystkich Świętych\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n",
                    new UTF8Encoding(true));

                Assert.AreEqual("Wszystkich Świętych", CalendarHolidays.LoadICalendar(path).GetName(new DateTime(2026, 11, 1)));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void DemoHolidaysFile_IsValidAndHasPolishHolidays()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CalendarDemo", "holidays.ics");
            Assert.IsTrue(File.Exists(path), "Test setup: " + path);

            CalendarHolidays h = CalendarHolidays.LoadICalendar(path);

            Assert.AreEqual("Boże Narodzenie", h.GetName(new DateTime(2026, 12, 26)));
            Assert.AreEqual("Wigilia Bożego Narodzenia", h.GetName(new DateTime(2026, 12, 24)));
            Assert.AreEqual("Poniedziałek Wielkanocny", h.GetName(new DateTime(2027, 3, 29)));
            Assert.AreEqual("Boże Ciało", h.GetName(new DateTime(2026, 6, 4)));
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 11, 24)));
        }

        [TestMethod]
        public void AddedFromCode_YearlyAndOneTime()
        {
            var h = new CalendarHolidays();
            int changes = 0;
            h.Changed += (s, e) => changes++;

            h.AddYearly(5, 3, "3 Maja");
            h.Add(new DateTime(2026, 11, 2, 10, 0, 0), "Dzień wolny");
            h.AddYearly(2, 29, "Leap");

            Assert.IsTrue(h.IsHoliday(new DateTime(1999, 5, 3)));
            Assert.IsTrue(h.IsHoliday(new DateTime(2026, 11, 2)));
            Assert.IsTrue(h.IsHoliday(new DateTime(2028, 2, 29)));
            Assert.AreEqual(3, changes);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => h.AddYearly(2, 30, "X"));

            h.Clear();
            Assert.IsFalse(h.IsHoliday(new DateTime(2026, 5, 3)));
        }
    }

    /// <summary>
    /// Holidays in the <see cref="Calendar"/> control.
    /// </summary>
    [STATestClass]
    public class CalendarHolidayTests : CalendarTestBase
    {
        private static readonly DateTime Wednesday = new DateTime(2026, 11, 25);

        private CalendarHolidays HolidayOnWednesday()
        {
            var holidays = new CalendarHolidays();
            holidays.Add(Wednesday, "Testowe święto");
            Cal.Holidays = holidays;
            return holidays;
        }

        private Bitmap Snapshot()
        {
            var bitmap = new Bitmap(Cal.Width, Cal.Height);
            Cal.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            return bitmap;
        }

        private static Point Center(Rectangle r) => new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);

        [TestMethod]
        public void Day_KnowsItsHoliday()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            HolidayOnWednesday();

            CalendarDay wednesday = Cal.FindDay(Wednesday);
            Assert.IsTrue(wednesday.IsHoliday);
            Assert.AreEqual("Testowe święto", wednesday.HolidayName);
            Assert.IsFalse(Cal.FindDay(Monday).IsHoliday);
        }

        [TestMethod]
        public void WorkTime_IsNotHighlightedOnHoliday()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Assert.IsTrue(Cal.GetTimeUnit(Wednesday.AddHours(10)).Highlighted, "Test setup");

            HolidayOnWednesday();

            Assert.IsFalse(Cal.GetTimeUnit(Wednesday.AddHours(10)).Highlighted);
            Assert.IsTrue(Cal.GetTimeUnit(Monday.AddHours(10)).Highlighted);
        }

        [TestMethod]
        public void ChangingHolidays_UpdatesHighlightsAndRepaints()
        {
            Assert.AreNotEqual(IntPtr.Zero, Cal.Handle);
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            CalendarHolidays holidays = HolidayOnWednesday();
            bool repainted = false;
            Cal.Invalidated += (s, e) => repainted = true;

            holidays.Add(Monday, "Drugie");

            Assert.IsTrue(repainted);
            Assert.IsFalse(Cal.GetTimeUnit(Monday.AddHours(10)).Highlighted);
        }

        [TestMethod]
        public void WeekView_HolidayTimeUnitsHaveHolidayColor()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            Cal.ScrollToTime(9);
            HolidayOnWednesday();
            var renderer = (CalendarSystemRenderer)Cal.Renderer;

            using (Bitmap bitmap = Snapshot())
            {
                // Just right of the unit's top border line, inside the unit.
                Rectangle unit = Cal.GetTimeUnit(Wednesday.AddHours(10)).Bounds;
                Rectangle normal = Cal.GetTimeUnit(Monday.AddHours(10)).Bounds;

                Assert.AreEqual(renderer.ColorTable.HolidayTimeUnitBackground.ToArgb(), bitmap.GetPixel(unit.Left + 5, unit.Top + 5).ToArgb(), "Holiday");
                Assert.AreNotEqual(renderer.ColorTable.HolidayTimeUnitBackground.ToArgb(), bitmap.GetPixel(normal.Left + 5, normal.Top + 5).ToArgb(), "Ordinary day");
            }
        }

        [TestMethod]
        public void MonthView_HolidayHasHolidayBackground()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            HolidayOnWednesday();
            var renderer = (CalendarSystemRenderer)Cal.Renderer;

            using (Bitmap bitmap = Snapshot())
            {
                Rectangle day = Cal.FindDay(Wednesday).Bounds;
                Point p = new Point(day.Left + 5, day.Bottom - 5); // below the header, where no item is

                Assert.AreEqual(renderer.ColorTable.HolidayBackground.ToArgb(), bitmap.GetPixel(p.X, p.Y).ToArgb());
            }
        }

        [TestMethod]
        [DataRow(6, DisplayName = "Week view")]
        [DataRow(27, DisplayName = "Month view")]
        public void HolidayName_IsDrawnInDayHeader(int lastDay)
        {
            var renderer = new HolidayNameRecordingRenderer(Cal);
            Cal.Renderer = renderer;
            Cal.SetViewRange(Monday, Monday.AddDays(lastDay));
            HolidayOnWednesday();

            using (Snapshot()) { }

            Assert.AreEqual(1, renderer.Names.Count, string.Join(" | ", renderer.Names));
            StringAssert.Contains(renderer.Names[0], "Testowe święto");
            Assert.AreSame(Cal.FindDay(Wednesday), renderer.Days[0]);
        }

        [TestMethod]
        public void WithoutHolidays_NothingChanges()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));

            Assert.IsNull(Cal.Holidays);
            Assert.IsNull(Cal.FindDay(Wednesday).HolidayName);
            Assert.IsTrue(Cal.GetTimeUnit(Wednesday.AddHours(10)).Highlighted);
        }

        [TestMethod]
        public void DisposedCalendar_IsNotNotifiedAnyMore()
        {
            CalendarHolidays holidays = HolidayOnWednesday();
            DisposeCalendar();

            holidays.Add(Monday, "After dispose"); // must not touch the disposed control
        }
    }

    internal class HolidayNameRecordingRenderer : CalendarProfessionalRenderer
    {
        public readonly List<string> Names = new List<string>();
        public readonly List<CalendarDay> Days = new List<CalendarDay>();

        public HolidayNameRecordingRenderer(Calendar calendar) : base(calendar) { }

        public override void OnDrawDayHolidayName(CalendarRendererBoxEventArgs e)
        {
            Names.Add(e.Text);
            Days.Add((CalendarDay)e.Tag);
            base.OnDrawDayHolidayName(e);
        }
    }

    /// <summary>
    /// Holidays in the <see cref="MonthView"/> control.
    /// </summary>
    [STATestClass]
    public class MonthViewHolidayTests
    {
        private Form _form;
        private TestMonthView _view;

        [TestInitialize]
        public void CreateView()
        {
            _form = new Form { Size = new Size(300, 400) };
            _view = new TestMonthView { Dock = DockStyle.Fill };
            _form.Controls.Add(_view);
            _form.CreateControl();
            _view.CreateControl();
            _view.ViewStart = new DateTime(2026, 11, 1);
            _view.SelectionStart = _view.SelectionEnd = new DateTime(2026, 11, 2);

            var holidays = new CalendarHolidays();
            holidays.Add(new DateTime(2026, 11, 11), "Narodowe Święto Niepodległości");
            _view.Holidays = holidays;
        }

        [TestCleanup]
        public void DisposeView() => _form?.Dispose();

        private Rectangle DayBounds(int dayOfNovember)
        {
            foreach (MonthViewDay day in _view.Months[0].Days)
            {
                if (day.Date == new DateTime(2026, 11, dayOfNovember)) return day.Bounds;
            }

            throw new AssertFailedException("Test setup: day not shown.");
        }

        [TestMethod]
        public void Holiday_HasHolidayBackground()
        {
            using (var bitmap = new Bitmap(_view.Width, _view.Height))
            {
                _view.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

                Rectangle holiday = DayBounds(11);
                Rectangle ordinary = DayBounds(12);

                Assert.AreEqual(_view.HolidayBackgroundColor.ToArgb(), bitmap.GetPixel(holiday.Left + 1, holiday.Top + 1).ToArgb());
                Assert.AreNotEqual(_view.HolidayBackgroundColor.ToArgb(), bitmap.GetPixel(ordinary.Left + 1, ordinary.Top + 1).ToArgb());
            }
        }

        [TestMethod]
        public void MouseOverHoliday_ShowsItsNameAsToolTip()
        {
            Rectangle holiday = DayBounds(11);
            Rectangle ordinary = DayBounds(12);

            _view.MouseMoveTo(new Point(holiday.Left + holiday.Width / 2, holiday.Top + holiday.Height / 2));
            Assert.AreEqual("Narodowe Święto Niepodległości", _view.HolidayToolTipText);

            _view.MouseMoveTo(new Point(ordinary.Left + ordinary.Width / 2, ordinary.Top + ordinary.Height / 2));
            Assert.IsTrue(string.IsNullOrEmpty(_view.HolidayToolTipText));
        }

        [TestMethod]
        public void ChangingHolidays_Repaints()
        {
            Assert.AreNotEqual(IntPtr.Zero, _view.Handle);
            bool repainted = false;
            _view.Invalidated += (s, e) => repainted = true;

            _view.Holidays.Add(new DateTime(2026, 11, 12), "X");

            Assert.IsTrue(repainted);
        }
    }

    /// <summary>
    /// Listing holidays, reading text with a BOM, and the holiday tooltip of <see cref="Calendar"/>.
    /// </summary>
    [STATestClass]
    public class HolidayImprovementTests : CalendarTestBase
    {
        [TestMethod]
        public void TextReaderWithBom_IsRead()
        {
            CalendarHolidays h = CalendarHolidays.LoadICalendar(new StringReader(
                (char)0xFEFF + "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nDTSTART;VALUE=DATE:20261101\r\nSUMMARY:Wszystkich Świętych\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n"));

            Assert.AreEqual("Wszystkich Świętych", h.GetName(new DateTime(2026, 11, 1)));
        }

        [TestMethod]
        public void GetHolidays_ListsEveryDayInRangeInOrder()
        {
            var h = new CalendarHolidays();
            h.AddYearly(12, 25, "Boże Narodzenie");
            h.Add(new DateTime(2026, 12, 31), "Sylwester (wolne)");
            h.AddICalendar(new StringReader(
                "BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nDTSTART;VALUE=DATE:20261224\r\nDTEND;VALUE=DATE:20261227\r\nSUMMARY:Święta\r\nEND:VEVENT\r\nEND:VCALENDAR"));

            List<CalendarHoliday> list = h.GetHolidays(new DateTime(2026, 12, 20), new DateTime(2027, 12, 25, 18, 0, 0));
            var text = new List<string>();
            foreach (CalendarHoliday holiday in list) text.Add($"{holiday.Date:yyyy-MM-dd} {holiday.Name}");

            CollectionAssert.AreEqual(new[]
            {
                "2026-12-24 Święta",
                "2026-12-25 Boże Narodzenie",
                "2026-12-25 Święta",
                "2026-12-26 Święta",
                "2026-12-31 Sylwester (wolne)",
                "2027-12-25 Boże Narodzenie",
            }, text, string.Join(" | ", text));
        }

        [TestMethod]
        public void GetHolidays_EndBeforeStart_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new CalendarHolidays().GetHolidays(new DateTime(2026, 2, 1), new DateTime(2026, 1, 1)));
        }

        [TestMethod]
        [DataRow(6, DisplayName = "Week view")]
        [DataRow(27, DisplayName = "Month view")]
        public void MouseOverHolidayHeader_ShowsFullNameAsToolTip(int lastDay)
        {
            Cal.SetViewRange(Monday, Monday.AddDays(lastDay));
            var holidays = new CalendarHolidays();
            DateTime wednesday = Monday.AddDays(2);
            holidays.Add(wednesday, "Bardzo długa nazwa święta, która nie mieści się w nagłówku");
            Cal.Holidays = holidays;

            Rectangle header = Cal.FindDay(wednesday).HeaderBounds;
            Rectangle otherHeader = Cal.FindDay(Monday).HeaderBounds;
            Rectangle body = Cal.FindDay(wednesday).Bounds;

            Cal.MouseMoveTo(new Point(header.Left + header.Width / 2, header.Top + header.Height / 2));
            Assert.AreEqual("Bardzo długa nazwa święta, która nie mieści się w nagłówku", Cal.HolidayToolTipText);

            Cal.MouseMoveTo(new Point(otherHeader.Left + otherHeader.Width / 2, otherHeader.Top + otherHeader.Height / 2));
            Assert.IsTrue(string.IsNullOrEmpty(Cal.HolidayToolTipText), "Not a holiday.");

            Cal.MouseMoveTo(new Point(body.Left + body.Width / 2, body.Bottom - 5));
            Assert.IsTrue(string.IsNullOrEmpty(Cal.HolidayToolTipText), "Only the header shows the tooltip.");
        }

        [TestMethod]
        public void ToolTip_FollowsChangedHolidays()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(6));
            var holidays = new CalendarHolidays();
            Cal.Holidays = holidays;
            Rectangle header = Cal.FindDay(Monday).HeaderBounds;
            Point p = new Point(header.Left + header.Width / 2, header.Top + header.Height / 2);

            Cal.MouseMoveTo(p);
            Assert.IsTrue(string.IsNullOrEmpty(Cal.HolidayToolTipText));

            holidays.Add(Monday, "Nowe");
            Cal.MouseMoveTo(p);

            Assert.AreEqual("Nowe", Cal.HolidayToolTipText);
        }
    }
}
