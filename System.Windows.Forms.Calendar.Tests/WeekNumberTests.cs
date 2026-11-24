using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Week numbers in the headers of the month view.
    /// </summary>
    [STATestClass]
    public class WeekNumberTests : CalendarTestBase
    {
        private CultureInfo _oldCulture;

        [TestInitialize]
        public void RememberCulture() => _oldCulture = Thread.CurrentThread.CurrentCulture;

        [TestCleanup]
        public void RestoreCulture() => Thread.CurrentThread.CurrentCulture = _oldCulture;

        /// <summary>Shows 21.12.2026 - 10.01.2027 and returns "first day of row: header" for each row.</summary>
        private List<string> WeekNumbers(string culture, DayOfWeek firstDay)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Cal.FirstDayOfWeek = firstDay;
            Cal.SetViewRange(new DateTime(2026, 12, 21), new DateTime(2027, 1, 10));

            var result = new List<string>();
            foreach (CalendarWeek week in Cal.Weeks)
                result.Add($"{week.StartDate:dd.MM}: {week.ToStringWeekNumber()}");

            return result;
        }

        [TestMethod]
        public void IsoCulture_WeeksStartingOnSunday_HaveIsoNumbers()
        {
            // ISO weeks: 21-27.12 is 52, 28.12-3.01 is 53, 4-10.01.2027 is 1.
            CollectionAssert.AreEqual(
                new[] { "20.12: Week 52", "27.12: Week 53", "03.01: Week 1", "10.01: Week 2" },
                WeekNumbers("pl-PL", DayOfWeek.Sunday));
        }

        [TestMethod]
        public void IsoCulture_WeeksStartingOnMonday_HaveIsoNumbers()
        {
            CollectionAssert.AreEqual(
                new[] { "21.12: KW 52", "28.12: KW 53", "04.01: KW 1" },
                WeekNumbers("de-DE", DayOfWeek.Monday));
        }

        [TestMethod]
        public void UsCulture_WeeksStartingOnSunday_HaveUsNumbers()
        {
            // en-US: weeks start on Sunday and week 1 is the week of 1 January.
            CollectionAssert.AreEqual(
                new[] { "20.12: Week 52", "27.12: Week 53", "03.01: Week 2", "10.01: Week 3" },
                WeekNumbers("en-US", DayOfWeek.Sunday));
        }

        [TestMethod]
        public void WithoutFormat_UsesPreviousLabels()
        {
            WeekNumbers("de-DE", DayOfWeek.Monday);
            Assert.AreEqual("KW 52", Cal.Weeks[0].ToStringWeekNumber());

            WeekNumbers("pl-PL", DayOfWeek.Monday);
            Assert.AreEqual("Week 52", Cal.Weeks[0].ToStringWeekNumber());
        }

        [TestMethod]
        public void WeekNumberFormat_IsUsedForHeaders()
        {
            WeekNumbers("pl-PL", DayOfWeek.Monday);

            Cal.WeekNumberFormat = "Tydz. {0}";

            Assert.AreEqual("Tydz. 52", Cal.Weeks[0].ToStringWeekNumber());
        }

        [TestMethod]
        public void InvalidWeekNumberFormat_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Cal.WeekNumberFormat = "Week {1");
            Assert.IsNull(Cal.WeekNumberFormat);
        }
    }
}
