using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class Hediff_RecruitKnight : HediffWithComps
{
    private AcceptanceReport recruitAcceptanceCache;
    private int nextAcceptanceCacheTick = -1;
    public AcceptanceReport RecruitAcceptance
    {
        get
        {
            if (Find.TickManager.TicksGame > nextAcceptanceCacheTick)
            {
                nextAcceptanceCacheTick = Find.TickManager.TicksGame + 2500;
                recruitAcceptanceCache = GlobalInteractionUtility.CanRecruitKnight(pawn, pawn.MapHeld, resultOnly: false);
            }
            return recruitAcceptanceCache;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        Command_Action command_RecruitKnight = new()
        {
            defaultLabel = "OARO_Commnad_RecruitKnight".Translate(),
            defaultDesc = "OARO_CommnadDesc_RecruitKnight".Translate(),
            action = RecruitKnightDialog
        };
        if (!RecruitAcceptance)
        {
            command_RecruitKnight.Disable(RecruitAcceptance.Reason);
        }

        yield return command_RecruitKnight;
    }

    private void RecruitKnightDialog()
    {
        if (!KnightPawnsManager.Instance.TryGetKnightRecord(pawn, out var kRecord))
        {
            return;
        }
        int needRecommendation = RecommendationUtility.RecommendationNeed_RecruitmentKnight(kRecord.RatkinOrder);
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
            "OARO_RecruitKnight_Confirm".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN),
            needRecommendation.Named(KeyLibrary_FormatArgName.Count)),
            acceptAction: RecruitKnight));
    }

    private void RecruitKnight()
    {
        AcceptanceReport acceptance = GlobalInteractionUtility.CanRecruitKnight(pawn, pawn.MapHeld, resultOnly: false);
        if (!acceptance)
        {
            nextAcceptanceCacheTick = -1;
            Messages.Message("OARO_CanNotRecruitKnightWithReason".Translate(acceptance.Reason.Named(KeyLibrary_FormatArgName.Reason)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        GlobalInteractionUtility.RecruitmentKnight(pawn, pawn.MapHeld);
    }
}
