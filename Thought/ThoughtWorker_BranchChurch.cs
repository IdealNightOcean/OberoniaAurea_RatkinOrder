using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_BranchChurch : ThoughtWorker
{
    private static SimpleMapCahce<int> mapCahce = new(60000, defaultValue: -1, onlyPlayerHome: true, GetChurchCount);

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.Faction.IsPlayerSafe())
        {
            return ThoughtState.Inactive;
        }

        int stage = mapCahce.GetCachedResult(p.Map);
        return stage < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
    }

    private static int GetChurchCount(Map map)
    {
        IReadOnlyList<Branch> branchesInRadius = map.GetComponent<MapComponent_RatkinOrder>()?.BranchesInRadius;
        if (branchesInRadius is null || branchesInRadius.Count == 0)
        {
            return -1;
        }

        int count = 0;
        bool hasAdvanced = false;
        for (int i = 0; i < branchesInRadius.Count; i++)
        {
            if (branchesInRadius[i].EffectTags.HasTag(KeyLibrary_EffectTag.AdvancedPropaganda))
            {
                count++;
                hasAdvanced = true;
            }
            else if (branchesInRadius[i].EffectTags.HasTag(KeyLibrary_EffectTag.Propaganda))
            {
                count++;
            }
        }

        if (count <= 0)
        {
            return -1;
        }

        return (count > 2 ? 2 : count) - 1 + (hasAdvanced ? 2 : 0);
    }

    public static void ClearStaticCache()
    {
        mapCahce.Reset();
    }
}