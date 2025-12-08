using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderLetterDefOf
{
    /// <summary>
    /// 公务信件
    /// </summary>
    public static OrderLetterDef OARO_OfficialLetter;
    /// <summary>
    /// 公务信件 + 简单附件
    /// </summary>
    public static OrderLetterDef OARO_OfficialLetter_SimpleAttachments;
    /// <summary>
    /// 紧急信件
    /// </summary>
    public static OrderLetterDef OARO_UrgentLetter;

    static OrderLetterDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderLetterDefOf));
    }
}
