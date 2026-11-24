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
- **Selection**: `SelectionStart` / `SelectionEnd` return the selected range and select the time units (or, in the month view, the days) containing the given dates.

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
