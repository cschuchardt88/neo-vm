// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SafeSpan.cs file belongs to the neo project and is free
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
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_SafeSpan
{
    [TestMethod]
    public void Primitive_GetSafeSpan_MatchesGetSpan()
    {
        StackItem integer = 1;
        CollectionAssert.AreEqual(integer.GetSpan().ToArray(), integer.GetSafeSpan().ToArray());

        StackItem flag = true;
        CollectionAssert.AreEqual(flag.GetSpan().ToArray(), flag.GetSafeSpan().ToArray());

        byte[] data = [1, 2, 3];
        StackItem bytes = data;
        CollectionAssert.AreEqual(data, bytes.GetSafeSpan().ToArray());

        var buffer = new Buffer(data);
        CollectionAssert.AreEqual(data, buffer.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_ConcatenatesChildSpans()
    {
        var array = new Array { 1, 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_SkipsNullItems()
    {
        var array = new Array { 1, StackItem.Null, 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_CircularReference_DoesNotRecurse()
    {
        var array = new Array();
        array.Add(1);
        array.Add(array);
        byte[] expected = [1];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Struct_CircularReference_DoesNotRecurse()
    {
        var s = new Struct();
        s.Add(s);
        Assert.AreEqual(0, s.GetSafeSpan().Length);
    }

    [TestMethod]
    public void Map_ConcatenatesKeyAndValueSpans()
    {
        var map = new Map { [1] = 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Null_GetSafeSpan_IsEmpty()
    {
        Assert.AreEqual(0, StackItem.Null.GetSafeSpan().Length);
    }

    [TestMethod]
    public void GetSpan_OnArray_StillThrows()
    {
        var array = new Array { 1 };
        Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan());
    }

    [TestMethod]
    public void AsSpan_MatchesGetSafeSpan()
    {
        StackItem item = 7;
        CollectionAssert.AreEqual(item.GetSafeSpan().ToArray(), item.AsSpan().ToArray());

        var array = new Array { 1, 2 };
        CollectionAssert.AreEqual(array.GetSafeSpan().ToArray(), array.AsSpan().ToArray());
    }

    [TestMethod]
    public void ExplicitConversions_FromStackItem()
    {
        StackItem number = 42;
        Assert.AreEqual(42, (int)number);
        Assert.AreEqual(42L, (long)number);
        Assert.AreEqual(new BigInteger(42), (BigInteger)number);
        Assert.AreEqual((byte)42, (byte)number);
        Assert.AreEqual((sbyte)42, (sbyte)number);
        Assert.AreEqual((short)42, (short)number);
        Assert.AreEqual((ushort)42, (ushort)number);
        Assert.AreEqual(42u, (uint)number);
        Assert.AreEqual(42UL, (ulong)number);

        StackItem flag = true;
        Assert.IsTrue((bool)flag);

        byte[] data = [1, 2, 3];
        StackItem bytes = data;
        CollectionAssert.AreEqual(data, (byte[])bytes);

        Assert.AreEqual("42", (string)number);
    }

    [TestMethod]
    public void ExplicitConversions_IntegerBooleanBuffer()
    {
        Integer n = 5;
        Assert.AreEqual(5, (int)n);
        Assert.AreEqual(new BigInteger(5), (BigInteger)n);
        Assert.IsTrue((bool)n);

        Neo.VM.Types.Boolean flag = true;
        Assert.IsTrue((bool)flag);
        Assert.AreEqual(BigInteger.One, (BigInteger)flag);

        byte[] data = [1, 2];
        Buffer buffer = data;
        CollectionAssert.AreEqual(data, (byte[])buffer);
        Assert.AreEqual(new BigInteger(data), (BigInteger)buffer);

        ByteString bs = data;
        CollectionAssert.AreEqual(data, (byte[])bs);
    }

    [TestMethod]
    public void EmptyAndNested_GetSafeSpan()
    {
        Assert.AreEqual(0, new Array().GetSafeSpan().Length);
        Assert.AreEqual(0, new Map().GetSafeSpan().Length);
        Assert.AreEqual(0, ((StackItem)0).GetSafeSpan().Length);
        CollectionAssert.AreEqual(new byte[] { 0 }, ((StackItem)false).GetSafeSpan().ToArray());

        var nested = new Array { new Array { 1, 2 }, 3 };
        byte[] expected = [1, 2, 3];
        CollectionAssert.AreEqual(expected, nested.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Map_CircularReference_DoesNotRecurse()
    {
        var map = new Map();
        map[1] = map;
        byte[] expected = [1];
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void PointerAndInterop_GetSafeSpan_IsEmpty()
    {
        var pointer = new Pointer(Script.Empty, 0);
        Assert.AreEqual(0, pointer.GetSafeSpan().Length);
        var interop = new InteropInterface(new object());
        Assert.AreEqual(0, interop.GetSafeSpan().Length);
    }
}
