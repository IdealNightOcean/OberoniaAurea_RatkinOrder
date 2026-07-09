using RimWorld;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct StatTransformer
{
    public float offset;
    public float factor;

    public StatTransformer()
    {
        offset = 0f;
        factor = 1f;
    }

    public StatTransformer(float offset, float factor)
    {
        this.offset = offset;
        this.factor = factor < 0f ? 0f : factor;
    }

    public static StatTransformer Invalid => new(0f, 1f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeWith(StatTransformer other)
    {
        offset += other.offset;
        factor *= other.factor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeOffset(float value) => offset += value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeFactor(float value) => factor *= value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unmerge(StatTransformer toRemove)
    {
        if (toRemove.factor == 0f)
        {
            Log.Error($"[OARO] 分离失败：'{nameof(toRemove)}' 的{nameof(toRemove.factor)}为0。");
            throw new ArgumentOutOfRangeException(
                paramName: nameof(toRemove.factor),
                message: $"{nameof(toRemove.factor)} of '{nameof(toRemove)}' cannot be 0. Unmerge operation requires a non-zero factor to avoid calculation errors.");
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
            Log.Error($"[OARO] 分离失败：值为0。");
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
    public readonly float DoTransform(OAROStatDefBase def, float? baseValueOverride = null)
    {
        return ((baseValueOverride ?? def.baseValue) + offset) * factor;
    }

    public readonly float DoTransformSafe(OAROStatDefBase def, float? baseValueOverride = null)
    {
        float result = (baseValueOverride ?? def.baseValue + offset) * factor;
        result = Mathf.Clamp(result, def.minValue, def.maxValue);

        if (def.statType == OAROStatDefBase.StatType.Integer)
        {
            result = Mathf.Round(result);
        }

        return result;
    }

    public readonly string TransSummary(OAROStatDefBase statDef)
    {
        if (statDef.statType == OAROStatDefBase.StatType.Percent)
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

    public readonly void AppendTransToExplanation(OAROStatDefBase statDef, StringBuilder explanation)
    {
        if (statDef.statType == OAROStatDefBase.StatType.Percent)
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
        return obj is StatTransformer other && Equals(other);
    }

    public readonly bool Equals(StatTransformer other)
    {
        return offset == other.offset && factor == other.factor;
    }

    public static bool operator ==(StatTransformer left, StatTransformer right)
    {
        return left.offset == right.offset && left.factor == right.factor;
    }
    public static bool operator !=(StatTransformer left, StatTransformer right)
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