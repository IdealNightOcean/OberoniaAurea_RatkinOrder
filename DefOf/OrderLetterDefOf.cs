using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderLetterDefOf
{
    public static OrderLetterDef OARO_OfficialLetter; //公务信件
    public static OrderLetterDef OARO_OfficialLetter_SimpleAttachments; //公务信件 + 简单附件
    public static OrderLetterDef OARO_UrgentLetter; //紧急信件

    static OrderLetterDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderLetterDefOf));
    }
}
