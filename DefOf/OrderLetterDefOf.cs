using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderLetterDefOf
{
    public static OrderLetterDef OARO_OfficialNeutralEvent; //公务中性事件
    public static OrderLetterDef OARO_OfficialPositiveEvent; //公务正面事件
    public static OrderLetterDef OARO_OfficialPositive_SimpleAttachments; //公务正面事件 + 简单附件

    static OrderLetterDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderLetterDefOf));
    }
}
