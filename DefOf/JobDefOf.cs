using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_JobDefOf
{
    /// <summary>
    /// 与...交谈
    /// </summary>
    public static JobDef OARO_Job_CommonTalkWith;
    /// <summary>
    /// 填充酒窖原料
    /// </summary>
    public static JobDef OARO_FillFermentingBarrel;
    /// <summary>
    /// 从酒窖中取出产物
    /// </summary>
    public static JobDef OARO_TakeProductOutOfFermentingBarrel;
    /// <summary>
    /// 从信息收取信件
    /// </summary>
    public static JobDef OARO_RecieveLetterFromBox;

    static OARO_JobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_JobDefOf));
    }
}
