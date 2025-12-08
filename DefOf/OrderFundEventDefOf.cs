using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderFundEventDefOf
{
    /// <summary>
    /// 每日随机
    /// </summary>
    public static OrderFundEventDef OARO_FundDailyChange;
    /// <summary>
    /// 新骑士团补助
    /// </summary>
    public static OrderFundEventDef OARO_NewOrderSubsidy;

    /// <summary>
    /// 时运 - 正
    /// </summary>
    public static OrderFundEventDef OARO_FundFortune_Positive;
    /// <summary>
    /// 时运 - 负
    /// </summary>
    public static OrderFundEventDef OARO_FundFortune_Negative;

    /// <summary>
    /// 归正 - 正
    /// </summary>
    public static OrderFundEventDef OARO_FundRestoration_Positive;
    /// <summary>
    /// 归正 - 负
    /// </summary>
    public static OrderFundEventDef OARO_FundRestoration_Negative;

    /// <summary>
    /// 赞助 - 立刻
    /// </summary>
    public static OrderFundEventDef OARO_PlayerSponsor_Immediate;
    /// <summary>
    /// 赞助 - 短期
    /// </summary>
    public static OrderFundEventDef OARO_PlayerSponsor_ShortTerm;
    /// <summary>
    /// 赞助 - 长期
    /// </summary>
    public static OrderFundEventDef OARO_PlayerSponsor_LongTerm;

    static OrderFundEventDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderFundEventDefOf));
    }
}
