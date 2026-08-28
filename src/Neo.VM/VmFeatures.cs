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
    SafeSubStr = 1 << 0,

    /// <summary>
    /// <see cref="OpCode.HASKEY"/> rejects indexes at or above
    /// <see cref="ExecutionEngineLimits.MaxItemSize"/>.
    /// </summary>
    StrictContainerAccess = 1 << 1,

    /// <summary>
    /// <see cref="OpCode.SHL"/> and <see cref="OpCode.SHR"/> always pop the
    /// value operand, including when the shift is zero.
    /// </summary>
    BoundedShift = 1 << 2,
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
