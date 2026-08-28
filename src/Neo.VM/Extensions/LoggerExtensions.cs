// Copyright (C) 2015-2026 The Neo Project.
//
// LoggerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.VM.Logging;
using System;

namespace Neo.VM.Extensions;

/// <summary>
/// Source-generated logging helpers for virtual machine execution events.
/// </summary>
internal static partial class LoggerExtensions
{
    /// <summary>
    /// Logs a VM execute message.
    /// </summary>
    [LoggerMessage(
        EventId = VirtualMachineEventId.Execute,
        EventName = nameof(VirtualMachineEventId.Execute),
        Message = "{Message}"
    )]
    public static partial void LogExecuteMessage(
        this ILogger logger,
        LogLevel logLevel,
        string message
    );

    /// <summary>
    /// Logs a VM fault with an exception.
    /// </summary>
    [LoggerMessage(
        EventId = VirtualMachineEventId.Fault,
        EventName = nameof(VirtualMachineEventId.Fault),
        Message = "{Message}"
    )]
    public static partial void LogFaultMessage(
        this ILogger logger,
        LogLevel logLevel,
        Exception exception,
        string message
    );

    /// <summary>
    /// Logs a VM break (debugger breakpoint) message.
    /// </summary>
    [LoggerMessage(
        EventId = VirtualMachineEventId.Break,
        EventName = nameof(VirtualMachineEventId.Break),
        Message = "{Message}"
    )]
    public static partial void LogBreakMessage(
        this ILogger logger,
        LogLevel logLevel,
        string message
    );
}
