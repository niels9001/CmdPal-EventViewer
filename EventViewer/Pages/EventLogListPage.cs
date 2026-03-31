// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

    public EventLogListPage(string logName, IconInfo icon)
    {
        _logName = logName;
        var displayName = logName == "ForwardedEvents" ? "Forwarded Events" : logName;
        Icon = icon;
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

            var items = new List<IListItem>();

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
            else
            {
                var grouped = events.GroupBy(e => e.TimeCreated.Date).OrderByDescending(g => g.Key);

                foreach (var dayGroup in grouped)
                {
                    var sectionTitle = GetDaySectionTitle(dayGroup.Key);
                    var dayItems = new List<IListItem>();

                    foreach (var evt in dayGroup)
                    {
                        var firstLine = GetFirstLine(evt.Message, 120);

                        var subtitle = string.Format(
                            CultureInfo.InvariantCulture,
                            "Event ID {0}  \u00B7  {1}",
                            evt.Id,
                            evt.ProviderName);

                        var keepOpenCommand = new AnonymousCommand(() => { })
                        {
                            Result = CommandResult.KeepOpen(),
                        };

                        dayItems.Add(new ListItem(keepOpenCommand)
                        {
                            Title = firstLine,
                            Subtitle = subtitle,
                            Icon = GetSeverityIcon(evt.Severity),
                            Tags = [GetTimeTag(evt.TimeCreated)],
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
                                    new DetailsElement { Key = "Event ID", Data = new DetailsLink { Text = evt.Id.ToString(CultureInfo.InvariantCulture) } },
                                    new DetailsElement { Key = "Level", Data = new DetailsTags { Tags = [GetSeverityTag(evt.Severity)] } },
                                    new DetailsElement { Key = "Log", Data = new DetailsLink { Text = evt.LogName } },
                                    new DetailsElement { Key = string.Empty, Data = new DetailsSeparator() },
                                    new DetailsElement { Key = "Provider", Data = new DetailsLink { Text = evt.ProviderName } },
                                    new DetailsElement { Key = "Computer", Data = new DetailsLink { Text = evt.MachineName ?? Environment.MachineName } },
                                    new DetailsElement { Key = "User", Data = new DetailsLink { Text = evt.UserName ?? "N/A" } },
                                    new DetailsElement { Key = string.Empty, Data = new DetailsSeparator() },
                                    new DetailsElement { Key = "Date/Time", Data = new DetailsLink { Text = evt.TimeCreated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) } },
                                ],
                            },
                        });
                    }

                    var section = new Section(sectionTitle, dayItems.ToArray());
                    items.AddRange(section);
                }
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
            EventSeverity.Critical => IconHelpers.FromRelativePath("Assets\\Critical.svg"),
            EventSeverity.Error => IconHelpers.FromRelativePath("Assets\\Error.svg"),
            EventSeverity.Warning => IconHelpers.FromRelativePath("Assets\\Warning.svg"),
            _ => IconHelpers.FromRelativePath("Assets\\Info.svg"),
        };
    }

    private static Tag GetTimeTag(DateTime timeCreated)
    {
        return new Tag(timeCreated.ToString("h:mm tt", CultureInfo.InvariantCulture))
        {
            Icon = new IconInfo("\uE823"),
        };
    }

    private static string GetDaySectionTitle(DateTime date)
    {
        var today = DateTime.Today;
        if (date == today)
        {
            return "Today";
        }

        if (date == today.AddDays(-1))
        {
            return "Yesterday";
        }

        return date.ToString("dddd, MMMM d", CultureInfo.CurrentCulture);
    }

    private static Tag GetSeverityTag(EventSeverity severity)
    {
        return severity switch
        {
            EventSeverity.Critical => new Tag("Critical")
            {
                Background = ColorHelpers.FromRgb(160, 0, 0),
                Foreground = ColorHelpers.FromRgb(255, 255, 255),
            },
            EventSeverity.Error => new Tag("Error")
            {
                Background = ColorHelpers.FromRgb(200, 40, 40),
                Foreground = ColorHelpers.FromRgb(255, 255, 255),
            },
            EventSeverity.Warning => new Tag("Warning")
            {
                Background = ColorHelpers.FromRgb(200, 160, 0),
                Foreground = ColorHelpers.FromRgb(0, 0, 0),
            },
            _ => new Tag("Info")
            {
                Background = ColorHelpers.FromRgb(60, 120, 200),
                Foreground = ColorHelpers.FromRgb(255, 255, 255),
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
            new Filter() { Id = "all", Name = "All", Icon = new IconInfo("\uE71D") },
            new Filter() { Id = "error", Name = "Errors", Icon = IconHelpers.FromRelativePath("Assets\\Error.svg") },
            new Filter() { Id = "warning", Name = "Warnings", Icon = IconHelpers.FromRelativePath("Assets\\Warning.svg") },
            new Filter() { Id = "info", Name = "Information", Icon = IconHelpers.FromRelativePath("Assets\\Info.svg") },
            new Filter() { Id = "critical", Name = "Critical", Icon = IconHelpers.FromRelativePath("Assets\\Critical.svg") },
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
