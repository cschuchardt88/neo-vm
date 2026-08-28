// Copyright (C) 2015-2026 The Neo Project.
//
// VirtualMachineEventId.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Logging;

/// <summary>
/// Well-known event IDs used by VM logger messages.
/// </summary>
public static class VirtualMachineEventId
{
    /// <summary>Event ID for unrecoverable VM faults.</summary>
    public const int Fault = 100;

    /// <summary>Event ID for engine or context creation.</summary>
    public const int Create = 200;

    /// <summary>Event ID for script or context load operations.</summary>
    public const int Load = 201;

    /// <summary>Event ID for debugger breakpoint stops.</summary>
    public const int Break = 204;

    /// <summary>Event ID for general execution progress messages.</summary>
    public const int Execute = 205;
}
