using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Windows.Forms.Calendar
{
    /// <summary>
    /// A set of holidays (days off) shown with a different background by <see cref="Calendar"/> and
    /// <see cref="MonthView"/>. One instance can be shared by several controls.
    /// </summary>
    /// <remarks>
    /// Holidays can be added from code (<see cref="Add"/>, <see cref="AddYearly"/>) or loaded from an
    /// iCalendar (.ics, RFC 5545) file with <see cref="LoadICalendar(string)"/>. From such a file every
    /// VEVENT becomes a holiday: SUMMARY is its name, DTSTART its first day and DTEND (exclusive, as in
    /// all-day events) its end. Yearly recurrence (RRULE:FREQ=YEARLY, with INTERVAL, COUNT, UNTIL and
    /// BYMONTH/BYMONTHDAY matching DTSTART) and EXDATE are supported; other recurrence rules are reported
    /// as errors instead of being silently ignored. Times and time zones are ignored: only the date written
    /// in the file counts.
    /// </remarks>
    public class CalendarHolidays
    {
        #region Entry

        /// <summary>
        /// One holiday (possibly lasting several days, possibly repeating every year).
        /// </summary>
        private sealed class Entry
        {
            public DateTime Start;
            public int Days = 1;
            public string Name;
            public bool Yearly;
            public int Interval = 1;
            public int Count;              // 0 = unlimited
            public DateTime Until = DateTime.MaxValue;
            public readonly HashSet<DateTime> Excluded = new HashSet<DateTime>();

            public bool Contains(DateTime date)
            {
                if (!Yearly)
                    return date >= Start && date < Start.AddDays(Days);

                // An occurrence that started this year or (if it lasts several days) in an earlier one.
                int yearsBack = Days / 365 + 1;

                for (int year = date.Year - yearsBack; year <= date.Year; year++)
                {
                    int sinceStart = year - Start.Year;
                    if (sinceStart < 0 || sinceStart % Interval != 0) continue;
                    if (Count > 0 && sinceStart / Interval >= Count) continue;

                    // E.g. 29 February repeats only in leap years.
                    if (Start.Month == 2 && Start.Day == 29 && !DateTime.IsLeapYear(year)) continue;

                    DateTime occurrence = new DateTime(year, Start.Month, Start.Day);
                    if (occurrence > Until || Excluded.Contains(occurrence)) continue;

                    if (date >= occurrence && date < occurrence.AddDays(Days))
                        return true;
                }

                return false;
            }
        }

        #endregion

        #region Fields

        private readonly List<Entry> _entries = new List<Entry>();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when holidays are added or removed. Controls using this set repaint themselves.
        /// </summary>
        public event EventHandler Changed;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a one-time holiday.
        /// </summary>
        /// <param name="date">Day of the holiday (the time of day is ignored).</param>
        /// <param name="name">Name shown in the day header; may be empty.</param>
        public void Add(DateTime date, string name)
        {
            _entries.Add(new Entry { Start = date.Date, Name = name ?? string.Empty });
            OnChanged();
        }

        /// <summary>
        /// Adds a holiday repeated every year on the same day, e.g. 25 December.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The month and day are not a valid date.</exception>
        public void AddYearly(int month, int day, string name)
        {
            // The rule starts in year 4, a leap year, so 29 February is valid too (and repeats in leap years only).
            DateTime start;
            try
            {
                start = new DateTime(4, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("day", string.Format("{0:00}-{1:00} is not a valid day of the year.", month, day));
            }

            _entries.Add(new Entry { Start = start, Name = name ?? string.Empty, Yearly = true });
            OnChanged();
        }

        /// <summary>
        /// Removes all holidays.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            OnChanged();
        }

        /// <summary>
        /// Gets a value indicating if the specified day is a holiday.
        /// </summary>
        public bool IsHoliday(DateTime date)
        {
            date = date.Date;

            foreach (Entry entry in _entries)
            {
                if (entry.Contains(date)) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the holidays from <paramref name="from"/> to <paramref name="to"/> (both days included),
        /// one entry per day and holiday, ordered by date: yearly holidays appear once for every year
        /// in the range, and a holiday lasting several days once for each of its days.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="to"/> is before <paramref name="from"/>.</exception>
        public List<CalendarHoliday> GetHolidays(DateTime from, DateTime to)
        {
            from = from.Date;
            to = to.Date;

            if (to < from) throw new ArgumentException("The end of the range is before its start.", "to");

            List<CalendarHoliday> result = new List<CalendarHoliday>();

            for (DateTime day = from; day <= to; day = day.AddDays(1))
            {
                foreach (Entry entry in _entries)
                {
                    if (entry.Contains(day))
                        result.Add(new CalendarHoliday(day, entry.Name));
                }

                if (day == DateTime.MaxValue.Date) break;
            }

            return result;
        }

        /// <summary>
        /// Gets the name of the holiday on the specified day (names of several holidays on the same day
        /// are joined with ", "), or <c>null</c> if the day is not a holiday.
        /// </summary>
        public string GetName(DateTime date)
        {
            date = date.Date;
            List<string> names = null;

            foreach (Entry entry in _entries)
            {
                if (!entry.Contains(date)) continue;

                if (names == null) names = new List<string>();
                if (!names.Contains(entry.Name)) names.Add(entry.Name);
            }

            if (names == null) return null;

            names.RemoveAll(string.IsNullOrEmpty);
            return string.Join(", ", names.ToArray());
        }

        /// <summary>
        /// Loads holidays from an iCalendar (.ics) file encoded in UTF-8.
        /// </summary>
        /// <exception cref="FormatException">The file is not a valid iCalendar file or uses a recurrence
        /// rule that is not supported; the message gives the line number.</exception>
        public static CalendarHolidays LoadICalendar(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true))
            {
                return LoadICalendar(reader);
            }
        }

        /// <summary>
        /// Loads holidays from iCalendar (.ics) text.
        /// </summary>
        /// <exception cref="FormatException">The text is not valid iCalendar or uses a recurrence rule that
        /// is not supported; the message gives the line number.</exception>
        public static CalendarHolidays LoadICalendar(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            CalendarHolidays holidays = new CalendarHolidays();
            holidays.AddICalendar(reader);
            return holidays;
        }

        /// <summary>
        /// Adds holidays from iCalendar (.ics) text to this set.
        /// </summary>
        /// <exception cref="FormatException">The text is not valid iCalendar or uses a recurrence rule that
        /// is not supported; the message gives the line number. Nothing is added in that case.</exception>
        public void AddICalendar(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            List<Entry> parsed = ICalendarParser.Parse(reader);
            _entries.AddRange(parsed);
            OnChanged();
        }

        #endregion

        #region Private Methods

        private void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        #endregion

        #region iCalendar

        /// <summary>
        /// Reads the VEVENT components of iCalendar text (the subset of RFC 5545 that holiday calendars use).
        /// </summary>
        private static class ICalendarParser
        {
            private sealed class Property
            {
                public string Name;
                public Dictionary<string, string> Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                public string Value;
                public int Line;
            }

            public static List<Entry> Parse(TextReader reader)
            {
                List<Entry> result = new List<Entry>();
                Stack<string> components = new Stack<string>();
                List<Property> eventProperties = null;
                int eventLine = 0;

                foreach (Property p in ReadProperties(reader))
                {
                    if (p.Name == "BEGIN")
                    {
                        string component = p.Value.Trim().ToUpperInvariant();
                        components.Push(component);

                        if (component == "VEVENT" && components.Count >= 1 && eventProperties == null)
                        {
                            eventProperties = new List<Property>();
                            eventLine = p.Line;
                        }
                    }
                    else if (p.Name == "END")
                    {
                        string component = p.Value.Trim().ToUpperInvariant();

                        if (components.Count == 0 || components.Peek() != component)
                            throw Error(p.Line, string.Format("END:{0} without a matching BEGIN.", component));

                        components.Pop();

                        if (component == "VEVENT" && !components.Contains("VEVENT"))
                        {
                            Entry entry = ToEntry(eventProperties, eventLine);
                            if (entry != null) result.Add(entry);
                            eventProperties = null;
                        }
                    }
                    else if (components.Count > 0 && components.Peek() == "VEVENT" && eventProperties != null)
                    {
                        // Properties of nested components (e.g. VALARM) are not the event's.
                        eventProperties.Add(p);
                    }
                }

                if (components.Count > 0)
                    throw Error(eventLine, string.Format("BEGIN:{0} is not closed.", components.Peek()));

                return result;
            }

            /// <summary>
            /// Unfolds the lines (a line starting with a space or tab continues the previous one)
            /// and splits them into name, parameters and value.
            /// </summary>
            private static IEnumerable<Property> ReadProperties(TextReader reader)
            {
                StringBuilder logical = null;
                int logicalLine = 0;
                int lineNumber = 0;
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    // Text read without encoding detection (e.g. a string) may still start with the UTF-8 BOM.
                    if (lineNumber == 1 && line.Length > 0 && line[0] == (char)0xFEFF)
                        line = line.Substring(1);

                    if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t'))
                    {
                        if (logical == null) throw Error(lineNumber, "A continuation line without a property before it.");
                        logical.Append(line, 1, line.Length - 1);
                        continue;
                    }

                    if (logical != null && logical.Length > 0)
                        yield return Split(logical.ToString(), logicalLine);

                    logical = new StringBuilder(line);
                    logicalLine = lineNumber;
                }

                if (logical != null && logical.Length > 0)
                    yield return Split(logical.ToString(), logicalLine);
            }

            private static Property Split(string text, int line)
            {
                // The value starts after the first ':' that is not inside a quoted parameter value.
                bool quoted = false;
                int colon = -1;

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '"') quoted = !quoted;
                    else if (text[i] == ':' && !quoted) { colon = i; break; }
                }

                if (colon < 0) throw Error(line, string.Format("\"{0}\" is not a property (no ':').", text));

                string[] head = text.Substring(0, colon).Split(';');
                Property p = new Property { Name = head[0].Trim().ToUpperInvariant(), Value = text.Substring(colon + 1), Line = line };

                for (int i = 1; i < head.Length; i++)
                {
                    int eq = head[i].IndexOf('=');
                    if (eq > 0)
                        p.Parameters[head[i].Substring(0, eq).Trim()] = head[i].Substring(eq + 1).Trim('"');
                }

                return p;
            }

            private static Entry ToEntry(List<Property> properties, int line)
            {
                Property start = Find(properties, "DTSTART");
                if (start == null) throw Error(line, "VEVENT without DTSTART.");

                Property status = Find(properties, "STATUS");
                if (status != null && status.Value.Trim().Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                    return null;

                Entry entry = new Entry();
                bool startIsDate;
                entry.Start = ParseDate(start, out startIsDate);

                Property summary = Find(properties, "SUMMARY");
                entry.Name = summary != null ? Unescape(summary.Value).Trim() : string.Empty;

                Property end = Find(properties, "DTEND");
                if (end != null)
                {
                    bool endIsDate;
                    DateTime endDate = ParseDate(end, out endIsDate);
                    bool endHasTime = !endIsDate && !end.Value.Trim().EndsWith("T000000", StringComparison.OrdinalIgnoreCase)
                                                 && !end.Value.Trim().EndsWith("T000000Z", StringComparison.OrdinalIgnoreCase);

                    // DTEND of all-day events is exclusive (the day after the last day).
                    DateTime lastDay = endHasTime ? endDate : endDate.AddDays(-1);
                    entry.Days = Math.Max(1, (lastDay - entry.Start).Days + 1);
                }

                Property rule = Find(properties, "RRULE");
                if (rule != null) ApplyRule(entry, rule);

                foreach (Property p in properties)
                {
                    if (p.Name != "EXDATE") continue;

                    foreach (string value in p.Value.Split(','))
                    {
                        if (value.Trim().Length == 0) continue;
                        bool isDate;
                        entry.Excluded.Add(ParseDate(new Property { Name = "EXDATE", Value = value, Line = p.Line, Parameters = p.Parameters }, out isDate));
                    }
                }

                return entry;
            }

            private static void ApplyRule(Entry entry, Property rule)
            {
                Dictionary<string, string> parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (string part in rule.Value.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq > 0) parts[part.Substring(0, eq).Trim()] = part.Substring(eq + 1).Trim();
                }

                string freq;
                if (!parts.TryGetValue("FREQ", out freq) || !freq.Equals("YEARLY", StringComparison.OrdinalIgnoreCase))
                    throw Error(rule.Line, string.Format("RRULE \"{0}\" is not supported: only FREQ=YEARLY is.", rule.Value));

                entry.Yearly = true;

                foreach (KeyValuePair<string, string> part in parts)
                {
                    switch (part.Key.ToUpperInvariant())
                    {
                        case "FREQ":
                        case "WKST":
                            break;
                        case "INTERVAL":
                            entry.Interval = PositiveNumber(part.Value, rule);
                            break;
                        case "COUNT":
                            entry.Count = PositiveNumber(part.Value, rule);
                            break;
                        case "UNTIL":
                            bool isDate;
                            entry.Until = ParseDate(new Property { Name = "UNTIL", Value = part.Value, Line = rule.Line }, out isDate);
                            break;
                        case "BYMONTH":
                            if (part.Value != entry.Start.Month.ToString(CultureInfo.InvariantCulture))
                                throw Unsupported(rule);
                            break;
                        case "BYMONTHDAY":
                            if (part.Value != entry.Start.Day.ToString(CultureInfo.InvariantCulture))
                                throw Unsupported(rule);
                            break;
                        default:
                            // E.g. BYDAY ("second Sunday of May") would need more than the same date every year.
                            throw Unsupported(rule);
                    }
                }
            }

            private static int PositiveNumber(string value, Property rule)
            {
                int n;
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out n) || n < 1)
                    throw Error(rule.Line, string.Format("\"{0}\" in RRULE is not a positive number.", value));
                return n;
            }

            private static Exception Unsupported(Property rule)
            {
                return Error(rule.Line, string.Format(
                    "RRULE \"{0}\" is not supported: only yearly repetition on the date of DTSTART is.", rule.Value));
            }

            /// <summary>
            /// Reads a DATE (yyyyMMdd) or DATE-TIME (yyyyMMddTHHmmss, optionally with Z) value; only the date is kept.
            /// </summary>
            private static DateTime ParseDate(Property p, out bool isDate)
            {
                string value = p.Value.Trim();
                isDate = value.Length == 8;

                DateTime date;
                if (value.Length < 8 || (value.Length > 8 && value[8] != 'T' && value[8] != 't')
                    || !DateTime.TryParseExact(value.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    throw Error(p.Line, string.Format("\"{0}\" in {1} is not a date (expected yyyyMMdd or yyyyMMddTHHmmss).", value, p.Name));
                }

                return date;
            }

            private static Property Find(List<Property> properties, string name)
            {
                foreach (Property p in properties)
                {
                    if (p.Name == name) return p;
                }

                return null;
            }

            /// <summary>
            /// Undoes the escaping of TEXT values (\\, \;, \, and \n).
            /// </summary>
            private static string Unescape(string text)
            {
                StringBuilder sb = new StringBuilder(text.Length);

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\\' && i + 1 < text.Length)
                    {
                        char next = text[++i];
                        sb.Append(next == 'n' || next == 'N' ? ' ' : next);
                    }
                    else
                    {
                        sb.Append(text[i]);
                    }
                }

                return sb.ToString();
            }

            private static FormatException Error(int line, string message)
            {
                return new FormatException(string.Format("iCalendar line {0}: {1}", line, message));
            }
        }

        #endregion
    }
}
