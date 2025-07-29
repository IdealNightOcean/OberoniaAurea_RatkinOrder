using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_BranchChurch : ThoughtWorker
{
    private static SimpleMapCahce<short> MapCahce = new(60000, defaultValue: 0, onlyPlayerHome: true, GetChurchCount);

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.Faction.IsPlayerSafe())
        {
            return ThoughtState.Inactive;
        }

        short count = MapCahce.GetCachedResult(p.Map);
        return count > 0 ? ThoughtState.ActiveAtStage(count - 1) : ThoughtState.Inactive;
    }


    private static short GetChurchCount(Map map)
    {
        IEnumerable<Branch> branchesInRadius = map.GetComponent<MapComponent_BranchCache>()?.branchesInRadius;
        if (branchesInRadius is null)
        {
            return 0;
        }

        short count = 0;
        foreach (Branch branch in branchesInRadius)
        {
            if (branch.EffectTags.HasActiveTag("Propaganda"))
            {
                count++;
            }
        }
        return count;
    }

    public static void ClearStaticCache()
    {
        MapCahce.Reset();
    }
}
