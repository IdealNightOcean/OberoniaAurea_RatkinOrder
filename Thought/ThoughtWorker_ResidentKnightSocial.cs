using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_KnightPersonalitySocial : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
    {
        if (!p.Faction.IsPlayerSafe() || otherPawn.Faction.IsPlayerSafe())
        {
            return ThoughtState.Inactive;
        }

        if (KnightPawnsManager.Instance.TryGetKnightRecord(p, out KnightRecord pRecord)
          && KnightPawnsManager.Instance.TryGetKnightRecord(otherPawn, out KnightRecord otherRecord))
        {
            if (KnightPersonalityExtension.IsResonatePersonality(pRecord.Personality, otherRecord.Personality))
            {
                return ThoughtState.ActiveAtStage(1);
            }
            else
            {
                return ThoughtState.ActiveAtStage(0);
            }
        }

        return ThoughtState.Inactive;
    }
}