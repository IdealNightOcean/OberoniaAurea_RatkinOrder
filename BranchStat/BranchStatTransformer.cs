using RimWorld;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatTransformer
{
    public float offset = 0f;
    public float factor = 1f;

    public BranchStatTransformer() { }
    public BranchStatTransformer(float offset, float factor, float fixedOffset)
    {
        this.offset = offset;
        this.factor = factor < 0f ? 0f : factor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeWith(BranchStatTransformer other)
    {
        offset += other.offset;
        factor *= other.factor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeOffset(float value) => offset += value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeFactor(float value) => factor *= value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unmerge(BranchStatTransformer toRemove)
    {
        if (toRemove.factor == 0f)
        {
            Log.Error($"[OARO] Unmerge failed: 'toRemove' has 0 factor.");
            throw new ArgumentOutOfRangeException(
                paramName: nameof(toRemove.factor),
                message: "factor of 'toRemove' cannot be 0. Unmerge operation requires a non-zero factor to avoid calculation errors.");
        }
        offset -= toRemove.offset;
        factor /= toRemove.factor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnmergeOffset(float value) => offset -= value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnmergeFactor(float value)
    {
        if (value == 0f)
        {
            Log.Error($"[OARO] Unmerge failed: value is 0.");
            throw new ArgumentOutOfRangeException(
                paramName: nameof(value),
                message: "Unmerge operation requires a non-zero factor to avoid calculation errors.");
        }
        factor /= value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        offset = 0f;
        factor = 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float DoTransform(BranchStatDef def, float? baseValueOverride = null)
    {
        return (baseValueOverride ?? def.baseValue + offset) * factor;
    }

    public readonly float DoTransformSafe(BranchStatDef def, float? baseValueOverride = null)
    {
        float result = (baseValueOverride ?? def.baseValue + offset) * factor;
        result = Mathf.Clamp(result, def.minValue, def.maxValue);

        if (def.statType == BranchStatDef.StatType.Int)
        {
            result = Mathf.Round(result);
        }

        return result;
    }

    public readonly string TransSummary(BranchStatDef statDef)
    {
        if (statDef.statType == BranchStatDef.StatType.Percent)
        {
            return statDef.label
                 + $": {offset.ToStringPercentSigned("0.##").Colorize((offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}"
                 + $" / ×{factor.ToStringPercent("0.##").Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}";
        }
        else
        {
            return statDef.label
                 + $": {offset.ToStringWithSign("0.##").Colorize((offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}"
                 + $" / ×{factor.ToString("0.##").Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}";
        }
    }

    public readonly void AppendTransExplanation(BranchStatDef statDef, StringBuilder explanation)
    {
        if (statDef.statType == BranchStatDef.StatType.Percent)
        {
            if (offset != 0f)
            {
                explanation.Append("    ");
                explanation.AppendLine("OARO_StatExplain_Offset".Translate(offset.ToStringPercentSigned("0.##"))
                                                                .Colorize((offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
            if (factor != 1f)
            {
                explanation.Append("    ");
                explanation.AppendLine("OARO_StatExplain_Factor".Translate(factor.ToStringPercentSigned("0.##"))
                                                                .Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
        }
        else
        {
            if (offset != 0f)
            {
                explanation.Append("    ");
                explanation.AppendLine("OARO_StatExplain_Offset".Translate(offset.ToStringWithSign("0.##"))
                                                                .Colorize((offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
            if (factor != 1f)
            {
                explanation.Append("    ");
                explanation.AppendLine("OARO_StatExplain_Factor".Translate(factor.ToString("0.##"))
                                                                .Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsValid() => offset != 0f || factor != 1f;

    public override readonly bool Equals(object obj)
    {
        return obj is BranchStatTransformer other && Equals(other);
    }

    public readonly bool Equals(BranchStatTransformer other)
    {
        return offset == other.offset && factor == other.factor;
    }

    public static bool operator ==(BranchStatTransformer left, BranchStatTransformer right)
    {
        return left.offset == right.offset && left.factor == right.factor;
    }
    public static bool operator !=(BranchStatTransformer left, BranchStatTransformer right)
    {
        return left.offset != right.offset || left.factor != right.factor;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17 * 23 + offset.GetHashCode();
            hash = hash * 23 + factor.GetHashCode();
            return hash;
        }
    }

    public override readonly string ToString()
    {
        return $"offset: {offset}, factor: {factor}";
    }
}