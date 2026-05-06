using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class KnightChivalryDefOf
{
    /// <summary>
    /// 勇气
    /// </summary>
    public static KnightChivalryDef OARO_Courage;
    /// <summary>
    /// 坚毅
    /// </summary>
    public static KnightChivalryDef OARO_Tenacity;
    /// <summary>
    /// 援护
    /// </summary>
    public static KnightChivalryDef OARO_Compassion;
    /// <summary>
    /// 公义
    /// </summary>
    public static KnightChivalryDef OARO_Justice;

    static KnightChivalryDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(KnightChivalryDefOf));
    }
}