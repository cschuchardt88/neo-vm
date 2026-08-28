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
    public void ImplicitConversions_FromStackItem()
    {
        StackItem number = 42;
        int i = number;
        long l = number;
        BigInteger big = number;
        Assert.AreEqual(42, i);
        Assert.AreEqual(42L, l);
        Assert.AreEqual(new BigInteger(42), big);

        StackItem flag = true;
        bool b = flag;
        Assert.IsTrue(b);

        byte[] data = [1, 2, 3];
        StackItem bytes = data;
        byte[] roundTrip = bytes;
        CollectionAssert.AreEqual(data, roundTrip);

        string text = number;
        Assert.AreEqual("42", text);
    }

    [TestMethod]
    public void ImplicitConversions_IntegerAndBuffer()
    {
        Integer n = 5;
        int i = n;
        BigInteger big = n;
        Assert.AreEqual(5, i);
        Assert.AreEqual(new BigInteger(5), big);

        byte[] data = [1, 2];
        Buffer buffer = data;
        byte[] copy = buffer;
        CollectionAssert.AreEqual(data, copy);
        BigInteger fromBuffer = buffer;
        Assert.AreEqual(new BigInteger(data), fromBuffer);
    }
}
