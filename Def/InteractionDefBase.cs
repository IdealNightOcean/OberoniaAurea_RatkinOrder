using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class InteractionDefBase : Def
{
    /// <summary>
    /// 是否有冷却
    /// </summary>
    public bool hasCoolDown;

    /// <summary>
    /// 是否使用默认冷却
    /// </summary>
    public bool useDefaultCD;

    /// <summary>
    /// 默认冷却时间
    /// </summary>
    public int defaultCdDays = -1;

    /// <summary>
    /// 最低骑士团关系
    /// </summary>
    public EsteemHandler.RelationshipKind floorRelationship = EsteemHandler.RelationshipKind.Stranger;

    /// <summary>
    /// 最低骑士团认可度
    /// </summary>
    public int floorEsteem = -1;

    /// <summary>
    /// 需求推荐信数量
    /// </summary>
    public int needRecommendation = -1;

    /// <summary>
    /// 需求白银数量
    /// </summary>
    public int needSilver = -1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (!hasCoolDown && useDefaultCD)
        {
            useDefaultCD = false;
            yield return $"'{nameof(useDefaultCD)}' disabled because '{nameof(hasCoolDown)}' is false.";
        }
        if (useDefaultCD && defaultCdDays < 0)
        {
            defaultCdDays = 0;
            yield return $"'{nameof(defaultCdDays)}' was negative. Set to 0.";
        }
    }
}