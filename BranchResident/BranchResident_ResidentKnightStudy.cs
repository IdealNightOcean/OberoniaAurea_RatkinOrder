using OberoniaAurea_Frame;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchResident_ResidentKnightStudy : BranchResident
{
    protected Dictionary<BranchMedalDef, int> medalsCost = [];

    public override void StartResidency(Branch branch)
    {
        base.StartResidency(branch);
        OAFrame_PawnUtility.MakePawnJoinPlayer(pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref medalsCost, nameof(medalsCost), LookMode.Def, LookMode.Value);
    }
}