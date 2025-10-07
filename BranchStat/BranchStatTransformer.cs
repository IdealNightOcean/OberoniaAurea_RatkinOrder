using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatTransformer
{
    public float offset = 0f;
    public float factor = 1f;
    public float fixedOffset = 0f;

    public static BranchStatTransformer DefaultTransformer => new();


    public BranchStatTransformer() { }
    public BranchStatTransformer(float offset, float factor, float fixedOffset)
    {
        this.offset = offset;
        this.factor = factor;
        this.fixedOffset = fixedOffset;
    }

    public void MergeWith(BranchStatTransformer other)
    {
        offset += other.offset;
        factor *= other.factor;
        fixedOffset += other.fixedOffset;
    }

    public void Unmerge(BranchStatTransformer toRemove)
    {
        if (toRemove.factor == 0f)
        {
            Log.Error($"Unmerge failed: 'toRemove' has 0 factor.");
            throw new ArgumentOutOfRangeException(
                paramName: nameof(toRemove.factor),
                message: "factor of 'toRemove' cannot be 0. Unmerge operation requires a non-zero factor to avoid calculation errors.");
        }
        offset -= toRemove.offset;
        factor /= toRemove.factor;
        fixedOffset -= toRemove.fixedOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        offset = 0f;
        factor = 1f;
        fixedOffset = 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float DoTransform(BranchStatDef def, float? baseValueOverride = null)
    {
        return (baseValueOverride ?? def.baseValue + offset) * factor + fixedOffset;
    }

    public readonly float DoTransformSafe(BranchStatDef def, float? baseValueOverride = null)
    {
        float result = (baseValueOverride ?? def.baseValue + offset) * factor + fixedOffset;
        result = Mathf.Clamp(result, def.minValue, def.maxValue);

        if (def.statType == BranchStatDef.StatType.Int)
        {
            result = Mathf.RoundToInt(result);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string GetTransformExplanation()
    {
        return "OARO_StatTransformerExplanation".Translate(
            offset,
            factor.ToStringPercent("F2"),
            fixedOffset
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsValid()
    {
        return offset != 0f || fixedOffset != 0f || factor != 1f;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is BranchStatTransformer other && Equals(other);
    }

    public readonly bool Equals(BranchStatTransformer other)
    {
        return offset == other.offset && factor == other.factor && fixedOffset == other.fixedOffset;
    }

    public static bool operator ==(BranchStatTransformer left, BranchStatTransformer right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(BranchStatTransformer left, BranchStatTransformer right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        int hash = 17 * 23 + offset.GetHashCode();
        hash = hash * 23 + factor.GetHashCode();
        hash = hash * 23 + fixedOffset.GetHashCode();
        return hash;
    }
    public override readonly string ToString()
    {
        return $"offset: {offset}, factor: {factor}, fixedOffset: {fixedOffset}";
    }
}

