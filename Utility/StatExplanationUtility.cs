using OberoniaAurea_Frame;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_StatExplanationUtility
{
    public static string GetBaseValueExplanation(this OAROStatDefBase statDef, float baseValue, string format = "0.##")
    {
        return statDef.statType switch
        {
            BranchStatDef.StatType.Integer => "OARO_StatExplain_BaseValue".Translate(((int)baseValue).ToStringWithSign()).Colorize(Color.yellow),
            BranchStatDef.StatType.Float => "OARO_StatExplain_BaseValue".Translate(baseValue.ToStringWithSign(format)).Colorize(Color.yellow),
            BranchStatDef.StatType.Percent => "OARO_StatExplain_BaseValue".Translate(baseValue.ToStringPercent(format)).Colorize(Color.yellow),
            _ => KeyLibrary_Misc.ErrorTipWithColor,
        };
    }

    public static void AppendStatResultExplanation(StringBuilder modifyExplain,
                                                   OAROStatDefBase statDef,
                                                   float baseValue,
                                                   float finalValue)
    {
        modifyExplain.AppendLine();
        switch (statDef.statType)
        {
            case BranchStatDef.StatType.Integer:
                modifyExplain.AppendLine("OARO_StatExplain_ResultInt".Translate(OAFrame_TextUtility.ColoredFloatString(finalValue, format: "F0", originPoint: baseValue, reverse: statDef.reverse)));
                break;
            case BranchStatDef.StatType.Float:
                modifyExplain.AppendLine("OARO_StatExplain_Result".Translate(OAFrame_TextUtility.ColoredFloatString(finalValue, originPoint: baseValue, reverse: statDef.reverse)));
                break;
            case BranchStatDef.StatType.Percent:
                modifyExplain.AppendLine("OARO_StatExplain_Result".Translate(OAFrame_TextUtility.ColoredPercentString(finalValue, originPoint: baseValue, reverse: statDef.reverse)));
                break;
            default: break;
        }
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument OffsetNamedArgument(float offset, OAROStatDefBase statDef, string format = "0.##")
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.PercentNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true) :
            OAFrame_TextUtility.FloatNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument ColoredOffsetNamedArgument(float offset, OAROStatDefBase statDef, string format = "0.##", float originPoint = 0f)
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.ColoredPercentNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true, originPoint: originPoint, reverse: statDef.reverse) :
            OAFrame_TextUtility.ColoredFloatNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true, originPoint: originPoint, reverse: statDef.reverse);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument FactorNamedArgument(float factor, OAROStatDefBase statDef, string format = "0.##")
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.PercentNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false) :
            OAFrame_TextUtility.FloatNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument ColoredFactorNamedArgument(float factor, OAROStatDefBase statDef, string format = "0.##", float originPoint = 1f)
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.ColoredPercentNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false, originPoint: originPoint, reverse: statDef.reverse) :
            OAFrame_TextUtility.ColoredFloatNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false, originPoint: originPoint, reverse: statDef.reverse);
    }
}
