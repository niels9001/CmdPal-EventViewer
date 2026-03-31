// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using EventViewer.Models;
using EventViewer.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EventViewer.Pages;

internal sealed partial class EventLogListPage : DynamicListPage
{
    private readonly string _logName;
    private IListItem[] _items = [];

    public EventLogListPage(string logName)
    {
        _logName = logName;
        var displayName = logName == "ForwardedEvents" ? "Forwarded Events" : logName;
        Icon = new IconInfo("\uE7C3");
        Title = displayName;
        Name = "View";
        PlaceholderText = "Search events...";
        ShowDetails = true;

        var filters = new SeverityFilters();
        filters.PropChanged += (_, _) => LoadEvents();
        Filters = filters;

        LoadEvents();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => LoadEvents();

    public override IListItem[] GetItems() => _items;

    private void LoadEvents()
    {
        IsLoading = true;
        var searchText = SearchText;
        var filterId = ((SeverityFilters)Filters).CurrentFilterId;

        Task.Run(async () =>
        {
            var levelFilter = GetLevelFilter(filterId);

            var events = await EventLogService.GetEventsFromLogAsync(
                _logName,
                levels: levelFilter,
                searchText: string.IsNullOrWhiteSpace(searchText) ? null : searchText).ConfigureAwait(false);

            var items = new List<IListItem>(events.Count);

            foreach (var evt in events)
            {
                var firstLine = GetFirstLine(evt.Message, 120);

                var subtitle = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}  \u00B7  Event ID {1}  \u00B7  {2}",
                    evt.TimeCreated.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    evt.Id,
                    evt.ProviderName);

                var keepOpenCommand = new AnonymousCommand(() => { })
                {
                    Result = CommandResult.KeepOpen(),
                };

                items.Add(new ListItem(keepOpenCommand)
                {
                    Title = firstLine,
                    Subtitle = subtitle,
                    Icon = GetSeverityIcon(evt.Severity),
                    Tags = [GetSeverityTag(evt.Severity)],
                    MoreCommands = [
                        new CommandContextItem(new OpenEventViewerCommand(_logName))
                        {
                            Title = "Open in Event Viewer",
                            Subtitle = "Open this log in the Windows Event Viewer",
                            Icon = new IconInfo("\uE8A7"),
                        },
                        new CommandContextItem(new CopyTextCommand(evt.Message))
                        {
                            Title = "Copy message",
                        },
                    ],
                    Details = new Details
                    {
                        Title = string.Format(CultureInfo.InvariantCulture, "{0} \u2014 Event {1}", evt.ProviderName, evt.Id),
                        Body = evt.Message,
                        Metadata = [
                            new DetailsElement { Key = "Log name", Data = new DetailsTags { Tags = [new Tag(evt.LogName)] } },
                            new DetailsElement { Key = "Source", Data = new DetailsTags { Tags = [new Tag(evt.ProviderName)] } },
                            new DetailsElement { Key = "Event ID", Data = new DetailsTags { Tags = [new Tag(evt.Id.ToString(CultureInfo.InvariantCulture))] } },
                            new DetailsElement { Key = "Level", Data = new DetailsTags { Tags = [GetSeverityTag(evt.Severity)] } },
                            new DetailsElement { Key = "User", Data = new DetailsTags { Tags = [new Tag(evt.UserName ?? "N/A")] } },
                            new DetailsElement { Key = "Computer", Data = new DetailsTags { Tags = [new Tag(evt.MachineName ?? Environment.MachineName)] } },
                            new DetailsElement { Key = "Date/Time", Data = new DetailsTags { Tags = [new Tag(evt.TimeCreated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))] } },
                        ],
                    },
                });
            }

            if (events.Count == 0)
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "No events found",
                    Subtitle = string.IsNullOrWhiteSpace(searchText)
                        ? "This log is empty"
                        : string.Format(CultureInfo.InvariantCulture, "No events matching \"{0}\"", searchText),
                });
            }

            _items = items.ToArray();
            RaiseItemsChanged();
            IsLoading = false;
        });
    }

    private static EventSeverity[]? GetLevelFilter(string? filterId)
    {
        return filterId switch
        {
            "critical" => [EventSeverity.Critical],
            "error" => [EventSeverity.Critical, EventSeverity.Error],
            "warning" => [EventSeverity.Warning],
            "info" => [EventSeverity.Information],
            _ => null,
        };
    }

    private static IconInfo GetSeverityIcon(EventSeverity severity)
    {
        return severity switch
        {
            EventSeverity.Critical => new IconInfo("\u2757"),
            EventSeverity.Error => new IconInfo("\u2757"),
            EventSeverity.Warning => new IconInfo("\u26A0\uFE0F"),
            EventSeverity.Information => new IconInfo("\u2139\uFE0F"),
            _ => new IconInfo("\u2139\uFE0F"),
        };
    }

    private static Tag GetSeverityTag(EventSeverity severity)
    {
        return severity switch
        {
            EventSeverity.Critical => new Tag("Critical")
            {
                Background = new OptionalColor { HasValue = true, Color = new Color { R = 160, G = 0, B = 0, A = 255 } },
                Foreground = new OptionalColor { HasValue = true, Color = new Color { R = 255, G = 255, B = 255, A = 255 } },
            },
            EventSeverity.Error => new Tag("Error")
            {
                Background = new OptionalColor { HasValue = true, Color = new Color { R = 200, G = 40, B = 40, A = 255 } },
                Foreground = new OptionalColor { HasValue = true, Color = new Color { R = 255, G = 255, B = 255, A = 255 } },
            },
            EventSeverity.Warning => new Tag("Warning")
            {
                Background = new OptionalColor { HasValue = true, Color = new Color { R = 200, G = 160, B = 0, A = 255 } },
                Foreground = new OptionalColor { HasValue = true, Color = new Color { R = 0, G = 0, B = 0, A = 255 } },
            },
            _ => new Tag("Info")
            {
                Background = new OptionalColor { HasValue = true, Color = new Color { R = 60, G = 120, B = 200, A = 255 } },
                Foreground = new OptionalColor { HasValue = true, Color = new Color { R = 255, G = 255, B = 255, A = 255 } },
            },
        };
    }

    private static string GetFirstLine(string message, int maxLength)
    {
        var span = message.AsSpan();
        var newlineIndex = span.IndexOfAny('\r', '\n');
        if (newlineIndex >= 0)
        {
            span = span[..newlineIndex];
        }

        if (span.Length <= maxLength)
        {
            return span.ToString();
        }

        return string.Concat(span[..(maxLength - 3)], "...");
    }
}

internal sealed partial class SeverityFilters : Filters
{
    public SeverityFilters()
    {
        CurrentFilterId = "all";
    }

    public override IFilterItem[] GetFilters()
    {
        return
        [
            new Filter() { Id = "all", Name = "All" },
            new Filter() { Id = "error", Name = "Errors", Icon = new IconInfo("\u2757") },
            new Filter() { Id = "warning", Name = "Warnings", Icon = new IconInfo("\u26A0\uFE0F") },
            new Filter() { Id = "info", Name = "Information", Icon = new IconInfo("\u2139\uFE0F") },
            new Filter() { Id = "critical", Name = "Critical" },
        ];
    }
}

internal sealed partial class OpenEventViewerCommand : InvokableCommand
{
    private readonly string _logName;

    public OpenEventViewerCommand(string logName)
    {
        _logName = logName;
        Name = "Open in Event Viewer";
    }

    public override CommandResult Invoke()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "eventvwr.msc",
            Arguments = string.Format(CultureInfo.InvariantCulture, "/c:{0}", _logName),
            UseShellExecute = true,
        });

        return CommandResult.Hide();
    }
}
