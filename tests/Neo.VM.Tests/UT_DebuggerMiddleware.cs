// Copyright (C) 2015-2026 The Neo Project.
//
// UT_DebuggerMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Middleware;
using System.Collections.Generic;

namespace Neo.Test;

[TestClass]
public class UT_DebuggerMiddleware
{
    [TestMethod]
    public void TestEngineExecuteStopsAtBreakpoint()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        debugger.AddBreakPoint(engine.CurrentContext.Script, 2);

        Assert.AreEqual(VMState.BREAK, engine.Execute());
        Assert.AreEqual(2, engine.CurrentContext.InstructionPointer);

        debugger.Continue();
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestOnBreakpointEvent()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.EmitPush(1);
        script.EmitPush(2);
        script.Emit(OpCode.ADD);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());

        var hits = new List<int>();
        DebuggerMiddleware debugger = new(engine);
        debugger.AddBreakpoint(2);
        debugger.OnBreakpoint += (_, e) => hits.Add(e.Context.InstructionPointer);

        Assert.AreEqual(VMState.BREAK, engine.Execute());
        CollectionAssert.AreEqual(new[] { 2 }, hits);

        debugger.Continue();
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(3, engine.ResultStack.Pop().GetInteger());
    }

    [TestMethod]
    public void TestStepMode()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine)
        {
            StepMode = true
        };

        Assert.AreEqual(VMState.BREAK, engine.Execute());
        Assert.AreEqual(1, engine.CurrentContext.InstructionPointer);

        debugger.Continue();
        Assert.AreEqual(VMState.BREAK, engine.Execute());
        Assert.AreEqual(2, engine.CurrentContext.InstructionPointer);
    }
}
