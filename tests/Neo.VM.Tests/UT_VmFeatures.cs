// Copyright (C) 2015-2026 The Neo Project.
//
// UT_VmFeatures.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.Test;

[TestClass]
public class UT_VmFeatures
{
    private static ExecutionEngine Engine(VmFeatures features)
    {
        var limits = ExecutionEngineLimits.Default with { Features = features };
        return new ExecutionEngine(null, limits);
    }

    private static VMState Run(ExecutionEngine engine, ScriptBuilder script)
    {
        engine.LoadScript(script.ToArray());
        return engine.Execute();
    }

    [TestMethod]
    public void Shl_ZeroShift_WithoutBoundedShift_DoesNotConsumeValue()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWBUFFER);
        sb.EmitPush(0);
        sb.Emit(OpCode.SHL);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        Assert.IsInstanceOfType(engine.ResultStack.Pop(), typeof(Buffer));
    }

    [TestMethod]
    public void Shl_ZeroShift_WithBoundedShift_ConsumesValue()
    {
        using var engine = Engine(VmFeatures.BoundedShift);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWBUFFER);
        sb.EmitPush(0);
        sb.Emit(OpCode.SHL);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void HasKey_NegativeIndex_Faults()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.Emit(OpCode.NEWARRAY0);
        sb.EmitPush(BigInteger.MinusOne);
        sb.Emit(OpCode.HASKEY);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void HasKey_LargeIndex_WithoutStrictContainerAccess_ReturnsFalse()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.Emit(OpCode.NEWARRAY0);
        sb.EmitPush((BigInteger)engine.Limits.MaxItemSize);
        sb.Emit(OpCode.HASKEY);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        Assert.IsFalse(engine.ResultStack.Pop().GetBoolean());
    }

    [TestMethod]
    public void HasKey_LargeIndex_WithStrictContainerAccess_Faults()
    {
        using var engine = Engine(VmFeatures.StrictContainerAccess);
        using var sb = new ScriptBuilder();
        sb.Emit(OpCode.NEWARRAY0);
        sb.EmitPush((BigInteger)engine.Limits.MaxItemSize);
        sb.Emit(OpCode.HASKEY);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void DefaultLimits_EnableCurrentFeatures()
    {
        Assert.IsTrue(ExecutionEngineLimits.Default.Has(VmFeatureSets.Current));
        using var engine = new ExecutionEngine();
        Assert.IsTrue(engine.Limits.Has(VmFeatures.BoundedShift));
        Assert.IsTrue(engine.Limits.Has(VmFeatures.SafeSubStr));
        Assert.IsTrue(engine.Limits.Has(VmFeatures.StrictContainerAccess));
    }
}
