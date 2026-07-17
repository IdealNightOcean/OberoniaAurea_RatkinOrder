using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ByResidentKnightBuff : HediffWithComps
{
    private HediffStage cachedStage;
    private int nextStageCacheTick = -1;
    public override HediffStage CurStage
    {
        get
        {
            if (Find.TickManager.TicksGame > nextStageCacheTick)
            {
                cachedStage = ResidentRoleManager.Instance.GetNewBuffStage();
                nextStageCacheTick = Find.TickManager.TicksGame + 60000;
            }
            return cachedStage;
        }
    }
}