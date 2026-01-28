using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatPart_MeditationFactor : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !pawn.Faction.IsPlayerSafe())
            return;

        if (!ResidentKnightsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
            return;

        val += OrderHallHandler.Instance.OrderHallLevel switch
        {
            < 2 => 0f,
            < 4 => 0.05f,
            4 => 0.1f,
            5 => 0.15f,
            6 => 0.2f,
            _ => 0.25f
        };

        Branch branch = record.Branch;
        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
            val += 0.25f;

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            val += 0.25f;

        if (branch.IsBranchOfType(Branch.BranchType.Honor))
            val += 0.25f;

        val += record.CurRank switch
        {
            ResidentKnightRecord.Rank.Elite => 0.1f,
            ResidentKnightRecord.Rank.Honor => 0.25f,
            ResidentKnightRecord.Rank.Crown => 0.5f,
            _ => 0f
        };

        val += ((pawn.GetStatValue(StatDefOf.LearningRateFactor) - 1f) * 0.1f);

        KnightPersonality resonatePersonality = KnightPersonalityUtility.GetResonatePersonality(record.Personality) & ResidentKnightsManager.Instance.AllHasPersonalityTypes.Value;
        val += KnightPersonalityUtility.GetContainedPersonalities(resonatePersonality).Count() * 0.1f;

        if (OrderHallHandler.Instance.KnightBuildingDefsByPersonality.TryGetValue(record.Personality, out HashSet<ThingDef> joyBuildingDefs))
            val += joyBuildingDefs.Count * 0.1f;
    }

    public override string ExplanationPart(StatRequest req)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !pawn.Faction.IsPlayerSafe())
            return null;

        if (!ResidentKnightsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
            return null;

        float stepChange;
        StringBuilder sb = new(128);

        stepChange = OrderHallHandler.Instance.OrderHallLevel switch
        {
            < 2 => 0f,
            < 4 => 0.05f,
            4 => 0.1f,
            5 => 0.15f,
            6 => 0.2f,
            _ => 0.25f
        };
        if (stepChange > 0f)
            sb.AppendLine("OARO_ChangeOffset_OrderHallLevel".Translate(stepChange.ToStringPercentSigned("0.##")));

        Branch branch = record.Branch;
        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
            sb.AppendLine("OARO_ChangeOffset_Reformation".Translate(OrderReformationDefOf.OARO_ReformationPlaceholder.label, 0.25f.ToStringPercentSigned("0.##")));

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            sb.AppendLine("OARO_ChangeOffset_BranchTypeOf".Translate($"OARO_{Branch.BranchType.Friendly}".Translate(),
                                                                     0.25f.ToStringPercentSigned("0.##").Named(KeyLibrary_FormatArgName.Offset)));
        }

        if (branch.IsBranchOfType(Branch.BranchType.Honor))
        {
            sb.AppendLine("OARO_ChangeOffset_BranchTypeOf".Translate($"OARO_{Branch.BranchType.Honor}".Translate(),
                                                                     0.25f.ToStringPercentSigned("0.##").Named(KeyLibrary_FormatArgName.Offset)));
        }

        stepChange = record.CurRank switch
        {
            ResidentKnightRecord.Rank.Elite => 0.1f,
            ResidentKnightRecord.Rank.Honor => 0.25f,
            ResidentKnightRecord.Rank.Crown => 0.5f,
            _ => 0f
        };
        if (stepChange > 0f)
            sb.AppendLine("OARO_ChangeOffset_ResidentKnightRank".Translate($"OARO_ResidentKnightRank_{record.CurRank}".Translate(), stepChange.ToStringPercentSigned("0.##")));

        stepChange = ((pawn.GetStatValue(StatDefOf.LearningRateFactor) - 1f) * 0.1f);
        sb.AppendLine(StatDefOf.LearningRateFactor.label + ": " + stepChange.ToStringPercentSigned("0.##"));

        KnightPersonality resonatePersonality = KnightPersonalityUtility.GetResonatePersonality(record.Personality) & ResidentKnightsManager.Instance.AllHasPersonalityTypes.Value;
        foreach (KnightPersonality rp in KnightPersonalityUtility.GetContainedPersonalities(resonatePersonality))
        {
            sb.AppendLine("OARO_ChangeOffset_ResonatePersonality".Translate($"OARO_KnightPersonality_{rp}".Translate(), 0.1f.ToStringPercentSigned("0.##")));
        }

        if (OrderHallHandler.Instance.KnightBuildingDefsByPersonality.TryGetValue(record.Personality, out HashSet<ThingDef> joyBuildingDefs))
        {
            foreach (ThingDef building in joyBuildingDefs)
            {
                sb.AppendLine("OARO_ChangeOffset_KnightJoyBuilding".Translate(building.label, 0.1f.ToStringPercentSigned("0.##")));
            }
        }

        return sb.ToString();
    }
}