using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士美德Def
/// </summary>
public class KnightVirtueDef : Def
{
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
    /// 对应课业等级（可选），达到该等级后解锁美德
    /// </summary>
    public int unlockOnAcademicLevel = -1;

    /// <summary>
    /// 1级基础词条
    /// </summary>
    public KnightVirtueTraitDef baseTrait;

    /// <summary>
    /// 2级词条选项（3选1）
    /// </summary>
    public List<KnightVirtueTraitDef> level2TraitOptions = [];
    /// <summary>
    /// 3级词条选项（3选1）
    /// </summary>
    public List<KnightVirtueTraitDef> level3TraitOptions = [];
    /// <summary>
    /// 4级词条选项（3选1）
    /// </summary>
    public List<KnightVirtueTraitDef> level4TraitOptions = [];

    public IEnumerable<KnightVirtueTraitDef> GetTraitOptionsForLevel(int level)
    {
        return level switch
        {
            1 => baseTrait != null ? [baseTrait] : [],
            2 => level2TraitOptions,
            3 => level3TraitOptions,
            4 => level4TraitOptions,
            _ => []
        };
    }

    public int TraitOptionsCountForLevel(int level)
    {
        return level switch
        {
            1 => baseTrait != null ? 1 : 0,
            2 => level2TraitOptions.Count,
            3 => level3TraitOptions.Count,
            4 => level4TraitOptions.Count,
            _ => 0
        };
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
        if (baseTrait is null)
        {
            yield return "KnightVirtueDef requires a baseTrait for level 1.";
        }
        if (level2TraitOptions.Count != 3)
        {
            yield return $"level2TraitOptions should have exactly 3 options, but has {level2TraitOptions.Count}.";
        }
        if (level3TraitOptions.Count != 3)
        {
            yield return $"level3TraitOptions should have exactly 3 options, but has {level3TraitOptions.Count}.";
        }
        if (level4TraitOptions.Count != 3)
        {
            yield return $"level4TraitOptions should have exactly 3 options, but has {level4TraitOptions.Count}.";
        }
    }

}
