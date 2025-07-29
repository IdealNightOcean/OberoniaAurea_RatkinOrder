using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class OARO_ThingDefOf
{
    public static ThingDef OARO_OrderRecommendation; //推荐信

    public static ThingDef OARO_BombardSupportMaker;
    public static ThingDef RK_StrawberryBeer; //草莓精酿
    public static ThingDef Bullet_Shell_HighExplosive;

    static OARO_ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ThingDefOf));
    }
}
