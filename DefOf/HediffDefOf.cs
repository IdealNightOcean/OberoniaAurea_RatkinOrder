using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_HediffDefOf
{
    public static HediffDef OARO_Hediff_InstinctTrain; //本能训练
    public static HediffDef OARO_Hediff_WarDeepInjury; //战后创伤
    public static HediffDef OARO_Hediff_ResidentKnight; //常驻骑士（隐藏）

    public static HediffDef OARO_Hediff_BannerBullet; // 旗弹标记

    static OARO_HediffDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_HediffDefOf));
    }
}
