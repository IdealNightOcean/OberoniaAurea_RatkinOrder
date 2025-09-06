using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_JobDefOf
{
    public static JobDef OARO_Job_CommonTalkWith; //与...交谈
    public static JobDef OARO_FillFermentingBarrel; //填充酒窖原料
    public static JobDef OARO_TakeProductOutOfFermentingBarrel; //从酒窖中取出产物
    public static JobDef OARO_RecieveLetterFromBox; // 从信息收取信件

    static OARO_JobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_JobDefOf));
    }
}
