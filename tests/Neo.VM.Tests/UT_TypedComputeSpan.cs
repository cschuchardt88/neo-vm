// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TypedComputeSpan.cs file belongs to the neo project and is free
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
using System;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_TypedComputeSpan
{
    [TestMethod]
    public void Integer_ComputeSpan_ToByteArray()
    {
        StackItem zero = 0;
        CollectionAssert.AreEqual(BigInteger.Zero.ToByteArray(), zero.GetSafeSpan().ToArray());
        Assert.AreEqual(0, zero.GetSpan().Length);

        StackItem one = 1;
        CollectionAssert.AreEqual(BigInteger.One.ToByteArray(), one.GetSafeSpan().ToArray());

        StackItem neg = -1;
        CollectionAssert.AreEqual(new BigInteger(-1).ToByteArray(), neg.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Boolean_ComputeSpan_OneOrZeroByte()
    {
        CollectionAssert.AreEqual(new byte[] { 1 }, ((StackItem)true).GetSafeSpan().ToArray());
        CollectionAssert.AreEqual(new byte[] { 0 }, ((StackItem)false).GetSafeSpan().ToArray());
        Assert.IsInstanceOfType<Boolean>((StackItem)true);
    }

    [TestMethod]
    public void ByteString_AndBuffer_Unchanged()
    {
        byte[] data = [1, 2, 3];
        StackItem bytes = data;
        CollectionAssert.AreEqual(data, bytes.GetSafeSpan().ToArray());
        CollectionAssert.AreEqual(data, ((ByteString)data).Memory.Span.ToArray());

        var buffer = new Buffer(data);
        CollectionAssert.AreEqual(data, buffer.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Null_ComputeSpan_IsEmpty()
    {
        Assert.AreEqual(0, StackItem.Null.GetSafeSpan().Length);
    }

    [TestMethod]
    public void Pointer_ComputeSpan_IsScriptBytes()
    {
        byte[] script = [0x10, 0x20, 0x30];
        var pointer = new Pointer(new Script(script), 2);
        CollectionAssert.AreEqual(script, pointer.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Interop_ComputeSpan_BlittableOrTypeName()
    {
        var boxed = new InteropInterface(123);
        CollectionAssert.AreEqual(BitConverter.GetBytes(123), boxed.GetSafeSpan().ToArray());

        var named = new InteropInterface(new object());
        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(nameof(Object)), named.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_AndStruct_SkipNull_ConcatChildren()
    {
        var array = new Array { 1, StackItem.Null, 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());

        var s = new Struct { 1, StackItem.Null, 2 };
        CollectionAssert.AreEqual(expected, s.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Map_ConcatenatesKeyThenValue()
    {
        var map = new Map { [1] = 2, [3] = 4 };
        CollectionAssert.AreEqual(map.GetSafeSpan().ToArray(), map.GetSafeSpan().ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, map.GetSafeSpan().ToArray());
    }
}
