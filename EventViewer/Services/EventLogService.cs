// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Threading.Tasks;
using EventViewer.Models;

namespace EventViewer.Services;

internal static class EventLogService
{
    public static Task<List<EventLogEntry>> GetEventsFromLogAsync(
        string logName,
        TimeSpan? lookback = null,
        EventSeverity[]? levels = null,
        string? searchText = null,
        int maxResults = 200)
    {
        return Task.Run(() =>
        {
            try
            {
                return QueryLog(logName, lookback ?? TimeSpan.FromDays(7), levels, searchText, maxResults);
            }
            catch (UnauthorizedAccessException)
            {
                return [new EventLogEntry
                {
                    TimeCreated = DateTime.Now,
                    Severity = EventSeverity.Warning,
                    LevelDisplayName = "Warning",
                    ProviderName = "EventViewer Extension",
                    Id = 0,
                    Message = $"Access denied to log '{logName}'. Try running as administrator.",
                    LogName = logName,
                }];
            }
            catch (EventLogNotFoundException)
            {
                return [new EventLogEntry
                {
                    TimeCreated = DateTime.Now,
                    Severity = EventSeverity.Warning,
                    LevelDisplayName = "Warning",
                    ProviderName = "EventViewer Extension",
                    Id = 0,
                    Message = $"Log '{logName}' was not found.",
                    LogName = logName,
                }];
            }
        });
    }

    private static List<EventLogEntry> QueryLog(
        string logName,
        TimeSpan lookback,
        EventSeverity[]? levels,
        string? searchText,
        int maxResults)
    {
        var milliseconds = (long)lookback.TotalMilliseconds;
        string xpath;

        if (levels is not null && levels.Length > 0)
        {
            var parts = new List<string>(levels.Length);
            foreach (var level in levels)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "Level={0}", (byte)level));
            }

            var levelFilter = string.Join(" or ", parts);
            xpath = string.Format(
                CultureInfo.InvariantCulture,
                "*[System[TimeCreated[timediff(@SystemTime) <= {0}] and ({1})]]",
                milliseconds,
                levelFilter);
        }
        else
        {
            xpath = string.Format(
                CultureInfo.InvariantCulture,
                "*[System[TimeCreated[timediff(@SystemTime) <= {0}]]]",
                milliseconds);
        }

        var eventQuery = new EventLogQuery(logName, PathType.LogName, xpath)
        {
            ReverseDirection = true,
        };

        var results = new List<EventLogEntry>();
        var hasSearchFilter = !string.IsNullOrWhiteSpace(searchText);

        using var reader = new EventLogReader(eventQuery);

        while (results.Count < maxResults)
        {
            using var record = reader.ReadEvent();
            if (record is null)
            {
                break;
            }

            var entry = ToEntry(record, logName);

            if (hasSearchFilter &&
                !entry.Message.Contains(searchText!, StringComparison.OrdinalIgnoreCase) &&
                !entry.ProviderName.Contains(searchText!, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(entry);
        }

        return results;
    }

    private static EventLogEntry ToEntry(EventRecord record, string logName)
    {
        string message;
        try
        {
            message = record.FormatDescription() ?? "(No message available)";
        }
        catch
        {
            message = "(Unable to format event message)";
        }

        string levelName;
        EventSeverity severity;
        if (record.Level.HasValue)
        {
            severity = (EventSeverity)record.Level.Value;
            try
            {
                levelName = record.LevelDisplayName ?? severity.ToString();
            }
            catch
            {
                levelName = severity.ToString();
            }
        }
        else
        {
            severity = EventSeverity.Information;
            levelName = "Information";
        }

        return new EventLogEntry
        {
            TimeCreated = record.TimeCreated?.ToLocalTime() ?? DateTime.Now,
            Severity = severity,
            LevelDisplayName = levelName,
            ProviderName = record.ProviderName ?? "Unknown",
            Id = record.Id,
            Message = message,
            LogName = logName,
            MachineName = record.MachineName,
            UserName = record.UserId?.Value,
        };
    }
}
