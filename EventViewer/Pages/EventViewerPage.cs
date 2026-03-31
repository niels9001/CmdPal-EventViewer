// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EventViewer.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EventViewer;

internal sealed partial class EventViewerPage : ListPage
{
    private static readonly (string Name, string Icon)[] WindowsLogs =
    [
        ("Application", "\uE74C"),
        ("Security", "\uE72E"),
        ("Setup", "\uE713"),
        ("System", "\uE770"),
        ("ForwardedEvents", "\uE8B0"),
    ];

    public EventViewerPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.scale-200.png");
        Title = "Event Viewer";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        var items = new IListItem[WindowsLogs.Length];

        for (var i = 0; i < WindowsLogs.Length; i++)
        {
            var (name, icon) = WindowsLogs[i];
            var displayName = name == "ForwardedEvents" ? "Forwarded Events" : name;

            items[i] = new ListItem(new EventLogListPage(name))
            {
                Title = displayName,
                Icon = new IconInfo(icon),
                Subtitle = "Windows Logs",
            };
        }

        return items;
    }
}
