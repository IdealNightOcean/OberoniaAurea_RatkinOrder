using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_HediffDefOf
{
    /// <summary>
    /// 本能训练
    /// </summary>
    public static HediffDef OARO_Hediff_IntensiveTrain;
    /// <summary>
    /// 战后创伤
    /// </summary>
    public static HediffDef OARO_Hediff_WarDeepInjury;

    /// <summary>
    /// 旗弹标记
    /// </summary>
    public static HediffDef OARO_Hediff_BannerBullet;

    /// <summary>
    /// 印记Buff
    /// </summary>
    public static HediffDef OARO_Hediff_BranchMedal;
    /// <summary>
    /// 影猎骑士增伤
    /// </summary>
    public static HediffDef OARO_Hediff_HonorHunting_Debuff;
    /// <summary>
    /// 圣骑士鼓舞
    /// </summary>
    public static HediffDef OARO_Hediff_HonorPaladin_Stimulate;

    /// <summary>
    /// 常驻骑士的Buff
    /// </summary>
    public static HediffDef OARO_Hediff_ByResidentKnightBuff;

    /// <summary>
    /// 骑士激励
    /// </summary>
    public static HediffDef OARO_Hediff_KnightlyTalk;

    /// <summary>
    /// 落难骑士
    /// </summary>
    public static HediffDef OARO_Hediff_InDistressKnight;

    /// <summary>
    /// 狼灾 - 灾狼
    /// </summary>
    public static HediffDef OARO_Hediff_WolfDisaster;

    /// <summary>
    /// 叛乱贵族 - 群情激愤下的叛乱
    /// </summary>
    public static HediffDef OARO_Hediff_NobilityTerritoryInHeat;
    /// <summary>
    /// 叛乱贵族 - 贵族被突袭
    /// </summary>
    public static HediffDef OARO_Hediff_NobilityTerritoryPounce;
    /// <summary>
    /// 叛乱贵族 - 玩家被突袭
    /// </summary>
    public static HediffDef OARO_Hediff_NobilityTerritoryPouncePlayer;

    static OARO_HediffDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_HediffDefOf));
    }
}
