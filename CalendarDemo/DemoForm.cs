using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Calendar;
using System.Xml.Serialization;
using System.IO;

namespace CalendarDemo
{
    public partial class DemoForm : Form
    {
        List<CalendarItem> _items = new List<CalendarItem>();

        public DemoForm()
        {
            InitializeComponent();

            //Monthview colors
            monthView1.MonthTitleColor = monthView1.MonthTitleColorInactive = CalendarColorTable.FromHex("#C2DAFC");
            monthView1.ArrowsColor = CalendarColorTable.FromHex("#77A1D3");
            monthView1.DaySelectedBackgroundColor = CalendarColorTable.FromHex("#F4CC52");
            monthView1.DaySelectedTextColor = monthView1.ForeColor;

        }

        public FileInfo ItemsFile
        {
            get 
            {
                return new FileInfo(Path.Combine(Application.StartupPath, "items.xml"));
            }
        }

        /// <summary>
        /// Holidays shown by both controls, read from holidays.ics next to the program (if it exists).
        /// </summary>
        public FileInfo HolidaysFile
        {
            get { return new FileInfo(Path.Combine(Application.StartupPath, "holidays.ics")); }
        }

        private void LoadHolidays()
        {
            if (!HolidaysFile.Exists) return;

            try
            {
                CalendarHolidays holidays = CalendarHolidays.LoadICalendar(HolidaysFile.FullName);
                calendar1.Holidays = holidays;
                monthView1.Holidays = holidays;
            }
            catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this,
                    "The holidays could not be loaded from " + HolidaysFile.FullName + ":\n" + ex.Message,
                    "Calendar Demo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadHolidays();

            if (ItemsFile.Exists)
            {
                List<ItemInfo> lst;

                try
                {
                    XmlSerializer xml = new XmlSerializer(typeof(List<ItemInfo>));

                    using (Stream s = ItemsFile.OpenRead())
                    {
                        lst = xml.Deserialize(s) as List<ItemInfo> ?? new List<ItemInfo>();
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    // XmlSerializer reports a damaged file as InvalidOperationException. Keep a copy,
                    // because saving on close will replace items.xml with the items of this session.
                    string backup = ItemsFile.FullName + ".bad";
                    string kept;

                    try
                    {
                        File.Copy(ItemsFile.FullName, backup, true);
                        kept = "A copy was saved as " + backup + ".";
                    }
                    catch (Exception copyEx) when (copyEx is IOException || copyEx is UnauthorizedAccessException)
                    {
                        kept = "A copy could not be saved: " + copyEx.Message;
                    }

                    MessageBox.Show(this,
                        "The items could not be loaded from " + ItemsFile.FullName + ":\n" + ex.Message + "\n\n" + kept,
                        "Calendar Demo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (ItemInfo item in lst)
                {
                    CalendarItem cal = new CalendarItem(calendar1, item.StartTime, item.EndTime, item.Text);

                    // An item without a color is saved as Color.Empty (A = 0). Checking R, G and B
                    // instead would also skip black (A = 255, R = G = B = 0).
                    if (item.A != 0)
                    {
                        cal.ApplyColor(Color.FromArgb(item.A, item.R, item.G, item.B));
                    }

                    // Same rule for the pattern: it is drawn only when its color is not empty.
                    if (item.PatternA != 0)
                    {
                        cal.Pattern = item.Pattern;
                        cal.PatternColor = Color.FromArgb(item.PatternA, item.PatternR, item.PatternG, item.PatternB);
                    }

                    _items.Add(cal);
                }

                PlaceItems();
            }
        }

        private void calendar1_LoadItems(object sender, CalendarLoadEventArgs e)
        {
            PlaceItems();
        }

        private void PlaceItems()
        {
            foreach (CalendarItem item in _items)
            {
                if (calendar1.ViewIntersects(item))
                {
                    calendar1.Items.Add(item);
                }
            }
        }

        private void calendar1_ItemCreated(object sender, CalendarItemCancelEventArgs e)
        {
            _items.Add(e.Item);
        }

        private void calendar1_ItemMouseHover(object sender, CalendarItemEventArgs e)
        {
            Text = e.Item.Text;
        }

        private void calendar1_ItemClick(object sender, CalendarItemEventArgs e)
        {
            //MessageBox.Show(e.Item.Text);
        }

        private void hourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calendar1.TimeScale = CalendarTimeScale.SixtyMinutes;
        }

        private void minutesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calendar1.TimeScale = CalendarTimeScale.ThirtyMinutes;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            calendar1.TimeScale = CalendarTimeScale.FifteenMinutes;
        }

        private void minutesToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            calendar1.TimeScale = CalendarTimeScale.SixMinutes;
        }

        private void minutesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calendar1.TimeScale = CalendarTimeScale.TenMinutes;
        }

        private void minutesToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            calendar1.TimeScale = CalendarTimeScale.FiveMinutes;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            // Right-clicking an item selects it, so the item actions below work on the
            // selected items. Without a selection they would do nothing, so disable them.
            bool hasSelection = false;

            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                hasSelection = true;
                break;
            }

            ToolStripItem[] itemActions =
            {
                redTagToolStripMenuItem, yellowTagToolStripMenuItem, greenTagToolStripMenuItem,
                blueTagToolStripMenuItem, otherColorTagToolStripMenuItem, patternToolStripMenuItem,
                selectImageToolStripMenuItem, imageAlignmentToolStripMenuItem, editItemToolStripMenuItem
            };

            foreach (ToolStripItem action in itemActions)
            {
                action.Enabled = hasSelection;
            }
        }

        private void redTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ApplyColor(Color.Red);
                calendar1.Invalidate(item);
            }
        }

        private void yellowTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ApplyColor(Color.Gold);
                calendar1.Invalidate(item);
            }
        }

        private void greenTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ApplyColor(Color.Green);
                calendar1.Invalidate(item);
            }
        }

        private void blueTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ApplyColor(Color.DarkBlue);
                calendar1.Invalidate(item);
            }
        }

        private void editItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calendar1.ActivateEditMode();
        }

        private void DemoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            List<ItemInfo> lst = new List<ItemInfo>();
            
            foreach (CalendarItem item in _items)
            {
                lst.Add(new ItemInfo(item.StartDate, item.EndDate, item.Text, item.BackgroundColor, item.Pattern, item.PatternColor));
            }

            XmlSerializer xmls = new XmlSerializer(lst.GetType());

            // Zapis do pliku tymczasowego, a potem podmiana: jeśli zapis się nie uda,
            // dotychczasowy items.xml zostaje nienaruszony.
            string target = ItemsFile.FullName;
            string temp = target + ".tmp";

            try
            {
                using (Stream s = File.Create(temp))
                {
                    xmls.Serialize(s, lst);
                }

                if (File.Exists(target))
                {
                    File.Replace(temp, target, null);
                }
                else
                {
                    File.Move(temp, target);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // E.g. no write access to the program folder. The previous items.xml is unchanged.
                DialogResult answer = MessageBox.Show(this,
                    "The items could not be saved to " + target + ":\n" + ex.Message + "\n\nClose without saving?",
                    "Calendar Demo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (answer == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }

        private void otherColorTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    foreach (CalendarItem item in calendar1.GetSelectedItems())
                    {
                        item.ApplyColor(dlg.Color);
                        calendar1.Invalidate(item);
                    }
                }
            }
        }

        private void calendar1_ItemDoubleClick(object sender, CalendarItemEventArgs e)
        {
            MessageBox.Show("Double click: " + e.Item.Text);
        }

        private void calendar1_ItemDeleted(object sender, CalendarItemEventArgs e)
        {
            _items.Remove(e.Item);
        }

        private void calendar1_DayHeaderClick(object sender, CalendarDayEventArgs e)
        {
            calendar1.SetViewRange(e.CalendarDay.Date, e.CalendarDay.Date);
        }

        private void diagonalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.Pattern = System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal;
                item.PatternColor = Color.Red;
                calendar1.Invalidate(item);
            }
        }

        private void verticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.Pattern = System.Drawing.Drawing2D.HatchStyle.Vertical;
                item.PatternColor = Color.Red;
                calendar1.Invalidate(item);
            }
        }

        private void horizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.Pattern = System.Drawing.Drawing2D.HatchStyle.Horizontal;
                item.PatternColor = Color.Red;
                calendar1.Invalidate(item);
            }
        }

        private void hatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.Pattern = System.Drawing.Drawing2D.HatchStyle.DiagonalCross;
                item.PatternColor = Color.Red;
                calendar1.Invalidate(item);
            }
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                // The pattern is drawn only when PatternColor is set, so clearing the color
                // removes it; the hatch style no longer matters.
                item.PatternColor = Color.Empty;
                calendar1.Invalidate(item);
            }
        }

        private void monthView1_SelectionChanged(object sender, EventArgs e)
        {
            calendar1.SetViewRange(monthView1.SelectionStart, monthView1.SelectionEnd);
        }

        private void northToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ImageAlign = CalendarItemImageAlign.North;
                calendar1.Invalidate(item);
            }
        }

        private void eastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ImageAlign = CalendarItemImageAlign.East;
                calendar1.Invalidate(item);
            }
        }

        private void southToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ImageAlign = CalendarItemImageAlign.South;
                calendar1.Invalidate(item);
            }
        }

        private void westToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CalendarItem item in calendar1.GetSelectedItems())
            {
                item.ImageAlign = CalendarItemImageAlign.West;
                calendar1.Invalidate(item);
            }
        }

        private void selectImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "*.gif|*.gif|*.png|*.png|*.jpg|*.jpg";

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Image.FromFile keeps the file locked while the image exists. Loading from
                    // an in-memory copy leaves the file free (the stream must stay open for the
                    // image's lifetime, which is fine for a MemoryStream).
                    Image img = Image.FromStream(new MemoryStream(File.ReadAllBytes(dlg.FileName)));

                    foreach (CalendarItem item in calendar1.GetSelectedItems())
                    {
                        item.Image = img;
                        calendar1.Invalidate(item);
                    }
                }
            }

            
        }
    }
}