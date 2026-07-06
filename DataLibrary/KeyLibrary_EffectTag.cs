using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class KeyLibrary_EffectTag
{
    /// <summary>
    /// 教堂宣讲
    /// </summary>
    public const string Propaganda = "Propaganda";
    /// <summary>
    /// 高级教堂宣讲
    /// </summary>
    public const string AdvancedPropaganda = "AdvancedPropaganda";
    /// <summary>
    /// 强化训练
    /// </summary>
    public const string IntensiveTrain = "IntensiveTrain";
    /// <summary>
    /// 阻止远行队被抢劫
    /// </summary>
    public const string CaravanPreventLoot = "CaravanPreventLoot";

    /// <summary>
    /// 禁用分队自然恢复
    /// </summary>
    public const string BlockSquadRecover = "BlockSquadRecover";
    /// <summary>
    /// 购买骑士军械无CD
    /// </summary>
    public const string PurchaseKnightlyArmamentsNoCD = "PurchaseKnightlyArmamentsNoCD";
    /// <summary>
    /// 禁用部署支援
    /// </summary>
    public const string BlockSupport = "BlockSupport";
    /// <summary>
    /// 禁用炮击支援
    /// </summary>
    public const string BlockBombard = "BlockBombard";

    /// <summary>
    /// 分部戒严
    /// </summary>
    public const string MartialLaw = "MartialLaw";

    /// <summary>
    /// 危险预警
    /// </summary>
    public const string DangerWarning = "DangerWarning";

    /// <summary>
    /// 骑士美德：修行精英（分队研习获得的修行点与美德升级概率翻倍，必然带回骑士日记）
    /// </summary>
    public const string StudyElite = "StudyElite";

    /// <summary>
    /// 骑士美德：精英教师（自身课业授导成功率翻倍）
    /// </summary>
    public const string ProminentTeacher = "ProminentTeacher";

    /// <summary>
    /// 骑士美德：美德精英（所有大类课业花费-10%）
    /// </summary>
    public const string VirtueElite = "VirtueElite";

    /// <summary>
    /// 骑士美德：美德誓言（超越阶位上限时课业惩罚由+300%→+150%；移除美德固定消耗10000修行点）
    /// </summary>
    public const string VirtueOath = "VirtueOath";

}