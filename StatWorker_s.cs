using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatWorker_MeditationPoint : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);
        return pawn.CanBeKnight() && ResidentKnightsManager.IsResidentKnight(pawn);
    }

    public override bool IsDisabledFor(Thing thing)
    {
        Pawn pawn = thing as Pawn;
        return !pawn.CanBeKnight() || !ResidentKnightsManager.IsResidentKnight(pawn);
    }
}


public class StatWorker_MeditationFactor : StatWorker
{
    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !ResidentKnightsManager.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
        {
            return;
        }

        Branch branch = record.Branch;
        RatkinOrder ratkinOrder = branch.RatkinOrder;

        val += OrderHallHandler.OrderHallLevel switch
        {
            < 2 => 0f,
            < 4 => 0.05f,
            4 => 0.1f,
            5 => 0.15f,
            6 => 0.2f,
            _ => 0.25f
        };

        if (ratkinOrder.ReformationManager.HasReformation(OARO_ModDefOf.OARO_ReformationPlaceholder))
        {
            val += 0.25f;
        }

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            val += 0.25f;
        }
        if (branch.IsBranchOfType(Branch.BranchType.Honor))
        {
            val += 0.25f;
        }
        val += record.CurRank switch
        {
            ResidentKnightRecord.Rank.Elite => 0.1f,
            ResidentKnightRecord.Rank.Honor => 0.25f,
            ResidentKnightRecord.Rank.Crown => 0.5f,
            _ => 0f
        };

        val += ((pawn.GetStatValue(StatDefOf.LearningRateFactor) - 1f) * 0.1f);



    }

}

public class StatWorker_MeditationBase : StatWorker
{
    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !ResidentKnightsManager.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
        {
            return;
        }

        Branch branch = record.Branch;
        RatkinOrder ratkinOrder = branch.RatkinOrder;

        if (OrderHallHandler.OrderHallRoom is not null)
        {
            val += 5f;
        }

        val += Mathf.Min(OrderHallHandler.AcademicFurnituresCount * 2f, 30f);

    }

}