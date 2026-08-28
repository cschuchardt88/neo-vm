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
using Neo.Test.Types;
using Neo.VM;
using Neo.VM.Extensions;
using Neo.VM.Logging;
using Neo.VM.Middleware;
using System;

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
        Assert.IsTrue(logger.Entries.Exists(static e => e.EventId.Id == VirtualMachineEventId.Execute));
    }

    [TestMethod]
    public void TestLogsOpcodeOperand()
    {
        var logger = new CollectingLogger();
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.PUSHINT8, [0xAB]);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("PUSHINT8") && m.Contains("AB", StringComparison.OrdinalIgnoreCase)));
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
        Assert.IsTrue(logger.Entries.Exists(static e => e.EventId.Id == VirtualMachineEventId.Fault && e.Exception is not null));

        logger.Messages.Clear();
        logger.Entries.Clear();
        Assert.AreEqual(VMState.FAULT, engine.Execute());
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution starting") && m.Contains("FAULT")));
        Assert.IsTrue(logger.Entries.Exists(static e => e.Level == LogLevel.Critical && e.Exception is not null));
    }

    [TestMethod]
    public void TestNullLoggerDoesNotThrow()
    {
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestNullEngineThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ExecuteLoggerMiddleware(null!));
    }

    [TestMethod]
    public void TestDisabledLoggerSkipsWrites()
    {
        var logger = new CollectingLogger { MinLevel = LogLevel.None };
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.HasCount(0, logger.Messages);
    }

    [TestMethod]
    public void TestLoggerExtensions()
    {
        var logger = new CollectingLogger();
        logger.LogExecuteMessage(LogLevel.Information, "exec");
        logger.LogFaultMessage(LogLevel.Error, new InvalidOperationException("boom"), "fault");
        logger.LogBreakMessage(LogLevel.Debug, "break");

        Assert.AreEqual(VirtualMachineEventId.Execute, logger.Entries[0].EventId.Id);
        Assert.AreEqual("exec", logger.Entries[0].Message);
        Assert.AreEqual(VirtualMachineEventId.Fault, logger.Entries[1].EventId.Id);
        Assert.AreEqual("fault", logger.Entries[1].Message);
        Assert.IsInstanceOfType<InvalidOperationException>(logger.Entries[1].Exception);
        Assert.AreEqual(VirtualMachineEventId.Break, logger.Entries[2].EventId.Id);
        Assert.AreEqual("break", logger.Entries[2].Message);
    }
}
