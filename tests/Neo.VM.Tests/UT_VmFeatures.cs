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
    public void Shr_ZeroShift_WithoutBoundedShift_DoesNotConsumeValue()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWBUFFER);
        sb.EmitPush(0);
        sb.Emit(OpCode.SHR);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        Assert.IsInstanceOfType(engine.ResultStack.Pop(), typeof(Buffer));
    }

    [TestMethod]
    public void Shr_ZeroShift_WithBoundedShift_ConsumesValue()
    {
        using var engine = Engine(VmFeatures.BoundedShift);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWBUFFER);
        sb.EmitPush(0);
        sb.Emit(OpCode.SHR);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void SubStr_WithoutSafeSubStr_AllowsUncheckedAdd()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        byte[] data = [1, 2, 3, 4];
        sb.EmitPush(data);
        sb.EmitPush(1);
        sb.EmitPush(2);
        sb.Emit(OpCode.SUBSTR);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        byte[] expected = [2, 3];
        CollectionAssert.AreEqual(expected, engine.ResultStack.Pop().GetSpan().ToArray());
    }

    [TestMethod]
    public void SubStr_Wrap_WithSafeSubStr_Faults()
    {
        using var engine = Engine(VmFeatures.SafeSubStr);
        using var sb = new ScriptBuilder();
        byte[] data = [1, 2];
        sb.EmitPush(data);
        sb.EmitPush(int.MaxValue);
        sb.EmitPush(1);
        sb.Emit(OpCode.SUBSTR);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void SubStr_Wrap_WithoutSafeSubStr_DoesNotUseCheckedAdd()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        byte[] data = [1, 2];
        sb.EmitPush(data);
        sb.EmitPush(int.MaxValue);
        sb.EmitPush(1);
        sb.Emit(OpCode.SUBSTR);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void HasKey_BufferLargeIndex_WithoutStrict_ReturnsFalse()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWBUFFER);
        sb.EmitPush((BigInteger)engine.Limits.MaxItemSize);
        sb.Emit(OpCode.HASKEY);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        Assert.IsFalse(engine.ResultStack.Pop().GetBoolean());
    }

    [TestMethod]
    public void HasKey_BufferAndByteString()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWBUFFER);
        sb.EmitPush(0);
        sb.Emit(OpCode.HASKEY);
        byte[] data = [9];
        sb.EmitPush(data);
        sb.EmitPush(0);
        sb.Emit(OpCode.HASKEY);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
    }

    [TestMethod]
    public void PickItem_NegativeIndex_Faults()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.Emit(OpCode.NEWARRAY0);
        sb.EmitPush(BigInteger.MinusOne);
        sb.Emit(OpCode.PICKITEM);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void Remove_NegativeIndex_Faults()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.EmitPush(1);
        sb.Emit(OpCode.PACK);
        sb.EmitPush(BigInteger.MinusOne);
        sb.Emit(OpCode.REMOVE);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void SetItem_NegativeIndex_Faults()
    {
        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.EmitPush(1);
        sb.Emit(OpCode.PACK);
        sb.EmitPush(BigInteger.MinusOne);
        sb.EmitPush(0);
        sb.Emit(OpCode.SETITEM);
        Assert.AreEqual(VMState.FAULT, Run(engine, sb));
    }

    [TestMethod]
    public void ConvertTo_WithoutLimits_UsesDefaultFeatures()
    {
        StackItem item = true;
        var converted = item.ConvertTo(StackItemType.Boolean);
        Assert.IsTrue(converted.GetBoolean());

        using var engine = Engine(VmFeatures.None);
        using var sb = new ScriptBuilder();
        sb.EmitPush(true);
        sb.Emit(OpCode.CONVERT, [(byte)StackItemType.Boolean]);
        Assert.AreEqual(VMState.HALT, Run(engine, sb));
        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
    }

    [TestMethod]
    public void DefaultLimits_EnableCurrentFeatures()
    {
        Assert.IsTrue(ExecutionEngineLimits.Default.Has(VmFeatureSets.Current));
        using var engine = new ExecutionEngine();
        Assert.IsTrue(engine.Limits.Has(VmFeatures.BoundedShift));
        Assert.IsTrue(engine.Limits.Has(VmFeatures.SafeSubStr));
        Assert.IsTrue(engine.Limits.Has(VmFeatures.StrictContainerAccess));
        Assert.IsTrue(engine.Limits.Has(VmFeatures.IEquatableContent));
    }
}
