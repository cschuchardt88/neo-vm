// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecuteLoggerMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Middleware;
using System;
using System.Collections.Generic;

namespace Neo.Test;

[TestClass]
public class UT_ExecuteLoggerMiddleware
{
    [TestMethod]
    public void TestLogsStartOpcodesAndFinish()
    {
        var logger = new CollectingLogger();
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.NOP);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());

        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution starting")));
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("NOP")));
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution finished") && m.Contains("HALT")));
    }

    [TestMethod]
    public void TestLogsFault()
    {
        var logger = new CollectingLogger();
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.ABORT);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.FAULT, engine.Execute());
        Assert.IsNotNull(engine.FaultException);
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution finished") && m.Contains("FAULT")));
    }

    private sealed class CollectingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
