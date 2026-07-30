using RimWorld;
using UnityEngine;
using Verse;

using OberoniaAurea.RatkinOrder.DataLibrary;using OberoniaAurea.RatkinOrder.Utility;using OberoniaAurea.RatkinOrder.UI;
namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// ThoughtWorker that modifies social opinion between knights based on virtue values.
/// Knights with higher virtue receive more respect from other knights.
/// </summary>
public class ThoughtWorker_VirtueSocialOpinion : ThoughtWorker
{
    /// <summary>
    /// Determines the current social state between observer and target based on virtue.
    /// </summary>
    /// <param name="observer">The pawn observing the target</param>
    /// <param name="target">The pawn being observed</param>
    /// <returns>ThoughtState indicating if the thought is active and at what stage</returns>
    protected override ThoughtState CurrentSocialStateInternal(Pawn observer, Pawn target)
    {
        // Null reference checks
        if (observer == null || target == null)
        {
            return ThoughtState.Inactive;
        }

        // Check if observer is in player faction
        if (!observer.Faction.IsPlayerSafe())
        {
            return ThoughtState.Inactive;
        }

        // Check if observer is a resident knight
        if (!ResidentKnightsManager.Instance.IsResidentKnight(observer))
        {
            return ThoughtState.Inactive;
        }

        // Get target's virtue value
        float virtueValue = target.GetStatValue(OARO_ModDefOf.OARO_Stat_Virtue);

        // If virtue value is 0 or negative, no opinion modifier
        if (virtueValue <= 0f)
        {
            return ThoughtState.Inactive;
        }

        // Calculate stage index based on virtue value
        // Stage 0 = 1 virtue, Stage 1 = 2 virtue, etc.
        int stageIndex = Mathf.FloorToInt(virtueValue) - 1;

        // Clamp stage index to valid range
        if (def.stages != null && def.stages.Count > 0)
        {
            stageIndex = Mathf.Clamp(stageIndex, 0, def.stages.Count - 1);
        }
        else
        {
            // If no stages defined, return inactive
            return ThoughtState.Inactive;
        }

        return ThoughtState.ActiveAtStage(stageIndex);
    }
}
