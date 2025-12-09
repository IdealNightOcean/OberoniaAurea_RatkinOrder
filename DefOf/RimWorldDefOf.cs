using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_RimWorldDefOf
{
    /// <summary>
    /// 森林狼
    /// </summary>
    public static PawnKindDef Wolf_Timber;

    /// <summary>
    /// 小雕塑
    /// </summary>
    public static ThingDef SculptureSmall;

    /// <summary>
    /// 工作类型 - 修养
    /// </summary>
    public static WorkTypeDef Patient;

    static OARO_RimWorldDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_RimWorldDefOf));
    }
}
