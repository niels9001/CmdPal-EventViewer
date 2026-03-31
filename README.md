# CmdPal-EventViewer

A [Command Palette](https://github.com/microsoft/CmdPal) extension that brings Windows Event Log viewing directly into the Command Palette experience.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)

## Features

- **Browse Event Logs** — Access all major Windows log categories: Application, Security, Setup, System, and Forwarded Events
- **Filter by Severity** — Narrow results by Critical, Error, Warning, Information, or Verbose levels
- **Time-Range Filtering** — View events from the last 7 days (default) with configurable time ranges
- **Search** — Search events by message content or provider name
- **Event Details** — See timestamp, provider, event ID, message, machine name, and user info

## Project Structure

```
EventViewer/
├── EventViewer.cs                  # IExtension implementation
├── EventViewerCommandsProvider.cs  # Command Palette command provider
├── Program.cs                      # Entry point (COM server)
├── Models/
│   └── EventLogEntry.cs            # Event log data model
├── Pages/
│   ├── EventViewerPage.cs          # Main page (log categories)
│   └── EventLogListPage.cs         # Event list display
├── Services/
│   └── EventLogService.cs          # Event log querying & XPath filtering
└── Assets/                         # Icons and images
```

## Prerequisites

- Windows 10 (19041) or later
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Command Palette](https://github.com/microsoft/CmdPal)

## Building

```bash
dotnet build EventViewer.sln
```

For a Release build with trimming:

```bash
dotnet build EventViewer.sln -c Release
```

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
