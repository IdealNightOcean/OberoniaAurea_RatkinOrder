using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ThingDefOf
{
    public static ThingDef Ratkin;
    public static ThingDef Ratkin_Su;

    public static ThingDef OARO_OrderRecommendation; //推荐信
    public static ThingDef OARO_OrderCodePedestal; //

    public static ThingDef OARO_BombardSupportMaker;
    public static ThingDef RK_StrawberryBeer; //草莓精酿

    public static ThingDef Bullet_Shell_HighExplosive; //高爆榴弹
    public static ThingDef OARO_Turret_OrderSuperHeavyHowitzer; //超重型榴弹炮

    public static ThingDef OARO_Bullet_BannerRifle; //旗弹（弹药）

    public static ThingDef OARO_OrderLetterBox; //骑士团信箱
    public static ThingDef OARO_WineDisplayShelf; //骑士团酒架

    static OARO_ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ThingDefOf));
    }
}
