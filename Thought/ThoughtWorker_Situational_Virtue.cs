using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_Situational_Virtue : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
    {
        if (!other.RaceProps.Humanlike || !KnightPawnsManager.Instance.IsKnight(pawn) || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            return ThoughtState.Inactive;


        float statValue = other.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue);
        if (statValue >= 1f)
        {
            return ThoughtState.ActiveDefault;
        }

        return ThoughtState.Inactive;
    }
}
