using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms.Calendar
{
    /// <summary>
    /// Hosts a calendar view where user can manage calendar items.
    /// </summary>
    [DefaultEvent("LoadItems")]
    public class Calendar
        : ScrollableControl
    {
        private long draggingOffsetInMinutes = 0;

        #region Subclasses

        /// <summary>
        /// Possible states of the calendar
        /// </summary>
        public enum CalendarState
        {
            /// <summary>
            /// Nothing happening
            /// </summary>
            Idle,

            /// <summary>
            /// User is currently dragging on view to select a time range
            /// </summary>
            DraggingTimeSelection,

            /// <summary>
            /// User is currently dragging an item among the view
            /// </summary>
            DraggingItem,

            /// <summary>
            /// User is editing an item's Text
            /// </summary>
            EditingItemText,

            /// <summary>
            /// User is currently resizing an item
            /// </summary>
            ResizingItem
        }

        #endregion

        #region Static

        /// <summary>
        /// Returns a value indicating if two date ranges intersect
        /// </summary>
        /// <param name="startA"></param>
        /// <param name="endA"></param>
        /// <param name="startB"></param>
        /// <param name="endB"></param>
        /// <returns></returns>
        public static bool DateIntersects(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        {
            return startB < endA && startA < endB;
        }

        #endregion

        #region Events

        /// <summary>
        /// Delegate that supports <see cref="LoadItems"/> event
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event Data</param>
        public delegate void CalendarLoadEventHandler(object sender, CalendarLoadEventArgs e);

        /// <summary>
        /// Delegate that supports item-related events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CalendarItemEventHandler(object sender, CalendarItemEventArgs e);

        /// <summary>
        /// Delegate that supports cancelable item-related events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CalendarItemCancelEventHandler(object sender, CalendarItemCancelEventArgs e);

        /// <summary>
        /// Delegate that supports <see cref="CalendarDay"/>-related events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CalendarDayEventHandler(object sender, CalendarDayEventArgs e);

        /// <summary>
        /// Occurs when items are load into view
        /// </summary>
        [Description("Occurs when items are load into view")]
        public event CalendarLoadEventHandler LoadItems;

        /// <summary>
        /// Occurs when a day header is clicked
        /// </summary>
        [Description("Occurs when a day header is clicked")]
        public event CalendarDayEventHandler DayHeaderClick;

        /// <summary>
        /// Occurs when an item is about to be created.
        /// </summary>
        /// <remarks>
        /// Event can be cancelled
        /// </remarks>
        [Description("Occurs when an item is about to be created.")]
        public event CalendarItemCancelEventHandler ItemCreating;

        /// <summary>
        /// Occurs when an item has been created.
        /// </summary>
        [Description("Occurs when an item has been created.")]
        public event CalendarItemCancelEventHandler ItemCreated;

        /// <summary>
        /// Occurs before an item is deleted
        /// </summary>
        [Description("Occurs before an item is deleted")]
        public event CalendarItemCancelEventHandler ItemDeleting;

        /// <summary>
        /// Occurs when an item has been deleted
        /// </summary>
        [Description("Occurs when an item has been deleted")]
        public event CalendarItemEventHandler ItemDeleted;

        /// <summary>
        /// Occurs when an item text is about to be edited
        /// </summary>
        [Description("Occurs when an item text is about to be edited")]
        public event CalendarItemCancelEventHandler ItemTextEditing;

        /// <summary>
        /// Occurs when an item text is edited
        /// </summary>
        [Description("Occurs when an item text is edited")]
        public event CalendarItemCancelEventHandler ItemTextEdited;

        /// <summary>
        /// Occurs when an item time range has changed
        /// </summary>
        [Description("Occurs when an item time range has changed")]
        public event CalendarItemEventHandler ItemDatesChanged;

        /// <summary>
        /// Occurs when an item is clicked
        /// </summary>
        [Description("Occurs when an item is clicked")]
        public event CalendarItemEventHandler ItemClick;

        /// <summary>
        /// Occurs when an item is double-clicked
        /// </summary>
        [Description("Occurs when an item is double-clicked")]
        public event CalendarItemEventHandler ItemDoubleClick;

        /// <summary>
        /// Occurs when an item is selected
        /// </summary>
        [Description("Occurs when an item is selected")]
        public event CalendarItemEventHandler ItemSelected;

        /// <summary>
        /// Occurs after the items are positioned
        /// </summary>
        /// <remarks>
        /// Items bounds can be altered using the <see cref="CalendarItem.SetBounds"/> method.
        /// </remarks>
        [Description("Occurs after the items are positioned")]
        public event EventHandler ItemsPositioned;

        /// <summary>
        /// Occurs when the mouse is moved over an item
        /// </summary>
        [Description("Occurs when the mouse is moved over an item")]
        public event CalendarItemEventHandler ItemMouseHover;

        #endregion

        #region Fields
        private CalendarTextBox _textBox;
        private bool _allowNew;
        private bool _allowItemEdit;
        private bool _allowItemResize;
        private bool _creatingItem;
        private CalendarDay[] _days;
        private CalendarDaysMode _daysMode;
        private CalendarItem _editModeItem;
        private bool _finalizingEdition;
        private DayOfWeek _firstDayOfWeek;
        private CalendarHighlightRange[] _highlightRanges;
        private CalendarItemCollection _items;
        private string _itemsDateFormat;
        private string _itemsTimeFormat;
        private string _weekNumberFormat;
        private bool _itemsLayoutPending;
        private CalendarHolidays _holidays;
        private int _maximumFullDays;
        private int _maximumViewDays;
        private CalendarRenderer _renderer;
        private CalendarState _state;
        private CalendarTimeScale _timeScale;
        private int _timeUnitsOffset;
        private DateTime _viewEnd;
        private DateTime _viewStart;
        private CalendarWeek[] _weeks;
        private List<CalendarSelectableElement> _selectedElements;
        private ICalendarSelectableElement _selectedElementEnd;
        private ICalendarSelectableElement _selectedElementStart;
        private Rectangle _selectedElementSquare;
        private CalendarItem itemOnState;
        private bool itemOnStateChanged;
        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new <see cref="Calendar"/> control
        /// </summary>
        public Calendar()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);

            DoubleBuffered = true;
            
            _selectedElements = new List<CalendarSelectableElement>();
            _items = new CalendarItemCollection(this);
            _renderer = new CalendarProfessionalRenderer(this);
            _maximumFullDays = 8;
            _maximumViewDays = 35;
            
            HighlightRanges = new CalendarHighlightRange[] { 
                new CalendarHighlightRange( DayOfWeek.Monday, new TimeSpan(8,0,0), new TimeSpan(17,0,0)),
                new CalendarHighlightRange( DayOfWeek.Tuesday, new TimeSpan(8,0,0), new TimeSpan(17,0,0)),
                new CalendarHighlightRange( DayOfWeek.Wednesday, new TimeSpan(8,0,0), new TimeSpan(17,0,0)),
                new CalendarHighlightRange( DayOfWeek.Thursday, new TimeSpan(8,0,0), new TimeSpan(17,0,0)),
                new CalendarHighlightRange( DayOfWeek.Friday, new TimeSpan(8,0,0), new TimeSpan(17,0,0)),
            };

            _timeScale = CalendarTimeScale.ThirtyMinutes;
            SetViewRange(DateTime.Now, DateTime.Now.AddDays(2));
            

            _itemsDateFormat = "dd/MMM";
            _allowItemEdit = true;
            _allowNew = true;
            _allowItemResize = true;
        }


        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating if the control let's the user create new items.
        /// </summary>
        [DefaultValue(true)]
        [Description("Allows the user to create new items on the view")]
        public bool AllowNew
        {
            get { return _allowNew; }
            set { _allowNew = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating if the user can edit the item using the mouse or keyboard
        /// </summary>
        [DefaultValue(true)]
        [Description("Allows or denies the user the edition of items text or date ranges.")]
        public bool AllowItemEdit
        {
            get { return _allowItemEdit; }
            set { _allowItemEdit = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating if calendar allows user to resize the calendar.
        /// </summary>
        [DefaultValue(true)]
        [Description("Allows or denies the user to resize items on the calendar")]
        public bool AllowItemResize
        {
            get { return _allowItemResize; }
            set { _allowItemResize = value; }
        }

        /// <summary>
        /// Gets the days visible on the ccurrent view
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CalendarDay[] Days
        {
            get { return _days; }
        }

        /// <summary>
        /// Gets the mode in which days are drawn.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CalendarDaysMode DaysMode
        {
            get { return _daysMode; }
        }

        /// <summary>
        /// Gets the union of day body rectangles
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle DaysBodyRectangle
        {
            get
            {
                Rectangle first = Days[0].BodyBounds;
                Rectangle last = Days[Days.Length - 1].BodyBounds;

                return Rectangle.Union(first, last);
            }
        }

        /// <summary>
        /// Gets if the calendar is currently in edit mode of some item
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EditMode
        {
            get { return TextBox != null; }
        }

        /// <summary>
        /// Gets the item being edited (if any)
        /// </summary>
        public CalendarItem EditModeItem
        {
            get 
            {
                return _editModeItem;
            }
        }

        /// <summary>
        /// Gets or sets the first day of weeks
        /// </summary>
        //[Description("Starting day of weeks")]
        //[DefaultValue(DayOfWeek.Sunday)]
        //public DayOfWeek FirstDayOfWeek
        //{
        //    set { _firstDayOfWeek = value; }
        //    get { return _firstDayOfWeek; }
        //}
        [Description("Starting day of weeks")]
        [DefaultValue(DayOfWeek.Sunday)]
        public DayOfWeek FirstDayOfWeek
        {
            get { return _firstDayOfWeek; }
            set
            {
                if (_firstDayOfWeek != value)
                {
                    _firstDayOfWeek = value;

                    this.UpdateDaysAndWeeks();
                    Renderer.PerformLayout();
                    base.Invalidate();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the time ranges that should be highlighted as work-time.
        /// This ranges are week based.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public CalendarHighlightRange[] HighlightRanges
        {
            get { return _highlightRanges; }
            set
            {
                if (_highlightRanges != null)
                {
                    foreach (CalendarHighlightRange range in _highlightRanges)
                    {
                        if (range != null && range.Calendar == this)
                            range.SetCalendar(null);
                    }
                }

                // null means no highlighted time.
                _highlightRanges = value ?? new CalendarHighlightRange[0];

                // Lets the ranges update the highlights when their day or times are changed later.
                foreach (CalendarHighlightRange range in _highlightRanges)
                {
                    if (range != null)
                        range.SetCalendar(this);
                }

                UpdateHighlights();
                Invalidate();
            }
        }

        /// <summary>
        /// Gets the collection of items currently on the view.
        /// </summary>
        /// <remarks>
        /// This collection changes every time the view is changed
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CalendarItemCollection Items
        {
            get { return _items; }
        }

        /// <summary>
        /// Gets or sets the format in which time is shown in the items, when applicable
        /// </summary>
        [DefaultValue("dd/MMM")]
        public string ItemsDateFormat
        {
            get { return _itemsDateFormat; }
            set { _itemsDateFormat = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the format in which time is shown in the items, when applicable.
        /// When empty (the default), the current culture's short time pattern is used,
        /// e.g. "15:00" in pl-PL and "3:00 PM" in en-US.
        /// </summary>
        [DefaultValue(null)]
        public string ItemsTimeFormat
        {
            get { return _itemsTimeFormat; }
            set { _itemsTimeFormat = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the holidays shown with a different background (and their names in the day headers).
        /// Work time (<see cref="HighlightRanges"/>) is not highlighted on holidays. The same set can be
        /// used by several controls, e.g. a <see cref="MonthView"/>; changes to it repaint the calendar.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public CalendarHolidays Holidays
        {
            get { return _holidays; }
            set
            {
                if (_holidays == value) return;

                if (_holidays != null) _holidays.Changed -= Holidays_Changed;
                _holidays = value;
                if (_holidays != null) _holidays.Changed += Holidays_Changed;

                Holidays_Changed(this, EventArgs.Empty);
            }
        }

        private void Holidays_Changed(object sender, EventArgs e)
        {
            UpdateHighlights();
            Invalidate();
        }

        /// <summary>
        /// The format actually used for times on items: <see cref="ItemsTimeFormat"/>, or the
        /// culture's short time pattern ("t") when it is empty.
        /// </summary>
        internal string EffectiveItemsTimeFormat
        {
            get { return string.IsNullOrEmpty(_itemsTimeFormat) ? "t" : _itemsTimeFormat; }
        }

        /// <summary>
        /// Notes that the dates of an item changed, so the items must be laid out again. The layout
        /// is done once, before the next paint or the next time item positions are needed (see
        /// <see cref="EnsureItemsLayout"/>), so changing many items costs one layout, not one per change.
        /// </summary>
        internal void RequestItemsLayout()
        {
            _itemsLayoutPending = true;
            Invalidate();
        }

        /// <summary>
        /// Lays the items out if <see cref="RequestItemsLayout"/> was called since the last layout.
        /// </summary>
        internal void EnsureItemsLayout()
        {
            if (_itemsLayoutPending && Days != null && Renderer != null)
            {
                Renderer.PerformItemsLayout();
            }
        }

        /// <summary>
        /// Called by <see cref="CalendarRenderer.PerformItemsLayout"/>: any layout brings all items up to date.
        /// </summary>
        internal void ItemsLayoutDone()
        {
            _itemsLayoutPending = false;
        }

        /// <summary>
        /// Gets or sets the composite format of the week headers in the month view, where {0} is
        /// the week number, e.g. "Tydz. {0}". When empty, "KW {0}" is used for German and
        /// "Week {0}" for other cultures.
        /// </summary>
        /// <exception cref="ArgumentException">The value is not a valid composite format.</exception>
        [DefaultValue(null)]
        [Description("Format of week headers in the month view; {0} is the week number")]
        public string WeekNumberFormat
        {
            get { return _weekNumberFormat; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Check now; an invalid format would otherwise throw on every repaint.
                    try
                    {
                        string.Format(value, 1);
                    }
                    catch (FormatException ex)
                    {
                        throw new ArgumentException("WeekNumberFormat is not a valid format: " + ex.Message, "value", ex);
                    }
                }

                _weekNumberFormat = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the maximum full days shown on the view. 
        /// After this amount of days, they will be shown as short days.
        /// </summary>
        [DefaultValue(8)]
        public int MaximumFullDays
        {
            get { return _maximumFullDays; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", value, "MaximumFullDays must be at least 1");
                }

                if (_maximumFullDays == value) return;

                _maximumFullDays = value;

                // The value decides between the day/week and the month layout; apply it to the current view.
                if (Days != null)
                {
                    UpdateDaysAndWeeks();
                    Renderer.PerformLayout();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of days supported by the view.
        /// Value must be a positive multiple of 7, not smaller than the current view.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a positive multiple of 7,
        /// or it is smaller than the number of days of the current view.</exception>
        [DefaultValue(35)]
        public int MaximumViewDays
        {
            get { return _maximumViewDays; }
            set
            {
                if (value < 7 || value % 7 != 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "MaximumViewDays must be a positive multiple of 7");
                }

                // A smaller limit would make the current view invalid, and any later change that
                // rebuilds the days (e.g. FirstDayOfWeek) would then throw.
                int viewDays = (ViewEnd.Date - ViewStart.Date).Days + 1;

                if (Days != null && viewDays > value)
                {
                    throw new ArgumentOutOfRangeException("value", value,
                        string.Format("MaximumViewDays can't be smaller than the current view ({0} days); change the view first", viewDays));
                }

                _maximumViewDays = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CalendarRenderer"/> of the <see cref="Calendar"/>
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CalendarRenderer Renderer
        {
            get { return _renderer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "The calendar needs a renderer");
                }

                _renderer = value;

                if (Created)
                {
                    // The renderer needs the calendar here (CalendarProfessionalRenderer sets its font),
                    // the same as in OnCreateControl; it used to get null and throw.
                    value.OnInitialize(new CalendarRendererEventArgs(this, null, Rectangle.Empty));
                }

                // The new renderer has computed no layout yet (e.g. the time scale bounds are empty).
                if (Days != null)
                {
                    value.PerformLayout();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets the last selected element
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICalendarSelectableElement SelectedElementEnd
        {
            get { return _selectedElementEnd; }
            set 
            { 
                _selectedElementEnd = value;

                UpdateSelectionElements();
            }
        }

        /// <summary>
        /// Gets the first selected element
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICalendarSelectableElement SelectedElementStart
        {
            get { return _selectedElementStart; }
            set 
            { 
                _selectedElementStart = value;

                UpdateSelectionElements();
            }
        }

        /// <summary>
        /// Gets or sets the end date-time of the view's selection: the end of the last selected
        /// time unit, or 23:59:59 of the last selected day. <see cref="DateTime.MinValue"/> when nothing is selected.
        /// </summary>
        /// <remarks>
        /// Setting it selects the time unit (or, in the month view, the day) that contains the moment
        /// just before the value, so the value returned by the getter selects the same element again.
        /// When nothing is selected, the element becomes both the start and the end of the selection.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not in the current view.</exception>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SelectionEnd
        {
            get
            {
                ICalendarSelectableElement start, end;
                if (!GetOrderedSelection(out start, out end)) return DateTime.MinValue;

                return end.Date.Add(SelectionLength(end));
            }
            set
            {
                ICalendarSelectableElement element = SelectableElementAt(value.AddTicks(-1), "value");

                if (SelectedElementStart == null)
                    SetSelectionRange(element, element);
                else
                    SelectedElementEnd = element;
            }
        }

        /// <summary>
        /// Gets or sets the start date-time of the view's selection: the start of the first selected
        /// time unit or day. <see cref="DateTime.MinValue"/> when nothing is selected.
        /// </summary>
        /// <remarks>
        /// Setting it selects the time unit (or, in the month view, the day) that contains the value.
        /// When nothing is selected, the element becomes both the start and the end of the selection.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not in the current view.</exception>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SelectionStart
        {
            get
            {
                ICalendarSelectableElement start, end;
                if (!GetOrderedSelection(out start, out end)) return DateTime.MinValue;

                return start.Date;
            }
            set
            {
                ICalendarSelectableElement element = SelectableElementAt(value, "value");

                if (SelectedElementEnd == null)
                    SetSelectionRange(element, element);
                else
                    SetSelectionRange(element, SelectedElementEnd);
            }
        }

        /// <summary>
        /// Gets the selected elements, the earlier one first. <c>false</c> when nothing is selected.
        /// </summary>
        private bool GetOrderedSelection(out ICalendarSelectableElement start, out ICalendarSelectableElement end)
        {
            start = SelectedElementStart;
            end = SelectedElementEnd;

            if (start == null || end == null) return false;

            if (end.CompareTo(start) < 0)
            {
                ICalendarSelectableElement tmp = start;
                start = end;
                end = tmp;
            }

            return true;
        }

        /// <summary>
        /// How much time a selected element covers, the same way <see cref="CreateItemOnSelection"/> counts it.
        /// </summary>
        private static TimeSpan SelectionLength(ICalendarSelectableElement element)
        {
            CalendarTimeScaleUnit unit = element as CalendarTimeScaleUnit;

            return unit != null ? unit.Duration : new TimeSpan(23, 59, 59);
        }

        /// <summary>
        /// The element a selection date refers to: a day in the month view, a time unit otherwise.
        /// </summary>
        private ICalendarSelectableElement SelectableElementAt(DateTime date, string paramName)
        {
            ICalendarSelectableElement element = DaysMode == CalendarDaysMode.Short
                ? (ICalendarSelectableElement)FindDay(date)
                : GetTimeUnit(date);

            if (element == null)
            {
                throw new ArgumentOutOfRangeException(paramName, date, "The date is not in the current view");
            }

            return element;
        }

        /// <summary>
        /// Gets or sets the state of the calendar
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CalendarState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Gets the TextBox of the edit mode
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal CalendarTextBox TextBox
        {
            get { return _textBox; }
            set { _textBox = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="CalendarTimeScale"/> for visualization.
        /// </summary>
        [DefaultValue(CalendarTimeScale.ThirtyMinutes)]
        public CalendarTimeScale TimeScale
        {
            get { return _timeScale; }
            set
            {
                // TimeUnitsOffset counts units of the current scale; remember the time at the top
                // of the view, so the view stays at the same time of day with the new scale.
                int topMinutes = -_timeUnitsOffset * (int)_timeScale;

                // A selection of time units refers to the units being replaced; it would stay
                // selected but no longer be part of the view, so drop it.
                if (_selectedElementStart is CalendarTimeScaleUnit || _selectedElementEnd is CalendarTimeScaleUnit)
                {
                    ClearSelectedComponents();
                    _selectedElementStart = null;
                    _selectedElementEnd = null;
                }

                _timeScale = value;

                if (Days != null)
                {
                    for (int i = 0; i < Days.Length; i++)
                    {
                        Days[i].UpdateUnits();
                    }
                }

                _timeUnitsOffset = -ClampUnitsFromMidnight(topMinutes / (int)value);

                Renderer.PerformLayout();
                Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the offset of scrolled units
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TimeUnitsOffset
        {
            get { return _timeUnitsOffset; }
            set 
            {
                _timeUnitsOffset = value;
                Renderer.PerformLayout();
                Invalidate(); 
            }
        }

        /// <summary>
        /// Gets or sets the end date-time of the current view.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime ViewEnd
        {
            get { return _viewEnd; }
            set
            {
                ValidateViewRange(_viewStart, value, "value");

                _viewEnd = value.Date.Add(new TimeSpan(23, 59, 59));
                ClearItems();
                UpdateDaysAndWeeks();
                Renderer.PerformLayout();
                Invalidate();
                ReloadItems();
            }
        }

        /// <summary>
        /// Gets or sets the start date-time of the current view.
        /// </summary>
        [DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden)]
        public DateTime ViewStart
        {
            get { return _viewStart; }
            set {
                ValidateViewRange(value, _viewEnd, "value");

                _viewStart = value.Date;
                ClearItems();
                UpdateDaysAndWeeks();
                Renderer.PerformLayout();
                Invalidate();
                ReloadItems();
            }
        }

        /// <summary>
        /// Gets the weeks currently visible on the calendar, if <see cref="DaysMode"/> is <see cref="CalendarDaysMode.Short"/>
        /// </summary>
        public CalendarWeek[] Weeks
        {
            get { return _weeks; }
        }


        #endregion

        #region Public Methods
        /// <summary>
        /// Selects item on calendar.
        /// </summary>
        /// <param name="item"></param>
        public void SetItemSelected(CalendarItem item)
        {

            CalendarItem hittedItem = item as CalendarItem;

            if (!Focused)
            {
                Focus();
            }

            if (hittedItem != null)
            {
                ClearSelectedItems();

                hittedItem.SetSelected(true);
                Invalidate(hittedItem);
                OnItemSelected(new CalendarItemEventArgs(hittedItem));

                itemOnState = hittedItem;
                itemOnStateChanged = false;

                SetSelectionRange(null, null);
            }
            else
            {
                ClearSelectedItems();
            }
        }


        /// <summary>
        /// Scrolls the view so the given time of day is at the top, regardless of the current scroll position.
        /// Near the end of the day the view stops at the last time unit, as with manual scrolling.
        /// </summary>
        /// <param name="num">Time of day in hours, e.g. 6.5 for 6:30 AM or 16.75 for 4:45 PM.</param>
        public void ScrollToTime(Single num)
        {
            if (Days == null || Days.Length == 0 || Days[0].TimeUnits == null) return;

            // Same rounding as before: whole time units, counted from midnight.
            int unitsFromMidnight = ClampUnitsFromMidnight((int)(60 / (int)_timeScale * num));

            if (TimeUnitsOffset != -unitsFromMidnight)
            {
                TimeUnitsOffset = -unitsFromMidnight;
            }
        }

        /// <summary>
        /// Limits the number of time units scrolled from midnight the same way as <see cref="ScrollTimeUnits"/>:
        /// not above midnight, and not past the point where the last time unit reaches the bottom of the view.
        /// </summary>
        private int ClampUnitsFromMidnight(int unitsFromMidnight)
        {
            if (Days == null || Days.Length == 0 || Days[0].TimeUnits == null)
                return 0;

            int units = Days[0].TimeUnits.Length;
            int visible = Math.Max(1, Renderer.GetVisibleTimeUnits());
            int maxUnitsFromMidnight = Math.Max(0, units - visible);

            return Math.Max(0, Math.Min(unitsFromMidnight, maxUnitsFromMidnight));
        }
                                    
        /// <summary>
        /// Activates the edit mode on the first selected item
        /// </summary>
        /// <param name="item"></param>
        public void ActivateEditMode()
        {
            foreach (CalendarItem item in Items)
            {
                if (item.Selected)
                {
                    ActivateEditMode(item);
                    return;
                }
            }
        }

        /// <summary>
        /// Activates the edit mode on the specified item
        /// </summary>
        /// <param name="item"></param>
        public void ActivateEditMode(CalendarItem item)
        {
            CalendarItemCancelEventArgs evt = new CalendarItemCancelEventArgs(item);
            
            // Check if Item is locked, do not enter edit mode
            if (item.Locked)
                return;
            if (!_creatingItem)
            {
                OnItemEditing(evt);
            }

            if (evt.Cancel)
            {
                return;
            }

            _editModeItem = item;
            TextBox = new CalendarTextBox(this);
            TextBox.KeyDown += new KeyEventHandler(TextBox_KeyDown);
            TextBox.LostFocus += new EventHandler(TextBox_LostFocus);
            Rectangle r = item.Bounds;
            r.Inflate(-2, -2);
            TextBox.Bounds = r;
            TextBox.BorderStyle = BorderStyle.None;
            TextBox.Text = item.Text;
            TextBox.Multiline = true;

            Controls.Add(TextBox);
            TextBox.Visible = true;
            TextBox.Focus();
            TextBox.SelectionStart = TextBox.Text.Length;

            SetState(CalendarState.EditingItemText);
        }

        /// <summary>
        /// Creates a new item on the current selection. 
        /// If there's no selection, this will be ignored.
        /// </summary>
        /// <param name="itemText">Text of the item</param>
        /// <param name="editMode">If <c>true</c> activates the edit mode so user can edit the text of the item.</param>
        public void CreateItemOnSelection(string itemText, bool editMode)
        {
            if (SelectedElementEnd == null || SelectedElementStart == null) return;

            CalendarTimeScaleUnit unitEnd = SelectedElementEnd as CalendarTimeScaleUnit;
            CalendarDayTop dayTop = SelectedElementEnd as CalendarDayTop;
            CalendarDay day = SelectedElementEnd as CalendarDay;
            TimeSpan duration = unitEnd != null ? unitEnd.Duration : new TimeSpan(23, 59, 59);
            CalendarItem item = new CalendarItem(this);

            DateTime dstart = SelectedElementStart.Date;
            DateTime dend = SelectedElementEnd.Date;

            if (dend.CompareTo(dstart) < 0)
            {
                DateTime dtmp = dend;
                dend = dstart;
                dstart = dtmp;
            }

            item.StartDate = dstart;
            item.EndDate = dend.Add(duration);
            item.Text = itemText;

            CalendarItemCancelEventArgs evtA = new CalendarItemCancelEventArgs(item);

            OnItemCreating(evtA);

            if (!evtA.Cancel)
            {
                Items.Add(item);

                if (editMode)
                {
                    _creatingItem = true;
                    ActivateEditMode(item);
                }
            }

            
        }

        /// <summary>
        /// Ensures the scrolling shows the specified time unit. It doesn't affect View date ranges.
        /// </summary>
        /// <param name="unit">Unit to ensure visibility</param>
        public void EnsureVisible(CalendarTimeScaleUnit unit)
        {
            if (Days == null || Days.Length == 0) return;

            Rectangle view = Days[0].BodyBounds;

            if (unit.Bounds.Bottom > view.Bottom)
            {
                // Scroll so the unit is the last fully visible one: with V visible units the view
                // then starts at unit (index - V + 1).
                int index = Convert.ToInt32(Math.Ceiling(unit.Date.TimeOfDay.TotalMinutes / (double)TimeScale));
                TimeUnitsOffset = -ClampUnitsFromMidnight(index - Renderer.GetVisibleTimeUnits() + 1);
            }
            else if (unit.Bounds.Top < view.Top)
            {
                TimeUnitsOffset = -Convert.ToInt32(Math.Ceiling(unit.Date.TimeOfDay.TotalMinutes / (double)TimeScale));
            }
        }

        /// <summary>
        /// Finalizes editing the <see cref="EditModeItem"/>.
        /// </summary>
        /// <param name="cancel">Value indicating if edition of item should be canceled.</param>
        public void FinalizeEditMode(bool cancel)
        {
            if (!EditMode || EditModeItem == null || _finalizingEdition) return;

            _finalizingEdition = true;

            string cancelText = _editModeItem.Text;
            CalendarItem itemBuffer = _editModeItem;
            _editModeItem = null;
            CalendarItemCancelEventArgs evt = new CalendarItemCancelEventArgs(itemBuffer);

            if(!cancel)
                itemBuffer.Text = TextBox.Text.Trim();

            if (TextBox != null)
            {
                TextBox.Visible = false;
                Controls.Remove(TextBox);
                TextBox.Dispose();
            }

            _textBox = null;

            if (_creatingItem)
            {
                OnItemCreated(evt);
            }
            else
            {
                OnItemEdited(evt);
            }

            if (evt.Cancel)
            {
                itemBuffer.Text = cancelText;
            }

            // Repaint the whole item with its final text (the new one, or the old one if the edit
            // was cancelled). Previously this was guarded by _editModeItem != null, which is always
            // false here because _editModeItem is cleared above, so it never ran.
            Invalidate(itemBuffer);

            _creatingItem = false;
            _finalizingEdition = false;

            if (State == CalendarState.EditingItemText)
            {
                SetState(CalendarState.Idle);
            }
        }

        /// <summary>
        /// Closes the edit box without applying the text and without raising
        /// <see cref="ItemTextEdited"/> or <see cref="ItemCreated"/>. Used when the edited item
        /// is removed from <see cref="Items"/> (also when the view changes), so there is nothing
        /// left to edit.
        /// </summary>
        internal void AbortEditMode()
        {
            if (!EditMode || _finalizingEdition) return;

            // Cleared first, so FinalizeEditMode (called by the text box losing focus while it is
            // removed) has nothing to finalize.
            _editModeItem = null;

            CalendarTextBox textBox = TextBox;
            _textBox = null;
            textBox.Visible = false;
            Controls.Remove(textBox);
            textBox.Dispose();

            _creatingItem = false;

            if (State == CalendarState.EditingItemText)
            {
                SetState(CalendarState.Idle);
            }
        }

        /// <summary>
        /// Finds the <see cref="CalendarDay"/> for the specified date, if in the view.
        /// </summary>
        /// <param name="d">Date to find day</param>
        /// <returns><see cref="CalendarDay"/> object that matches the date, <c>null</c> if day was not found.</returns>
        public CalendarDay FindDay(DateTime d)
        {
            if (Days == null) return null;

            for (int i = 0; i < Days.Length; i++)
            {
                if (Days[i].Date.Date.Equals(d.Date.Date))
                {
                    return Days[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the items that are currently selected
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CalendarItem> GetSelectedItems()
        {
            List<CalendarItem> items = new List<CalendarItem>();

            foreach (CalendarItem item in Items)
            {
                if (item.Selected)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>
        /// Gets the time unit that starts with the specified date
        /// </summary>
        /// <param name="dateTime">Date and time of the unit you want to extract</param>
        /// <returns>Matching time unit. <c>null</c> If out of range.</returns>
        public CalendarTimeScaleUnit GetTimeUnit(DateTime d)
        {
            if (Days != null)
            {
                foreach (CalendarDay day in Days)
                {
                    if (day.Date.Equals(d.Date))
                    {
                        double duration = Convert.ToDouble((int)TimeScale);
                        int index = 
                            Convert.ToInt32(
                                Math.Floor(
                                    d.TimeOfDay.TotalMinutes / duration
                                )
                            );

                        return day.TimeUnits[index];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for the first hitted <see cref="ICalendarSelectableElement"/>
        /// </summary>
        /// <param name="p">Point to check for hit test</param>
        /// <returns></returns>
        public ICalendarSelectableElement HitTest(Point p)
        {
            return HitTest(p, false);
        }

        /// <summary>
        /// Searches for the first hitted <see cref="ICalendarSelectableElement"/>
        /// </summary>
        /// <param name="p">Point to check for hit test</param>
        /// <returns></returns>
        public ICalendarSelectableElement HitTest(Point p, bool ignoreItems)
        {
            // Positions must be current, also the day top height used below.
            EnsureItemsLayout();

            if(!ignoreItems)
                foreach (CalendarItem item in Items)
                {
                    foreach (Rectangle r in item.GetAllBounds())
                    {
                        if (r.Contains(p))
                        {
                            return item;
                        } 
                    }
                }

            for (int i = 0; i < Days.Length; i++)
            {
                if (Days[i].Bounds.Contains(p))
                {
                    if (DaysMode == CalendarDaysMode.Expanded)
                    {
                        if (Days[i].DayTop.Bounds.Contains(p))
                        {
                            return Days[i].DayTop;
                        }
                        else
                        {
                            for (int j = 0; j < Days[i].TimeUnits.Length; j++)
                            {
                                if (Days[i].TimeUnits[j].Visible &&
                                    Days[i].TimeUnits[j].Bounds.Contains(p))
                                {
                                    return Days[i].TimeUnits[j];
                                }
                            }
                        }

                        return Days[i];
                    }
                    else if (DaysMode == CalendarDaysMode.Short)
                    {
                        return Days[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the item hitted at the specified location. Null if no item hitted.
        /// </summary>
        /// <param name="p">Location to serach for items</param>
        /// <returns>Hitted item at the location. Null if no item hitted.</returns>
        public CalendarItem ItemAt(Point p)
        {
            return HitTest(p) as CalendarItem;
        }

        /// <summary>
        /// Invalidates the bounds of the specified day
        /// </summary>
        /// <param name="day"></param>
        public void Invalidate(CalendarDay day)
        {
            Invalidate(day.Bounds);
        }

        /// <summary>
        /// Ivalidates the bounds of the specified unit
        /// </summary>
        /// <param name="unit"></param>
        public void Invalidate(CalendarTimeScaleUnit unit)
        {
            Invalidate(unit.Bounds);
        }

        /// <summary>
        /// Invalidates the area of the specified item
        /// </summary>
        /// <param name="item"></param>
        public void Invalidate(CalendarItem item)
        {
            Rectangle r = item.Bounds;

            foreach (Rectangle bounds in item.GetAllBounds())
            {
                r = Rectangle.Union(r, bounds);
            }

            r.Inflate(Renderer.ItemShadowPadding + Renderer.ItemInvalidateMargin, Renderer.ItemShadowPadding + Renderer.ItemInvalidateMargin);
            Invalidate(r);
        }

        /// <summary>
        /// Establishes the selection range with only one graphical update.
        /// </summary>
        /// <param name="selectionStart">Fisrt selected element</param>
        /// <param name="selectionEnd">Last selection element</param>
        public void SetSelectionRange(ICalendarSelectableElement selectionStart, ICalendarSelectableElement selectionEnd)
        {
            _selectedElementStart = selectionStart;
            SelectedElementEnd = selectionEnd;
        }

        /// <summary>
        /// Sets the value of <see cref="ViewStart"/> and <see cref="ViewEnd"/> properties
        /// triggering only one repaint process
        /// </summary>
        /// <param name="dateStart">Start date of view</param>
        /// <param name="dateEnd">End date of view</param>
        /// <exception cref="ArgumentOutOfRangeException">The range is shorter than 1 day or longer than <see cref="MaximumViewDays"/>; the current view is left unchanged.</exception>
        public void SetViewRange(DateTime dateStart, DateTime dateEnd)
        {
            // Validate before touching _viewStart, so an invalid range leaves the current view as it was.
            ValidateViewRange(dateStart, dateEnd, "dateEnd");

            _viewStart = dateStart.Date;
            ViewEnd = dateEnd;
        }

        /// <summary>
        /// Throws if the view range from <paramref name="start"/> to <paramref name="end"/> (whole days)
        /// is shorter than 1 day or longer than <see cref="MaximumViewDays"/>.
        /// Called before the range is stored, so an invalid value never reaches the calendar's state.
        /// </summary>
        private void ValidateViewRange(DateTime start, DateTime end, string paramName)
        {
            int days = (end.Date - start.Date).Days + 1;

            if (days < 1 || days > MaximumViewDays)
            {
                throw new ArgumentOutOfRangeException(paramName, days,
                    string.Format("Days between ViewStart ({0:d}) and ViewEnd ({1:d}) should be between 1 and MaximumViewDays ({2})",
                        start, end, MaximumViewDays));
            }
        }

        /// <summary>
        /// Returns a value indicating if the view range intersects the specified date range.
        /// </summary>
        /// <param name="dateStart"></param>
        /// <param name="dateEnd"></param>
        public bool ViewIntersects(DateTime dateStart, DateTime dateEnd)
        {
            return DateIntersects(ViewStart, ViewEnd, dateStart, dateEnd);
            
        }

        /// <summary>
        /// Returns a value indicating if the view range intersect the date range of the specified item
        /// </summary>
        /// <param name="item"></param>
        public bool ViewIntersects(CalendarItem item)
        {
            return ViewIntersects(item.StartDate, item.EndDate);
        }

        #endregion

        #region Private Methods
       
        protected override bool IsInputKey(Keys keyData)
        {
            if (
                keyData == Keys.Down ||
                keyData == Keys.Up ||
                keyData == Keys.Right || 
                keyData == Keys.Left)
            {
                return true;
            }
            else
            {

                return base.IsInputKey(keyData);
            }
        }

        /// <summary>
        /// Removes all the items currently on the calendar
        /// </summary>
        private void ClearItems()
        {
            Items.Clear();
            Renderer.DayTopHeight = Renderer.DayTopMinHeight;
        }

        /// <summary>
        /// Unselects the selected items
        /// </summary>
        private void ClearSelectedItems()
        {
            foreach (CalendarItem item in Items)
            {
                bool wasSelected = item.Selected;

                item.SetSelected(false);

                // Invalidate(CalendarItem) covers all of the item's rectangles (an item spanning
                // several weeks has one per week) plus its shadow, not just the first one.
                if (wasSelected)
                {
                    Invalidate(item);
                }
            }
        }

        /// <summary>
        /// Deletes the currently selected item
        /// </summary>
        private void DeleteSelectedItems()
        {
            Stack<CalendarItem> toDelete = new Stack<CalendarItem>();

            foreach (CalendarItem item in Items)
            {
                // Check if Item is locked
                if (!item.Locked && item.Selected)
                {
                    CalendarItemCancelEventArgs evt = new CalendarItemCancelEventArgs(item);

                    OnItemDeleting(evt);

                    if (!evt.Cancel)
                    {
                        toDelete.Push(item);
                    }
                }
            }

            if (toDelete.Count  > 0)
            {
                while (toDelete.Count > 0)
                {
                    CalendarItem item = toDelete.Pop();

                    Items.Remove(item);

                    OnItemDeleted(new CalendarItemEventArgs(item));
                }

                Renderer.PerformItemsLayout();
            }
        }

        /// <summary>
        /// Clears current items and reloads for specified view
        /// </summary>
        private void ReloadItems()
        {
            OnLoadItems(new CalendarLoadEventArgs(this, ViewStart, ViewEnd));
        }

        /// <summary>
        /// Grows the rectangle to repaint currently selected elements
        /// </summary>
        /// <param name="rect"></param>
        private void GrowSquare(Rectangle rect)
        {
            if (_selectedElementSquare.IsEmpty)
            {
                _selectedElementSquare = rect;
            }
            else
            {
                _selectedElementSquare = Rectangle.Union(_selectedElementSquare, rect);
            }
        }

        /// <summary>
        /// Clears selection of currently selected components (As quick as possible)
        /// </summary>
        private void ClearSelectedComponents()
        {
            foreach (CalendarSelectableElement element in _selectedElements)
            {
                element.SetSelected(false);
            }

            _selectedElements.Clear();

            Invalidate(_selectedElementSquare);
            _selectedElementSquare = Rectangle.Empty;

        }

        /// <summary>
        /// Scrolls the calendar using the specified delta
        /// </summary>
        /// <param name="delta"></param>
        private void ScrollCalendar(int delta)
        {
            if (delta < 0)
            {
                SetViewRange(ViewStart.AddDays(7), ViewEnd.AddDays(7));
            }
            else
            {
                SetViewRange(ViewStart.AddDays(-7), ViewEnd.AddDays(-7));
            }
        }

        /// <summary>
        /// Raises the <see cref="ItemsPositioned"/> event
        /// </summary>
        internal void RaiseItemsPositioned()
        {
            OnItemsPositioned(EventArgs.Empty);
        }

        /// <summary>
        /// Scrolls the time units using the specified delta
        /// </summary>
        /// <param name="delta"></param>
        private void ScrollTimeUnits(int delta)
        {
            int possible = TimeUnitsOffset;
            int visible = Renderer.GetVisibleTimeUnits();

            if (delta < 0)
            {
                possible--;
            }
            else
            {
                possible++;
            }

            if (possible > 0)
            {
                possible = 0;
            }
            else if (
                Days != null
                && Days.Length > 0
                && Days[0].TimeUnits != null
                && possible * -1 >= Days[0].TimeUnits.Length)
            {
                possible = Days[0].TimeUnits.Length - 1;
                possible *= -1;
            }
            else if (Days != null
               && Days.Length > 0
               && Days[0].TimeUnits != null)
            {
                int max = Days[0].TimeUnits.Length - visible;
                max *= -1;
                if (possible < max) possible = max;
            }

            if (possible != TimeUnitsOffset)
            {
                TimeUnitsOffset = possible;
            }
        }

        /// <summary>
        /// Sets the value of the <see cref="DaysMode"/> property.
        /// </summary>
        /// <param name="mode">Mode in which days will be rendered</param>
        private void SetDaysMode(CalendarDaysMode mode)
        {
            _daysMode = mode;
        }

        /// <summary>
        /// Sets the value of the <see cref="State"/> property
        /// </summary>
        /// <param name="state">Current state of the calendar</param>
        private void SetState(CalendarState state)
        {
            _state = state;
        }

        /// <summary>
        /// Handles the LostFocus event of the TextBox that edit items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            FinalizeEditMode(false);
        }

        /// <summary>
        /// Handles the Keydown event of the TextBox that edit items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                FinalizeEditMode(true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                FinalizeEditMode(false);
            }
        }

        /// <summary>
        /// Updates the 
        /// </summary>
        private void UpdateDaysAndWeeks()
        {
            TimeSpan span = (new DateTime(ViewEnd.Year, ViewEnd.Month, ViewEnd.Day, 23, 59, 59)).Subtract(ViewStart.Date);
            int preDays = 0;
            span = span.Add(new TimeSpan(0,0,0,1,0));

            // The setters and SetViewRange already validate the range before storing it
            // (ValidateViewRange); this is only a safety net.
            ValidateViewRange(ViewStart, ViewEnd, "value");

            if (span.Days > MaximumFullDays)
            {
                SetDaysMode(CalendarDaysMode.Short);
                preDays = ((int)ViewStart.DayOfWeek - (int)FirstDayOfWeek + 7) % 7; // days back to the week start, always 0..6
                span = span.Add(new TimeSpan(preDays, 0, 0, 0));

                while (span.Days % 7 != 0)
                    span = span.Add(new TimeSpan(1, 0, 0, 0));
            }
            else
            {
                SetDaysMode(CalendarDaysMode.Expanded);
            }

            // The current selection refers to time units / days of the view being replaced.
            // Keeping it would mix elements of two views (e.g. Shift+arrow would then loop forever
            // in UpdateSelectionElements), so drop it.
            ClearSelectedComponents();
            _selectedElementStart = null;
            _selectedElementEnd = null;

            _days = new CalendarDay[span.Days];

            for (int i = 0; i < Days.Length; i++)
                Days[i] = new CalendarDay(this, ViewStart.AddDays(-preDays + i), i);

            
            //Weeks
            if (DaysMode == CalendarDaysMode.Short)
            {
                List<CalendarWeek> weeks = new List<CalendarWeek>();

                for (int i = 0; i < Days.Length; i++)
                {
                    if (Days[i].Date.DayOfWeek == FirstDayOfWeek)
                    {
                        weeks.Add(new CalendarWeek(this, Days[i].Date));
                    }
                }

                _weeks = weeks.ToArray();
            }
            else
            {
                _weeks = new CalendarWeek[] { };
            }

            UpdateHighlights();
            
        }

        /// <summary>
        /// Updates the value of the <see cref="CalendarTimeScaleUnit.Highlighted"/> property on the time units of days.
        /// </summary>
        internal void UpdateHighlights()
        {
            if (Days == null) return;

            for (int i = 0; i < Days.Length; i++)
            {
                Days[i].UpdateHighlights();
            }
        }

        /// <summary>
        /// Informs elements who's selected and who's not, and repaints <see cref="_selectedElementSquare"/>
        /// </summary>
        private void UpdateSelectionElements()
        {
            CalendarTimeScaleUnit unitStart = _selectedElementStart as CalendarTimeScaleUnit;
            CalendarDayTop topStart = _selectedElementStart as CalendarDayTop;
            CalendarDay dayStart = _selectedElementStart as CalendarDay;
            CalendarTimeScaleUnit unitEnd = _selectedElementEnd as CalendarTimeScaleUnit;
            CalendarDayTop topEnd = _selectedElementEnd as CalendarDayTop;
            CalendarDay dayEnd = _selectedElementEnd as CalendarDay;

            ClearSelectedComponents();

            if (_selectedElementEnd == null || _selectedElementStart == null) return;

            if (_selectedElementEnd.CompareTo(SelectedElementStart) < 0)
            {
                //swap
                unitStart = _selectedElementEnd as CalendarTimeScaleUnit;
                topStart = _selectedElementEnd as CalendarDayTop;
                dayStart = _selectedElementEnd as CalendarDay;
                unitEnd = SelectedElementStart as CalendarTimeScaleUnit;
                topEnd = SelectedElementStart as CalendarDayTop;
                dayEnd = _selectedElementStart as CalendarDay;
            }

            if (unitStart != null && unitEnd != null)
            {
                bool reached = false;
                // i < Days.Length: stop at the last day even if unitEnd was never reached.
                for (int i = unitStart.Day.Index; !reached && i < Days.Length; i++)
                {
                    for (int j = (i == unitStart.Day.Index ? unitStart.Index : 0); i < Days.Length && j < Days[i].TimeUnits.Length; j++)
                    {
                        CalendarTimeScaleUnit unit = Days[i].TimeUnits[j];
                        unit.SetSelected(true);
                        GrowSquare(unit.Bounds);
                        _selectedElements.Add(unit);

                        if (unit.Equals(unitEnd))
                        {
                            reached = true;
                            break;
                        }
                    }
                }
            }
            else if (topStart != null && topEnd != null)
            {
                for (int i = topStart.Day.Index; i <= topEnd.Day.Index ; i++)
                {
                    CalendarDayTop top = Days[i].DayTop;

                    top.SetSelected(true);
                    GrowSquare(top.Bounds);
                    _selectedElements.Add(top);
                }
            }
            else if (dayStart != null && dayEnd != null)
            {
                for (int i = dayStart.Index; i <= dayEnd.Index; i++)
                {
                    CalendarDay day = Days[i];

                    day.SetSelected(true);
                    GrowSquare(day.Bounds);
                    _selectedElements.Add(day);
                }
            }

            Invalidate(_selectedElementSquare);
        }

        #endregion

        #region Overrided Events and Raisers

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            Renderer.OnInitialize(new CalendarRendererEventArgs(new CalendarRendererEventArgs(this, null, Rectangle.Empty)));
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            Select();
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            //fix
            //CreateItemOnSelection(string.Empty, true);
            if (this.AllowNew)
                CreateItemOnSelection(string.Empty, true);
            //koniec
        }

        protected virtual void OnDayHeaderClick(CalendarDayEventArgs e)
        {
            if (DayHeaderClick != null)
            {
                DayHeaderClick(this, e);
            }
        }

        protected virtual void OnItemClick(CalendarItemEventArgs e)
        {
            if (ItemClick != null)
            {
                ItemClick(this, e);
            }
        }

        protected virtual void OnItemCreating(CalendarItemCancelEventArgs e)
        {
            if (ItemCreating != null)
            {
                ItemCreating(this, e);
            }
        }

        protected virtual void OnItemCreated(CalendarItemCancelEventArgs e)
        {
            if (ItemCreated != null)
            {
                ItemCreated(this, e);
            }
        }

        protected virtual void OnItemDeleting(CalendarItemCancelEventArgs e)
        {
            if (ItemDeleting != null)
            {
                ItemDeleting(this, e);
            }
        }

        protected virtual void OnItemDeleted(CalendarItemEventArgs e)
        {
            if (ItemDeleted != null)
            {
                ItemDeleted(this, e);
            }
        }

        protected virtual void OnItemDoubleClick(CalendarItemEventArgs e)
        {
            if (ItemDoubleClick != null)
            {
                ItemDoubleClick(this, e);
            }
        }

        protected virtual void OnItemEditing(CalendarItemCancelEventArgs e)
        {
            if (ItemTextEditing != null)
            {
                ItemTextEditing(this, e);
            }
        }

        protected virtual void OnItemEdited(CalendarItemCancelEventArgs e)
        {
            if (ItemTextEdited != null)
            {
                ItemTextEdited(this, e);
            }
        }

        protected virtual void OnItemSelected(CalendarItemEventArgs e)
        {
            if (ItemSelected != null)
            {
                ItemSelected(this, e);
            }
        }

        protected virtual void OnItemsPositioned(EventArgs e)
        {
            if (ItemsPositioned != null)
            {
                ItemsPositioned(this, e);
            }
        }

        protected virtual void OnItemDatesChanged(CalendarItemEventArgs e)
        {
            if (ItemDatesChanged != null)
            {
                ItemDatesChanged(this, e);
            }
        }

        protected virtual void OnItemMouseHover(CalendarItemEventArgs e)
        {
            if (ItemMouseHover != null)
            {
                ItemMouseHover(this, e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            bool shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
            int jump = (int)TimeScale;
            ICalendarSelectableElement sStart = null;
            ICalendarSelectableElement sEnd = null;

            if (e.KeyCode == Keys.F2)
            {
                ActivateEditMode();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedItems();
            }
            else if (e.KeyCode == Keys.Insert)
            {
                if (AllowNew)
                    CreateItemOnSelection(string.Empty, true);
            }
            else if (DaysMode == CalendarDaysMode.Short && SelectedElementEnd != null && MonthViewDaysForKey(e.KeyCode) != 0)
            {
                // The month view shows whole days (time units are hidden there), so the arrows move
                // by a day (left/right) or a week (up/down), and Shift extends the selection.
                CalendarDay target = FindDay(SelectedElementEnd.Date.Date.AddDays(MonthViewDaysForKey(e.KeyCode)));

                if (target != null)
                {
                    if (e.Shift && SelectedElementStart != null)
                        sStart = SelectedElementStart;

                    sEnd = target;
                }
            }
            else if (e.KeyCode == Keys.Down && SelectedElementStart != null && SelectedElementEnd != null)
            {
                if (e.Shift)
                    sStart = SelectedElementStart;

                sEnd = GetTimeUnit(SelectedElementEnd.Date.Add(new TimeSpan(0,(int)TimeScale,0)));
            }
            else if (e.KeyCode == Keys.Up && SelectedElementStart != null && SelectedElementEnd != null)
            {
                if (e.Shift)
                    sStart = SelectedElementStart;

                sEnd = GetTimeUnit(SelectedElementEnd.Date.Add(new TimeSpan(0, -(int)TimeScale, 0)));
            }
            else if (e.KeyCode == Keys.Right && SelectedElementEnd != null)
            {
                sEnd = GetTimeUnit(SelectedElementEnd.Date.Add(new TimeSpan(24, 0, 0)));
            }
            else if (e.KeyCode == Keys.Left && SelectedElementEnd != null)
            {
                sEnd = GetTimeUnit(SelectedElementEnd.Date.Add(new TimeSpan(-24, 0, 0)));
            }
            else if (e.KeyCode == Keys.PageDown && SelectedElementEnd != null)
            {
                sEnd = GetTimeUnit(SelectedElementEnd.Date.Add(new TimeSpan(0, (int)TimeScale * 3, 0)));
            }
            else if (e.KeyCode == Keys.PageUp && SelectedElementEnd != null)
            {
                sEnd = GetTimeUnit(SelectedElementEnd.Date.Add(new TimeSpan(0, -(int)TimeScale * 3, 0)));
            }
            

            // sEnd is null when the key would leave the view; the selection then stays as it is
            // (previously Shift+arrow at the edge of the view cleared it).
            if (sStart != null && sEnd != null)
            {
                SetSelectionRange(sStart, sEnd);
            }
            else if (sEnd != null)
            {
                SetSelectionRange(sEnd, sEnd);

                if(sEnd is CalendarTimeScaleUnit)
                    EnsureVisible(sEnd as CalendarTimeScaleUnit);
            }
        }

        /// <summary>
        /// How many days an arrow or page key moves the selection in the month view; 0 for other keys.
        /// </summary>
        private static int MonthViewDaysForKey(Keys key)
        {
            switch (key)
            {
                case Keys.Right: return 1;
                case Keys.Left: return -1;
                case Keys.Down:
                case Keys.PageDown: return 7;
                case Keys.Up:
                case Keys.PageUp: return -7;
                default: return 0;
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            // Only printable characters start a new item; Enter, Escape, Backspace, Ctrl+key
            // and other control characters would otherwise create empty items.
            if (AllowNew && !char.IsControl(e.KeyChar))
                CreateItemOnSelection(e.KeyChar.ToString(), true);
        }

        protected virtual void OnLoadItems(CalendarLoadEventArgs e)
        {
            if (LoadItems != null)
            {
                LoadItems(this, e);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            CalendarItem item = ItemAt(e.Location);

            if (item != null)
            {
                OnItemDoubleClick(new CalendarItemEventArgs(item));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)

        {

            base.OnMouseDown(e);
            
        ICalendarSelectableElement hitted = HitTest(e.Location);
            CalendarItem hittedItem = hitted as CalendarItem;
            bool shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (!Focused)
            {
                Focus();
            }

            switch (State)
            {
                case CalendarState.Idle:
                    if (hittedItem != null)
                    {
                        if (!shiftPressed)
                            ClearSelectedItems();

                        hittedItem.SetSelected(true);
                        Invalidate(hittedItem);
                        OnItemSelected(new CalendarItemEventArgs(hittedItem));

                        itemOnState = hittedItem;
                        itemOnStateChanged = false;

                        if (!hittedItem.Locked && AllowItemEdit)
                        {
                            ////<----- Add the following code to initialize draggingOffsetInMinutes<br />
                            ICalendarSelectableElement clickedTimeUnit = HitTest(e.Location, true);
                                    if (clickedTimeUnit == null)
                                        draggingOffsetInMinutes = 0;
                                    else
                                        draggingOffsetInMinutes = (long)hittedItem.Date.Subtract(clickedTimeUnit.Date).TotalMinutes;
                             //<-------End

                            if (itemOnState.ResizeStartDateZone(e.Location) && AllowItemResize)
                            {
                                SetState(CalendarState.ResizingItem);
                                itemOnState.SetIsResizingStartDate(true);
                            }
                            else if (itemOnState.ResizeEndDateZone(e.Location) && AllowItemResize)
                            {
                                SetState(CalendarState.ResizingItem);
                                itemOnState.SetIsResizingEndDate(true);
                            }
                            else
                            {
                                SetState(CalendarState.DraggingItem);
                            } 
                        }

                        SetSelectionRange(null, null);
                    }
                    else
                    {
                        ClearSelectedItems();

                        if (shiftPressed)
                        {
                            // Shift+click extends the current selection to the clicked element,
                            // or starts a new one when nothing is selected yet.
                            if (hitted != null)
                            {
                                if (SelectedElementStart == null)
                                    SetSelectionRange(hitted, hitted);
                                else if (SelectedElementEnd == null || !SelectedElementEnd.Equals(hitted))
                                    SelectedElementEnd = hitted;
                            }
                        }
                        else
                        {
                            if (SelectedElementStart == null || (hitted != null && !SelectedElementStart.Equals(hitted)))
                            {
                                SetSelectionRange(hitted, hitted);
                            }
                        }

                        SetState(CalendarState.DraggingTimeSelection);
                    }
                    break;
                case CalendarState.DraggingTimeSelection:
                    break;
                case CalendarState.DraggingItem:
                    break;
                case CalendarState.ResizingItem:
                    break;
                case CalendarState.EditingItemText:
                    break;
                    
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            ICalendarSelectableElement hitted = HitTest(e.Location, State != CalendarState.Idle);
            CalendarItem hittedItem = hitted as CalendarItem;
            CalendarDayTop hittedTop = hitted as CalendarDayTop;
            bool shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (hitted != null)
            {
                switch (State)
                {
                    case CalendarState.Idle:
                        Cursor should = Cursors.Default;

                        if (hittedItem != null)
                        {
                            // Check if Item is locked, do not change Cursor to SizeWE or SizeNS
                            if (!hittedItem.Locked && (hittedItem.ResizeEndDateZone(e.Location) || hittedItem.ResizeStartDateZone(e.Location)) && AllowItemResize)
                            {
                                should = hittedItem.IsOnDayTop || DaysMode == CalendarDaysMode.Short ? Cursors.SizeWE : Cursors.SizeNS;
                            }

                            OnItemMouseHover(new CalendarItemEventArgs(hittedItem));

                        }
                        if (!Cursor.Equals(should)) Cursor = should;
                        break;
                    case CalendarState.DraggingTimeSelection:
                        if (SelectedElementStart != null && (SelectedElementEnd == null || !SelectedElementEnd.Equals(hitted)))
                            SelectedElementEnd = hitted;
                        break;
                    //case CalendarState.DraggingItem:
                    //    TimeSpan duration = itemOnState.Duration;
                    //    itemOnState.SetIsDragging(true);
                    //    itemOnState.StartDate = hitted.Date;
                    //    itemOnState.EndDate = itemOnState.StartDate.Add(duration);
                    //    Renderer.PerformItemsLayout();
                    //    Invalidate();
                    //    itemOnStateChanged = true;
                    //    break;
                    case CalendarState.DraggingItem:
                        TimeSpan duration = itemOnState.Duration;
                        itemOnState.SetIsDragging(true);

                        // Keep the item where it was grabbed relative to the mouse: without the offset
                        // the item's start jumped to the element under the mouse on the first move.
                        DateTime target = hitted.Date.AddMinutes(draggingOffsetInMinutes);
                        DateTime newStart;

                        if ((this.DaysMode == CalendarDaysMode.Short) || (hitted is CalendarDayTop))
                        {
                            // Moving between days keeps the time of day.
                            newStart = target.Date.Add(itemOnState.StartDate.TimeOfDay);
                        }
                        else
                            newStart = target;

                        if (newStart != itemOnState.StartDate)
                        {
                            itemOnState.StartDate = newStart;
                            itemOnState.EndDate = newStart.Add(duration);
                            Renderer.PerformItemsLayout();
                            Invalidate();
                            itemOnStateChanged = true;
                        }
                        break;
                    case CalendarState.ResizingItem:
                        //fix
                        //if (itemOnState.IsResizingEndDate && hitted.Date.CompareTo(itemOnState.StartDate) >= 0)
                        //{
                        //    itemOnState.EndDate = hitted.Date.Add(hittedTop != null || DaysMode == CalendarDaysMode.Short? new TimeSpan(23, 59, 59) : Days[0].TimeUnits[0].Duration );
                        //}
                        //else if (itemOnState.IsResizingStartDate && hitted.Date.CompareTo(itemOnState.EndDate) <= 0)
                        //{
                        //    itemOnState.StartDate = hitted.Date;
                        //}
                        if (itemOnState.IsResizingEndDate)
                        {
                            if (hitted.Date.CompareTo(itemOnState.StartDate) >= 0)
                            {
                                itemOnState.EndDate = hitted.Date.Add(hittedTop != null || DaysMode == CalendarDaysMode.Short ? new TimeSpan(23, 59, 59) : Days[0].TimeUnits[0].Duration);
                            }
                            else
                            {
                                itemOnState.SetIsResizingEndDate(false);
                                itemOnState.SetIsResizingStartDate(true);
                            }
                        }
                        else if (itemOnState.IsResizingStartDate)
                        {
                            if (hitted.Date.CompareTo(itemOnState.EndDate) < 0)
                            {
                                itemOnState.StartDate = hitted.Date;
                            }
                            else
                            {
                                itemOnState.SetIsResizingStartDate(false);
                                itemOnState.SetIsResizingEndDate(true);
                            }
                        }
                        //fix koniec
                        Renderer.PerformItemsLayout();
                        Invalidate();
                        itemOnStateChanged = true;
                        break;
                    case CalendarState.EditingItemText:
                        break;     
                } 
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            ICalendarSelectableElement hitted = HitTest(e.Location, State == CalendarState.DraggingTimeSelection);
            CalendarItem hittedItem = hitted as CalendarItem;
            CalendarDay hittedDay = hitted as CalendarDay;
            bool shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;

            switch (State)
            {
                case CalendarState.Idle:
                    
                    break;
                case CalendarState.DraggingTimeSelection:
                    if (SelectedElementStart == null || (hitted != null && (SelectedElementEnd == null || !SelectedElementEnd.Equals(hitted))))
                    {
                        SelectedElementEnd = hitted;
                    }
                    if (hittedDay != null)
                    {
                        if (hittedDay.HeaderBounds.Contains(e.Location))
                        {
                            OnDayHeaderClick(new CalendarDayEventArgs(hittedDay));
                        }
                    }
                    break;
                case CalendarState.DraggingItem:
                    if(itemOnStateChanged)
                        OnItemDatesChanged(new CalendarItemEventArgs(itemOnState));
                    break;
                case CalendarState.ResizingItem:
                    if (itemOnStateChanged)
                        OnItemDatesChanged(new CalendarItemEventArgs(itemOnState));
                    break;
                case CalendarState.EditingItemText:
                    break;
            }

            if (itemOnState != null)
            {
                itemOnState.SetIsDragging(false);
                itemOnState.SetIsResizingEndDate(false);
                itemOnState.SetIsResizingStartDate(false);
                Invalidate(itemOnState);
                OnItemClick(new CalendarItemEventArgs(itemOnState));
                itemOnState = null;
            }
            SetState(CalendarState.Idle);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (DaysMode == CalendarDaysMode.Expanded)
            {
                ScrollTimeUnits(e.Delta);
            }
            else if (DaysMode == CalendarDaysMode.Short)
            {
                ScrollCalendar(e.Delta);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Items whose dates were changed from code since the last layout.
            EnsureItemsLayout();

            CalendarRendererEventArgs evt = new CalendarRendererEventArgs(this, e.Graphics, e.ClipRectangle);

            ///Calendar background
            Renderer.OnDrawBackground(evt);

            /// Headers / Timescale
            switch (DaysMode)
            {
                case CalendarDaysMode.Short:
                    Renderer.OnDrawDayNameHeaders(evt);
                    Renderer.OnDrawWeekHeaders(evt);
                    break;
                case CalendarDaysMode.Expanded:
                    Renderer.OnDrawTimeScale(evt);
                    break;
                default:
                    throw new NotImplementedException("Current DaysMode not implemented");
            }

            ///Days on view
            Renderer.OnDrawDays(evt);

            ///Items
            Renderer.OnDrawItems(evt);

            ///Overflow marks
            Renderer.OnDrawOverflows(evt);
        }

        protected override void Dispose(bool disposing)
        {
            // A shared holiday set would otherwise keep this control alive through its Changed event.
            if (disposing && _holidays != null)
            {
                _holidays.Changed -= Holidays_Changed;
            }

            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            TimeUnitsOffset = TimeUnitsOffset;
            Renderer.PerformLayout();
        }

        #endregion
    }
}
