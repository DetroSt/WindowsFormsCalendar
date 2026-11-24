using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Forms.Calendar.Tests
{
    /// <summary>
    /// Renderer that records what it draws, so tests can count text and pattern drawing.
    /// </summary>
    internal class RecordingRenderer : CalendarProfessionalRenderer
    {
        public readonly List<Rectangle> TextBounds = new List<Rectangle>();
        public int PatternCalls;

        public RecordingRenderer(Calendar calendar) : base(calendar) { }

        public override void OnDrawItemText(CalendarRendererBoxEventArgs e)
        {
            TextBounds.Add(e.Bounds);
            base.OnDrawItemText(e);
        }

        public override void OnDrawItemPattern(CalendarRendererItemBoundsEventArgs e)
        {
            PatternCalls++;
            base.OnDrawItemPattern(e);
        }
    }

    /// <summary>
    /// Where and how items are drawn after they are added or removed.
    /// </summary>
    [STATestClass]
    public class DrawingTests : CalendarTestBase
    {
        private CalendarItem At(int day, int startMinute, int endMinute, string text)
        {
            DateTime d = Monday.AddDays(day);
            return new CalendarItem(Cal, d.AddMinutes(startMinute), d.AddMinutes(endMinute), text);
        }

        [TestMethod]
        public void ItemsInSameTimeUnit_WithoutCommonTime_DoNotCoverEachOther()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(1));
            Assert.AreEqual(CalendarTimeScale.ThirtyMinutes, Cal.TimeScale);

            // Both in the 10:00-10:30 unit.
            CalendarItem a = At(0, 600, 610, "10:00-10:10");
            CalendarItem b = At(0, 615, 625, "10:15-10:25");
            // Both reach into the 10:30-11:00 unit.
            CalendarItem c = At(1, 600, 645, "10:00-10:45");
            CalendarItem d = At(1, 650, 680, "10:50-11:20");
            Cal.Items.AddRange(new[] { a, b, c, d });

            Assert.IsFalse(a.Bounds.IntersectsWith(b.Bounds), $"{a.Bounds} and {b.Bounds} overlap.");
            Assert.IsFalse(c.Bounds.IntersectsWith(d.Bounds), $"{c.Bounds} and {d.Bounds} overlap.");
        }

        [TestMethod]
        public void RandomItemsWithMinutePrecision_NeverOverlapOnScreen()
        {
            var random = new Random(45678);

            for (int trial = 0; trial < 300; trial++)
            {
                Form form;
                TestCalendar cal = CreateCalendar(out form);
                try
                {
                    cal.SetViewRange(Monday, Monday);
                    var items = new List<CalendarItem>();

                    for (int k = random.Next(2, 8); k > 0; k--)
                    {
                        int start = 8 * 60 + 5 * random.Next(0, 72);   // 8:00-14:00 in 5 minute steps
                        int end = start + 5 * random.Next(1, 24);       // 5 minutes to 2 hours
                        var item = new CalendarItem(cal, Monday.AddMinutes(start), Monday.AddMinutes(end), $"{start}-{end}");
                        cal.Items.Add(item);
                        items.Add(item);
                    }

                    for (int i = 0; i < items.Count; i++)
                    {
                        Assert.IsFalse(items[i].Bounds.IsEmpty, $"Trial {trial}: item {items[i].Text} was not placed.");

                        for (int j = i + 1; j < items.Count; j++)
                            Assert.IsFalse(items[i].Bounds.IntersectsWith(items[j].Bounds),
                                $"Trial {trial}: items {items[i].Text} and {items[j].Text} overlap on screen.");
                    }
                }
                finally
                {
                    form.Dispose();
                }
            }
        }

        [TestMethod]
        public void AllDayItemOutsideView_WeekView_IsNotDrawn()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(2));
            CalendarItem outside = AllDay(10, 10, "outside");

            Cal.Items.Add(outside);

            Assert.IsTrue(outside.Bounds.IsEmpty, $"Item outside the view was placed at {outside.Bounds}.");
        }

        [TestMethod]
        public void ItemOutsideView_MonthView_IsNotDrawnAndDoesNotThrow()
        {
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            CalendarItem inside = AllDay(2, 3, "inside");
            CalendarItem outside = new CalendarItem(Cal, Monday.AddDays(40), Monday.AddDays(40).AddHours(1), "outside");

            Cal.Items.Add(inside);
            Cal.Items.Add(outside); // used to throw NullReferenceException

            Assert.IsFalse(inside.Bounds.IsEmpty);
            Assert.IsTrue(outside.Bounds.IsEmpty, $"Item outside the view was placed at {outside.Bounds}.");
        }

        [TestMethod]
        [DataRow(false, DisplayName = "No other items")]
        [DataRow(true, DisplayName = "A timed item stays")]
        public void DayTop_ShrinksBackAfterLastAllDayItemIsRemoved(bool keepTimedItem)
        {
            Cal.SetViewRange(Monday, Monday.AddDays(2));
            int minimum = Cal.Renderer.DayTopHeight;
            Assert.AreEqual(Cal.Renderer.DayTopMinHeight, minimum);

            if (keepTimedItem)
                Cal.Items.Add(Timed(10, 11, "timed"));

            CalendarItem allDay = AllDay(0, 1, "all day");
            Cal.Items.Add(allDay);
            Assert.IsTrue(Cal.Renderer.DayTopHeight > minimum, "Test setup: the day top should grow.");

            Cal.Items.Remove(allDay);

            Assert.AreEqual(minimum, Cal.Renderer.DayTopHeight);
        }

        private RecordingRenderer DrawMultiWeekItem(CalendarItem item, out List<Rectangle> parts)
        {
            var renderer = new RecordingRenderer(Cal);
            Cal.Renderer = renderer;
            Cal.SetViewRange(Monday, Monday.AddDays(27));
            Cal.Items.Add(item);

            parts = new List<Rectangle>(item.GetAllBounds());
            Assert.AreEqual(3, parts.Count, "Test setup: the item should have one rectangle per week.");

            renderer.TextBounds.Clear();
            renderer.PatternCalls = 0;
            using (var bitmap = new Bitmap(Cal.Width, Cal.Height))
                Cal.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

            return renderer;
        }

        [TestMethod]
        public void MultiWeekItem_TextAndPatternAreDrawnOncePerPart()
        {
            CalendarItem item = AllDay(3, 17, "three weeks");
            item.PatternColor = Color.Red;

            List<Rectangle> parts;
            RecordingRenderer renderer = DrawMultiWeekItem(item, out parts);

            Assert.AreEqual(parts.Count, renderer.TextBounds.Count, "Text draws");
            Assert.AreEqual(parts.Count, renderer.PatternCalls, "Pattern draws");
        }

        [TestMethod]
        public void MultiWeekItemWithImage_TextIsDrawnInsideEachPart()
        {
            CalendarItem item = AllDay(3, 17, "three weeks");
            item.Image = new Bitmap(8, 8);

            List<Rectangle> parts;
            RecordingRenderer renderer = DrawMultiWeekItem(item, out parts);

            Assert.AreEqual(parts.Count, renderer.TextBounds.Count, "Text draws");
            foreach (Rectangle part in parts)
            {
                Assert.IsTrue(renderer.TextBounds.Exists(t => part.Contains(t)),
                    $"No text was drawn inside the part {part}; text drawn at: {string.Join(", ", renderer.TextBounds)}.");
            }
        }
    }
}
