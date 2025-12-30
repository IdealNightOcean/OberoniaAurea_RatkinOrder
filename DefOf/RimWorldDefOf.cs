using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_RimWorldDefOf
{
    /// <summary>
    /// 工作类型 - 修养
    /// </summary>
    public static WorkTypeDef Patient;

    static OARO_RimWorldDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_RimWorldDefOf));
    }
}
