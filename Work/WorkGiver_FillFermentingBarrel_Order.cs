using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorkGiver_FillFermentingBarrel_Order : WorkGiver_FillFermentingBarrel
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn.story is not null && pawn.IsRatkin())
        {
            BackstoryDef adultHood = pawn.story.Adulthood;
            if (adultHood == OARO_ModDefOf.Ratkin_Knight || adultHood == OARO_ModDefOf.Ratkin_KnightCommander)
            {
                return base.ShouldSkip(pawn, forced);
            }
        }

        return true;
    }
}
