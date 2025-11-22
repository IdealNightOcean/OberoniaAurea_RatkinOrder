using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_ResidentKnight : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.Faction.IsPlayerSafe() || !p.CanBeKnight())
        {
            return ThoughtState.Inactive;
        }

        if (ResidentKnightsManager.Instance.TryGetKnightRecord(p, out ResidentKnightRecord pRecord))
        {
            return ThoughtState.ActiveAtStage((int)pRecord.CurRank);
        }

        return ThoughtState.Inactive;
    }
}