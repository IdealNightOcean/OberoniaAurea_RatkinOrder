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

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight record))
            return;

        val += OrderStationHandler.Instance.OrderStationLevel switch
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


        val += branch.TraditionHandler.ExtraMeditationFactor.Value;

        val += record.CurRank switch
        {
            ResidentKnightRank.Elite => 0.1f,
            ResidentKnightRank.Honor => 0.25f,
            ResidentKnightRank.Crown => 0.5f,
            _ => 0f
        };

        val += ((pawn.GetStatValue(StatDefOf.LearningRateFactor) - 1f) * 0.1f);

        val += record.Chivalry.resonateChivalries.Count() * 0.1f;

        if (OrderStationHandler.BuildingHandler.KnightBuildingDefsByChivalry.TryGetValue(record.Chivalry, out HashSet<ThingDef> joyBuildingDefs))
            val += joyBuildingDefs.Count * 0.1f;
    }

    public override string ExplanationPart(StatRequest req)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !pawn.Faction.IsPlayerSafe())
            return null;

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight record))
            return null;

        float stepChange;
        StringBuilder sb = new(128);

        stepChange = OrderStationHandler.Instance.OrderStationLevel switch
        {
            < 2 => 0f,
            < 4 => 0.05f,
            4 => 0.1f,
            5 => 0.15f,
            6 => 0.2f,
            _ => 0.25f
        };
        if (stepChange > 0f)
            sb.AppendLine("OARO_ChangeOffset_OrderStationLevel".Translate(stepChange.ToStringPercentSigned("0.##")));

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

        stepChange = branch.TraditionHandler.ExtraMeditationFactor.Value;
        if (stepChange != 0f)
            sb.AppendLine("OARO_ChangeOffset_BranchTradition".Translate(stepChange.ToStringPercentSigned("0.##").Named(KeyLibrary_FormatArgName.Offset)));

        stepChange = record.CurRank switch
        {
            ResidentKnightRank.Elite => 0.1f,
            ResidentKnightRank.Honor => 0.25f,
            ResidentKnightRank.Crown => 0.5f,
            _ => 0f
        };
        if (stepChange > 0f)
            sb.AppendLine("OARO_ChangeOffset_ResidentKnightRank".Translate($"OARO_ResidentKnightRank_{record.CurRank}".Translate(), stepChange.ToStringPercentSigned("0.##")));

        stepChange = ((pawn.GetStatValue(StatDefOf.LearningRateFactor) - 1f) * 0.1f);
        sb.AppendLine(StatDefOf.LearningRateFactor.label + ": " + stepChange.ToStringPercentSigned("0.##"));

        foreach (KnightChivalryDef chivalry in record.Chivalry.resonateChivalries)
        {
            sb.AppendLine("OARO_ChangeOffset_ResonateChivalry".Translate(chivalry.LabelCap, 0.1f.ToStringPercentSigned("0.##")));
        }

        if (OrderStationHandler.BuildingHandler.KnightBuildingDefsByChivalry.TryGetValue(record.Chivalry, out HashSet<ThingDef> preferredBuildings))
        {
            foreach (ThingDef building in preferredBuildings)
            {
                sb.AppendLine("OARO_ChangeOffset_KnightJoyBuilding".Translate(building.label, 0.1f.ToStringPercentSigned("0.##")));
            }
        }

        return sb.ToString();
    }
}