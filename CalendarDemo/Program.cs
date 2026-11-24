using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

namespace CalendarDemo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Keep the .NET Framework default font, which the form layout was designed with
            // (.NET Core 3.0+ defaults to Segoe UI 9pt).
            Application.SetDefaultFont(new Font(new FontFamily("Microsoft Sans Serif"), 8.25f));
            Application.Run(new DemoForm());
        }
    }
}