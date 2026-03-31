// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace EventViewer.Models;

internal sealed class EventLogEntry
{
    public required DateTime TimeCreated { get; init; }

    public required EventSeverity Severity { get; init; }

    public required string LevelDisplayName { get; init; }

    public required string ProviderName { get; init; }

    public required int Id { get; init; }

    public required string Message { get; init; }

    public required string LogName { get; init; }

    public string? MachineName { get; init; }

    public string? UserName { get; init; }
}

internal enum EventSeverity : byte
{
    Critical = 1,
    Error = 2,
    Warning = 3,
    Information = 4,
    Verbose = 5,
}
