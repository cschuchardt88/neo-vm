// Copyright (C) 2015-2026 The Neo Project.
//
// ExecuteLoggerMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo.VM.Extensions;
using System;

namespace Neo.VM.Middleware;

/// <summary>
/// Middleware that logs VM startup, per-opcode execution (at trace level), and completion.
/// </summary>
public sealed class ExecuteLoggerMiddleware : IEngineMiddleware
{
    private readonly Func<ExecutionEngine> _getEngine;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteLoggerMiddleware"/> class.
    /// </summary>
    /// <param name="engine">The execution engine to log.</param>
    /// <param name="logger">Logger that receives execution messages.</param>
    public ExecuteLoggerMiddleware(ExecutionEngine engine, ILogger? logger = null)
        : this(() => engine, logger)
    {
        ArgumentNullException.ThrowIfNull(engine);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteLoggerMiddleware"/> class
    /// with a factory used to resolve the engine (for dependency injection).
    /// </summary>
    /// <param name="engineFactory">Factory that returns the execution engine.</param>
    /// <param name="logger">Logger that receives execution messages.</param>
    public ExecuteLoggerMiddleware(Func<ExecutionEngine> engineFactory, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(engineFactory);
        _getEngine = engineFactory;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public void PreExecution(ExecutionDelegate next)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            var engine = _getEngine();
            _logger.LogExecuteMessage(LogLevel.Information, $"VM execution starting | State: {engine.State}");
            if (engine.State == VMState.FAULT && engine.FaultException is not null)
                _logger.LogFaultMessage(LogLevel.Critical, engine.FaultException, engine.FaultException.Message);
        }

        next();
    }

    /// <inheritdoc />
    public void PreExecute(ExecutionContext? context, ExecuteDelegate next)
    {
        if (context is not null && _logger.IsEnabled(LogLevel.Trace))
        {
            var instruction = context.CurrentInstruction;
            var opcode = instruction?.OpCode.ToString() ?? "RET";
            var operand = instruction is { Operand.Length: > 0 }
                ? Convert.ToHexString(instruction.Operand.Span)
                : string.Empty;
            var suffix = operand.Length == 0 ? string.Empty : $" {operand}";
            _logger.LogExecuteMessage(
                LogLevel.Trace,
                $"Execute opcode | IP: {context.InstructionPointer:X04} | {opcode}{suffix}");
        }

        next(context);
    }

    /// <inheritdoc />
    public void PostExecute(ExecutionContext? context, ExecuteDelegate next)
        => next(context);

    /// <inheritdoc />
    public void PostExecution(ExecutionDelegate next)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            var engine = _getEngine();
            _logger.LogExecuteMessage(LogLevel.Information, $"VM execution finished | State: {engine.State}");
            if (engine.State == VMState.FAULT && engine.FaultException is not null)
                _logger.LogFaultMessage(LogLevel.Critical, engine.FaultException, engine.FaultException.Message);
        }

        next();
    }
}
