using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CalendarDemo
{
    public class ItemInfo
    {
        public DateTime StartTime;
        public DateTime EndTime;
        public string Text;
        public int A;
        public int R;
        public int G;
        public int B;

        // Pattern fields are stored after the color ones, so files saved before they
        // existed still load: missing values default to 0, i.e. no pattern (PatternA = 0).
        public HatchStyle Pattern;
        public int PatternA;
        public int PatternR;
        public int PatternG;
        public int PatternB;

        public ItemInfo()
        { }

        public ItemInfo(DateTime startTime, DateTime endTime, string text, Color color, HatchStyle pattern, Color patternColor)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
            Pattern = pattern;
            PatternA = patternColor.A;
            PatternR = patternColor.R;
            PatternG = patternColor.G;
            PatternB = patternColor.B;
        }
    }
}
