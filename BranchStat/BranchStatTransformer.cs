using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatTransformer
{
    private const float MIN_MAGNITUDE = 1e-6f;

    private float offset = 0f;
    private float factor = 1f;
    private float factorUsed = 1f;

    private float fixedOffset = 0f;

    public float Offset
    {
        readonly get { return offset; }
        set
        {
            offset = value;
        }
    }
    public float FixedOffset
    {
        readonly get { return fixedOffset; }
        set
        {
            fixedOffset = value;
        }
    }
    public float Factor
    {
        readonly get { return factor; }
        set
        {
            factorUsed = value;
            factor = EnsureMinMagnitude(value);
        }
    }

    public BranchStatTransformer() { }
    public BranchStatTransformer(float offset, float factor, float fixedOffset)
    {
        this.offset = offset;
        Factor = factor;

        this.fixedOffset = fixedOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureFactorMinMagnitude()
    {
        factor = EnsureMinMagnitude(factor);
    }

    public static BranchStatTransformer Merge(BranchStatTransformer x, BranchStatTransformer y)
    {
        return new BranchStatTransformer(
            x.offset + y.offset,
            x.factor * y.factor,
            x.fixedOffset + y.fixedOffset
        );
    }

    public static BranchStatTransformer Unmerge(BranchStatTransformer origin, BranchStatTransformer divided)
    {
        return new BranchStatTransformer(
            origin.offset - divided.offset,
            origin.factor / divided.factor,
            origin.fixedOffset - divided.fixedOffset
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RoundToInt()
    {
        offset = Mathf.RoundToInt(offset);
        fixedOffset = Mathf.RoundToInt(fixedOffset);
        Factor = Mathf.RoundToInt(factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        offset = 0f;
        factor = 1f;
        factorUsed = 1f;
        fixedOffset = 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float DoTransform(BranchStatDef def, float? baseValueOverride = null)
    {
        return (baseValueOverride ?? def.baseValue + offset) * factorUsed + fixedOffset;
    }

    public readonly float DoTransformSafe(BranchStatDef def, float? baseValueOverride = null)
    {
        float result = (baseValueOverride ?? def.baseValue + offset) * factorUsed + fixedOffset;
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
            factor.ToStringPercent(),
            fixedOffset
        );
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(BranchStatTransformer transformer)
    {
        return transformer.fixedOffset != 0f ||
               (transformer.offset == 0f
                   ? Mathf.Abs(1f - transformer.factor) > MIN_MAGNITUDE
                   : Mathf.Abs(transformer.factor) > MIN_MAGNITUDE);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float EnsureMinMagnitude(float value)
    {
        if (value == 0f)
        {
            return MIN_MAGNITUDE;
        }

        return value > 0f ? Mathf.Max(value, MIN_MAGNITUDE) : Mathf.Min(value, -MIN_MAGNITUDE);
    }

    public override readonly bool Equals(object obj)
    {
        return obj is BranchStatTransformer other && Equals(other);
    }

    public readonly bool Equals(BranchStatTransformer other)
    {
        return offset == other.offset && factor == other.factor && factorUsed == other.factorUsed && fixedOffset == other.factorUsed;
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
        hash = hash * 23 + factorUsed.GetHashCode();
        hash = hash * 23 + fixedOffset.GetHashCode();
        return hash;
    }
    public override readonly string ToString()
    {
        return $"offset: {offset}, factor: {factor}, factorUsed: {factorUsed}, fixedOffset: {fixedOffset}";
    }
}

