# Windows Forms Calendar
Window calendar scheduler control

A calendar control based on https://www.codeproject.com/Articles/38699/A-Professional-Calendar-Agenda-View-That-You-Will with fixes i added from various comments on Codeproject

## Requirements

- `System.Windows.Forms.Calendar` (the control library) targets both **.NET Framework 4.8** (`net48`) and **.NET 10** (`net10.0-windows`), so it can be used from either kind of application.
- `CalendarDemo` targets **.NET 10** (`net10.0-windows`).
- Building needs the .NET 10 SDK and the .NET Framework 4.8 targeting pack (installed with Visual Studio's .NET desktop development workload, or separately as the .NET Framework 4.8 Developer Pack).

```
dotnet build System.Windows.Forms.Calendar.sln
```

## Tests

`System.Windows.Forms.Calendar.Tests` (MSTest) runs against both library builds (`net48` and `net10.0-windows`). It covers item layout and drawing, the item collection, view range and view limits, weeks starting on any day, week numbers, keyboard and mouse handling (dragging items, selecting days), scrolling and time scales, edit mode, highlighted work time, renderers, and the `MonthView` control.

```
dotnet test System.Windows.Forms.Calendar.sln
```

## Usage notes

- **Changing an item's dates from code** (`StartDate`, `EndDate`) moves it on screen automatically. The items are laid out once, before the next paint or when item positions are read (`Bounds`, `HitTest`), so changing many items costs a single layout. Adding, removing or replacing items through `calendar.Items` (including `IList` / `ICollection` access) also updates the layout. Calling `calendar.Renderer.PerformItemsLayout()` is no longer needed, but still works.
- **Times** follow the current culture: the time scale shows 00-23 for cultures with a 24-hour clock and 12, 01-11 with AM/PM otherwise, and items use the culture's short time pattern (e.g. `15:00` in pl-PL, `3:00 PM` in en-US) unless `ItemsTimeFormat` is set (e.g. to `hh:mm tt`, the previous default).
- **Week headers** in the month view use `WeekNumberFormat` (e.g. `"Tydz. {0}"`); without it they show `KW {0}` for German and `Week {0}` otherwise. Week numbers follow the current culture's rules.
- **Moving several items**: select items with Shift+click, then drag any of them; all selected items move by the same amount (by whole days when dragged between days, keeping each item's time of day). A click without dragging on one of the selected items selects only that item. If any selected item is `Locked`, the group is not moved. Resizing still applies to one item. After the mouse is released, `ItemDatesChanged` is raised for each moved item and then `ItemsDatesChanged` once with all of them (e.g. to save them in one transaction).
- **Cancelling a move**: Escape while dragging or resizing puts the items back where they were. The same happens when the mouse-up does not reach the calendar (another window, e.g. a message box, took the mouse during the drag); no date change is reported then. Only the left button moves or resizes items.
- **Selection**: `SelectionStart` / `SelectionEnd` return the selected range and select the time units (or, in the month view, the days) containing the given dates.

### Holidays

Days off can be shown with a different background in `Calendar` (all views) and `MonthView`. In `Calendar` the holiday name is shown in the day header (the full name also as a tooltip over the header) and work time (`HighlightRanges`) is not highlighted on holidays; in `MonthView` the name is shown as a tooltip. `GetHolidays(from, to)` lists the holidays of a date range, one entry per day.

```csharp
CalendarHolidays holidays = CalendarHolidays.LoadICalendar("holidays.ics");
calendar1.Holidays = holidays;   // one set can be shared by both controls
monthView1.Holidays = holidays;
```

The file is an iCalendar (`.ics`) file, UTF-8. Every `VEVENT` is a holiday: `SUMMARY` is its name, `DTSTART` its first day and `DTEND` (exclusive, as in all-day events) its end. Fixed-date holidays can repeat with `RRULE:FREQ=YEARLY` (with `INTERVAL`, `COUNT`, `UNTIL`, `EXDATE`); other recurrence rules (e.g. `BYDAY`) are reported as errors (`FormatException` with the line number), so movable holidays such as Easter are listed with their dates. Holidays can also be added from code (`Add`, `AddYearly`). Colors: `CalendarColorTable.HolidayBackground`, `HolidayTimeUnitBackground`, `HolidayDayTopBackground`, `HolidayText`, and `MonthView.HolidayBackgroundColor` / `HolidayTextColor`.

`CalendarDemo` loads `holidays.ics` from its folder; the sample file lists Polish public holidays (movable ones for 2026-2027) and should be checked against the official list before relying on it.

### Behaviour changes in this fork

Some setters now validate their values and throw instead of leaving the control in a broken state:

| Member | Throws |
|---|---|
| `MaximumViewDays` | `ArgumentOutOfRangeException` for values that are not a positive multiple of 7, or smaller than the current view |
| `MaximumFullDays` | `ArgumentOutOfRangeException` for values below 1 |
| `ViewStart`, `ViewEnd`, `SetViewRange` | `ArgumentOutOfRangeException` for ranges shorter than 1 day or longer than `MaximumViewDays` (the view is left unchanged) |
| `Renderer` | `ArgumentNullException` for `null` |
| `SelectionStart`, `SelectionEnd` | `ArgumentOutOfRangeException` for dates outside the current view |
| `WeekNumberFormat` | `ArgumentException` for an invalid composite format |
| `MonthView.DayNamesLength` | `ArgumentOutOfRangeException` for negative values |
| `CalendarColorTable.FromHex` | `ArgumentNullException` for `null`, `FormatException` for anything but 6 hex digits (optionally prefixed with `#`) |
