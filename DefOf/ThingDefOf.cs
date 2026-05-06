using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ThingDefOf
{
    /// <summary>
    /// 鼠族
    /// </summary>
    public static ThingDef Ratkin;

    /// <summary>
    /// 推荐信
    /// </summary>
    public static ThingDef OARO_OrderRecommendation;
    /// <summary>
    /// 骑士团团规台
    /// </summary>
    public static ThingDef OARO_OrderCodePedestal;

    public static ThingDef OARO_BombardSupportMaker;
    /// <summary>
    /// 草莓精酿
    /// </summary>
    public static ThingDef RK_StrawberryBeer;

    /// <summary>
    /// 鼠族高爆榴弹
    /// </summary>
    public static ThingDef OARO_BulletShell_HeavyGrenade;
    /// <summary>
    /// 超重型榴弹炮
    /// </summary>
    public static ThingDef OARO_Turret_OrderSuperHeavyHowitzer;

    /// <summary>
    /// 旗弹（弹药）
    /// </summary>
    public static ThingDef OARO_Bullet_BannerRifle;

    /// <summary>
    /// 骑士团信箱
    /// </summary>
    public static ThingDef OARO_OrderLetterBox;
    /// <summary>
    /// 骑士团酒架
    /// </summary>
    public static ThingDef OARO_WineDisplayShelf;

    /// <summary>
    /// 瘟疫样本
    /// </summary>
    public static ThingDef OARO_PlagueSample;

    /// <summary>
    /// 设计规划图
    /// </summary>
    public static ThingDef OARO_DesignDrawing;

    /// <summary>
    /// 骑士日记
    /// </summary>
    public static ThingDef OARO_KnightDiary;

    static OARO_ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ThingDefOf));
    }
}
