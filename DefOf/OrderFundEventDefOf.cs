using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderFundEventDefOf
{
    public static OrderFundEventDef OARO_FundDailyChange; // 每日随机
    public static OrderFundEventDef OARO_NewOrderSubsidy; // 新骑士团

    public static OrderFundEventDef OARO_FundFortune_Positive;
    public static OrderFundEventDef OARO_FundFortune_Negative;

    public static OrderFundEventDef OARO_FundRestoration_Positive;
    public static OrderFundEventDef OARO_FundRestoration_Negative;

    public static OrderFundEventDef OARO_PlayerSponsor_Immediate;
    public static OrderFundEventDef OARO_PlayerSponsor_ShortTerm;
    public static OrderFundEventDef OARO_PlayerSponsor_LongTerm;

    static OrderFundEventDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderFundEventDefOf));
    }
}
