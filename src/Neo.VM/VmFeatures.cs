// Copyright (C) 2015-2026 The Neo Project.
//
// VmFeatures.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM;

/// <summary>
/// Opcode behavior switches. Protocol hardforks map onto these flags in
/// <c>neo.dll</c>; this assembly does not know fork names or chain height.
/// </summary>
[Flags]
public enum VmFeatures : ulong
{
    None = 0,

    /// <summary>
    /// <see cref="OpCode.SUBSTR"/> uses a checked <c>index + count</c>
    /// so the range check cannot wrap.
    /// </summary>
    /// <remarks>
    /// Mapped from HF_Echidna in neo.dll <c>ApplicationEngine.LimitsFor</c>.
    /// Off: unchecked add (pre-Echidna). On: <c>checked</c> add (current).
    /// </remarks>
    SafeSubStr = 1 << 0,

    /// <summary>
    /// <see cref="OpCode.HASKEY"/> rejects indexes at or above
    /// <see cref="ExecutionEngineLimits.MaxItemSize"/>.
    /// </summary>
    /// <remarks>
    /// Mapped from HF_Gorgon in neo.dll <c>ApplicationEngine.LimitsFor</c>.
    /// Off: only negative indexes fault; a too-large index returns false.
    /// On: index ≥ MaxItemSize faults (current).
    /// </remarks>
    StrictContainerAccess = 1 << 1,

    /// <summary>
    /// <see cref="OpCode.SHL"/> and <see cref="OpCode.SHR"/> always pop the
    /// value operand, including when the shift is zero.
    /// </summary>
    /// <remarks>
    /// Mapped from HF_Gorgon in neo.dll <c>ApplicationEngine.LimitsFor</c>
    /// (replaces the old VulnerableSHL/VulnerableSHR jump-table overlays).
    /// Off: shift 0 returns after popping the shift, leaving the value on the
    /// stack. On: always pop the value and push the shifted result (current).
    /// For integer x, x≪0 equals x, so the numeric result matches; the extra
    /// pop still type-checks the value (e.g. Buffer faults when on).
    /// </remarks>
    BoundedShift = 1 << 2,

    /// <summary>
    /// <see cref="Types.StackItem.Equals(Types.StackItem)"/> uses content
    /// equality so <see cref="IEquatable{T}"/> / host asserts work
    /// (neo#4042 / neo#2983).
    /// </summary>
    /// <remarks>
    /// Not a protocol hardfork. <see cref="OpCode.EQUAL"/> still calls
    /// <c>Equals(other, limits)</c>, which stays reference equality for
    /// Array/Map/Buffer. Off: <c>Equals(other)</c> is reference-only and
    /// <see cref="Types.Struct.Equals(Types.StackItem)"/> throws.
    /// On: Array/Map/Buffer/Struct compare contents for the no-limits overload.
    /// </remarks>
    IEquatableContent = 1 << 3,
}

/// <summary>
/// Named combinations of <see cref="VmFeatures"/>.
/// </summary>
public static class VmFeatureSets
{
    /// <summary>
    /// All behaviors of the current VM. Standalone tests and default
    /// <see cref="ExecutionEngineLimits"/> use this set.
    /// </summary>
    public const VmFeatures Current =
        VmFeatures.SafeSubStr | VmFeatures.StrictContainerAccess | VmFeatures.BoundedShift;
}
