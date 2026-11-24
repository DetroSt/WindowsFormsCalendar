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
