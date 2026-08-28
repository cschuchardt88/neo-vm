// Copyright (C) 2015-2026 The Neo Project.
//
// StackItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types;

/// <summary>
/// The base class for all types in the VM.
/// </summary>
public abstract partial class StackItem : IEquatable<StackItem>
{
    /// <summary>
    /// Stack Item hashcode.
    /// </summary>
    private int _hashCode = 0;

    [ThreadStatic]
    private static Boolean? tls_true;

    /// <summary>
    /// Represents <see langword="true"/> in the VM.
    /// </summary>
    public static Boolean True
    {
        get
        {
            tls_true ??= new(true);
            return tls_true;
        }
    }

    [ThreadStatic]
    private static Boolean? tls_false;

    /// <summary>
    /// Represents <see langword="false"/> in the VM.
    /// </summary>
    public static Boolean False
    {
        get
        {
            tls_false ??= new(false);
            return tls_false;
        }
    }

    [ThreadStatic]
    private static Null? tls_null;

    /// <summary>
    /// Represents <see langword="null"/> in the VM.
    /// </summary>
    public static StackItem Null
    {
        get
        {
            tls_null ??= new();
            return tls_null;
        }
    }

    /// <summary>
    /// Indicates whether the object is <see cref="Null"/>.
    /// </summary>
    public bool IsNull => this is Null;

    /// <summary>
    /// The type of this VM object.
    /// </summary>
    public abstract StackItemType Type { get; }

    /// <summary>
    /// Byte length of parameterless GetSpan.
    /// </summary>
    public virtual int Size => GetSpan().Length;

    /// <summary>
    /// Convert the VM object to the specified type using default engine limits
    /// (<see cref="VmFeatureSets.Current"/>).
    /// </summary>
    /// <param name="type">The type to be converted to.</param>
    /// <returns>The converted object.</returns>
    public StackItem ConvertTo(StackItemType type)
        => ConvertTo(type, ExecutionEngineLimits.Default);

    /// <summary>
    /// Convert the VM object to the specified type.
    /// Opcode handlers pass <see cref="ExecutionEngine.Limits"/> so conversions
    /// can honor <see cref="VmFeatures"/> without storing flags on the item.
    /// </summary>
    /// <param name="type">The type to be converted to.</param>
    /// <param name="limits">Engine limits and feature flags for this execution.</param>
    /// <returns>The converted object.</returns>
    public virtual StackItem ConvertTo(StackItemType type, ExecutionEngineLimits limits)
    {
        if (type == Type) return this;
        if (type == StackItemType.Boolean) return GetBoolean();
        throw new InvalidCastException();
    }

    internal virtual void Cleanup()
    {
    }

    /// <summary>
    /// Copy the object and all its children.
    /// </summary>
    /// <returns>The copied object.</returns>
    public StackItem DeepCopy(bool asImmutable = false)
    {
        return DeepCopy(new(ReferenceEqualityComparer.Instance), asImmutable);
    }

    internal virtual StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
    {
        return this;
    }

    public sealed override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is StackItem item) return Equals(item);
        return false;
    }

    public virtual bool Equals(StackItem? other)
    {
        return ReferenceEquals(this, other);
    }

    internal virtual bool Equals(StackItem? other, ExecutionEngineLimits limits)
    {
        return Equals(other);
    }

    /// <summary>
    /// Generates a hash code based on the item's span.
    ///
    /// This method provides a hash code for the StackItem based on its byte span.
    /// It is used for efficient storage and retrieval in hash-based collections.
    ///
    /// Use this method when you need a hash code for a StackItem.
    /// </summary>
    /// <returns>The hash code for the StackItem.</returns>
    public override int GetHashCode()
    {
        if (_hashCode == 0)
        {
            _hashCode = HashCode.Combine(Type, GetSpan().XxHash3_32());
        }
        return _hashCode;
    }

    /// <summary>
    /// Wrap the specified <see cref="object"/> and return an <see cref="InteropInterface"/> containing the <see cref="object"/>.
    /// </summary>
    /// <param name="value">The wrapped <see cref="object"/>.</param>
    /// <returns></returns>
    public static StackItem FromInterface(object? value)
    {
        if (value is null) return Null;
        return new InteropInterface(value);
    }

    /// <summary>
    /// Get the boolean value represented by the VM object.
    /// </summary>
    /// <returns>The boolean value represented by the VM object.</returns>
    public abstract bool GetBoolean();

    /// <summary>
    /// Get the integer value represented by the VM object.
    /// </summary>
    /// <returns>The integer value represented by the VM object.</returns>
    public virtual BigInteger GetInteger()
    {
        throw new InvalidCastException();
    }

    /// <summary>
    /// Get the <see cref="object"/> wrapped by this interface and convert it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The wrapped <see cref="object"/>.</returns>
    [return: MaybeNull]
    public virtual T GetInterface<T>() where T : notnull
    {
        throw new InvalidCastException();
    }

    /// <summary>
    /// Get the readonly span used to read the VM object data.
    /// Same as <see cref="GetSpan(ExecutionEngineLimits)"/> with
    /// <see cref="ExecutionEngineLimits.Default"/>.
    /// </summary>
    public ReadOnlySpan<byte> GetSpan()
        => GetSpan(ExecutionEngineLimits.Default);

    /// <summary>
    /// Opcode path for splice handlers. Derived types only override
    /// <see cref="ComputeSpan"/>; this method always goes through
    /// <see cref="GetSafeSpan()"/>. Array/Map/Struct require
    /// <see cref="VmFeatures.CompoundSpan"/>.
    /// </summary>
    public ReadOnlySpan<byte> GetSpan(ExecutionEngineLimits limits)
    {
        if (this is CompoundType)
        {
            if (!limits.Has(VmFeatures.CompoundSpan))
                throw new InvalidCastException();
            var span = GetSafeSpan();
            limits.AssertMaxItemSize(span.Length);
            return span;
        }
        return GetSafeSpan();
    }

    /// <summary>
    /// Cycle-safe byte representation. Compounds always succeed here;
    /// parameterless GetSpan follows opcode limits.
    /// </summary>
    internal ReadOnlySpan<byte> GetSafeSpan()
    {
        var visited = new HashSet<StackItem>(ReferenceEqualityComparer.Instance);
        return GetSafeSpan(visited);
    }

    /// <summary>
    /// If <paramref name="visited"/> already contains this item, returns empty
    /// (circular reference). Otherwise computes <see cref="ComputeSpan"/>.
    /// </summary>
    protected internal ReadOnlySpan<byte> GetSafeSpan(HashSet<StackItem> visited)
    {
        if (!visited.Add(this))
            return [];
        try
        {
            return ComputeSpan(visited);
        }
        finally
        {
            visited.Remove(this);
        }
    }

    /// <summary>
    /// Type-specific bytes. Compounds recurse through
    /// <see cref="GetSafeSpan(HashSet{StackItem})"/>.
    /// </summary>
    protected virtual ReadOnlySpan<byte> ComputeSpan(HashSet<StackItem> visited) => [];

    /// <summary>
    /// Get the <see cref="string"/> value represented by the VM object.
    /// </summary>
    /// <returns>The <see cref="string"/> value represented by the VM object.</returns>
    public virtual string? GetString()
    {
        return GetSpan().ToStrictUtf8String();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(sbyte value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(byte value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(short value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(ushort value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(int value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(uint value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(long value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(ulong value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(BigInteger value)
    {
        return (Integer)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(bool value)
    {
        return value ? True : False;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(byte[] value)
    {
        return (ByteString)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(ReadOnlyMemory<byte> value)
    {
        return (ByteString)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StackItem(string value)
    {
        return (ByteString)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator byte(StackItem value) => (byte)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator sbyte(StackItem value) => (sbyte)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator short(StackItem value) => (short)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ushort(StackItem value) => (ushort)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(StackItem value) => (int)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator uint(StackItem value) => (uint)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator long(StackItem value) => (long)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ulong(StackItem value) => (ulong)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigInteger(StackItem value)
        => value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool(StackItem value) => value.GetBoolean();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator byte[](StackItem value) => [.. value.GetSpan()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator string(StackItem value) => value.ToString() ?? string.Empty;
}
