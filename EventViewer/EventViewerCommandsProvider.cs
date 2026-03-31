// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EventViewer;

public partial class EventViewerCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public EventViewerCommandsProvider()
    {
        DisplayName = "Event Viewer";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.scale-200.png");
        _commands = [
            new CommandItem(new EventViewerPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
