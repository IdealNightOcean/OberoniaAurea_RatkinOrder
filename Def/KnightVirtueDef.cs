using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士美德Def
/// </summary>
public class KnightVirtueDef : Def
{
    private static readonly Type DefaultVirtueType = typeof(KnightVirtue);

    public Type virtueClass = DefaultVirtueType;

    /// <summary>
    /// 对应骑士精神大类
    /// </summary>
    public KnightChivalryDef chivalry;

    /// <summary>
    /// 最高美德等级（默认4级）
    /// </summary>
    public int maxLevel = 4;

    /// <summary>
    /// 骑士美德类型
    /// </summary>
    public KnightVirtueType virtueType = KnightVirtueType.Normal;

    /// <summary>
    /// 对应骑士课业（可选）
    /// </summary>
    public KnightAcademicDef relatedAcademicDef;
    /// <summary>
    /// 对应课业等级（可选），达到该等级后解锁美德；-1表示不受课业等级限制
    /// </summary>
    public int unlockOnAcademicLevel = -1;

    /// <summary>
    /// 等级词条选项
    /// </summary>
    public List<KnightVirtueTraitGroups> traitGroups = [];


    public List<StatModifier> statOffsets;

    public List<StatModifier> statFactors;

    /// <summary>
    /// 根据美德属性值调整的属性Offset
    /// </summary>
    public List<StatModifierBySeverity> statOffsetsByVirtue;

    /// <summary>
    /// 根据美德属性值调整的属性Factor
    /// </summary>  
    public List<StatModifierBySeverity> statFactorsByVirtue;

    public List<KnightVirtueCompProperties> comps;

    public IReadOnlyList<KnightVirtueTraitDef> GetTraitOptionsForLevel(int level)
    {
        if (level < 1 || level > maxLevel)
        {
            Log.Error($"[OARO] Invalid virtue level: {level}. Valid range is 1 to {maxLevel}.");
            return [];
        }

        if (level > traitGroups.Count)
            return [];

        return traitGroups[level - 1].traitOptions;
    }


    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (virtueType == KnightVirtueType.Academic && relatedAcademicDef is null)
        {
            yield return "Academic virtue type requires a related academic definition.";
        }
    }
}