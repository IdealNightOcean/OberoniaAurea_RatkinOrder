using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpWorker_FixedCaravan_SkillHelp : JointPatrolCaravanHelpWorker_FixedCaravan
{
    public override string RequestHelpReason(Branch branch)
    {
        JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();
        if (modEx_SkillHelp is null || modEx_SkillHelp.requireSkill is null || modEx_SkillHelp.requestHelpReason is null)
        {
            return base.RequestHelpReason(branch);
        }

        return modEx_SkillHelp.requestHelpReason.Formatted(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                                                       modEx_SkillHelp.requireSkill.Named(KeyLibrary_FormatArgName.SKILL),
                                                       modEx_SkillHelp.minLevel.Named(KeyLibrary_FormatArgName.Level),
                                                       Def.Named(OARO_KeyLibrary_FormatArgName.CARAVANHELPDEF));
    }

    public override bool Notify_CaravanArrived(Caravan caravan, Branch branch, WorldObject_InteractiveBase incidentSite)
    {
        JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();

        if (modEx_SkillHelp?.requireSkill is null)
        {
            Log.Error($"[OARO] {nameof(JointPatrolCaravanHelp_SkillHelpExtension)} 中的 {nameof(SkillRequirement)} 为null");
            return false;
        }
        else if (modEx_SkillHelp.minLevel > OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, modEx_SkillHelp.requireSkill))
        {
            Messages.Message(
                text: "OAFrame_MissSkillSatisfiedPawn".Translate(modEx_SkillHelp.requireSkill.Named(KeyLibrary_FormatArgName.SKILL),
                                                                 modEx_SkillHelp.minLevel.Named(KeyLibrary_FormatArgName.Level)),
                def: MessageTypeDefOf.RejectInput,
                historical: false);
            return false;
        }

        return base.Notify_CaravanArrived(caravan, branch, incidentSite);
    }

    public override bool PostStartWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();
        if (modEx_SkillHelp is not null)
        {
            incidentSite.SetTicksRemaining(modEx_SkillHelp.ticksNeeded);
        }
        else
        {
            incidentSite.SetTicksRemaining(30000);
        }
        return true;
    }

    protected bool CanApply(Branch branch, FixedCaravan fixedCaravan)
    {
        if (!branch.IsValid() || fixedCaravan is null)
        {
            return false;
        }
        if (Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>()?.requireSkill is null)
        {
            return false;
        }
        return true;
    }

    public override void InterruptWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        try
        {
            if (!CanApply(branch, fixedCaravan))
            {
                incidentSite.SafeDestroy();
                return;
            }

            HelpFailed(fixedCaravan, branch, incidentSite);
            Messages.Message("OARO_CaravanHelpInterrupted".Translate(incidentSite.Name.Named("CaravanHelpSiteName")), incidentSite, MessageTypeDefOf.NeutralEvent);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "中断帮助工作",
                typeName: nameof(JointPatrolCaravanHelpWorker_FixedCaravan_SkillHelp),
                methodName: nameof(InterruptWork),
                needStackTrace: true);
        }

        incidentSite.SafeDestroy();
    }

    public override void FinishWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        try
        {
            if (!CanApply(branch, fixedCaravan))
            {
                incidentSite.SafeDestroy();
                return;
            }

            JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();
            float successChance = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(fixedCaravan.PawnsListForReading, modEx_SkillHelp.requireSkill) * 0.06f;
            if (Rand.Chance(successChance))
            {
                HelpSucceed(fixedCaravan, branch, incidentSite);
            }
            else
            {
                HelpFailed(fixedCaravan, branch, incidentSite);
            }
            Messages.Message("OARO_CaravanHelpCompleted".Translate(incidentSite.Name.Named("CaravanHelpSiteName")), incidentSite, MessageTypeDefOf.NeutralEvent);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "完成帮助工作",
                typeName: nameof(JointPatrolCaravanHelpWorker_FixedCaravan_SkillHelp),
                methodName: nameof(FinishWork),
                needStackTrace: true);
        }

        incidentSite.SafeDestroy();
    }

    private void HelpSucceed(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();
        foreach (Pawn p in fixedCaravan.PawnsListForReading)
        {
            p.skills?.Learn(modEx_SkillHelp.requireSkill, 6000f);
        }
        extraRewardText.AppendLine();
        extraRewardText.AppendLine("OAFrame_AllCarvanMemberGetXP".Translate(modEx_SkillHelp.requireSkill.Named(KeyLibrary_FormatArgName.SKILL), 6000.Named(KeyLibrary_FormatArgName.Count)));
        base.ApplyEffect(branch);
        incidentSite.SendWorkResolvedSignal();
        incidentSite.SafeDestroy();
    }

    private void HelpFailed(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();
        foreach (Pawn p in fixedCaravan.PawnsListForReading)
        {
            p.skills?.Learn(modEx_SkillHelp.requireSkill, 6000f);
        }

        TaggedString failedThankText;
        if (String.IsNullOrEmpty(modEx_SkillHelp.failedThankText))
        {
            failedThankText = "OARO_JointPatrolCaravanIncident_ThankText_Fail".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), Def.Named(OARO_KeyLibrary_FormatArgName.CARAVANHELPDEF));
        }
        else
        {
            failedThankText = modEx_SkillHelp.failedThankText.Formatted(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), Def.Named(OARO_KeyLibrary_FormatArgName.CARAVANHELPDEF));
        }
        failedThankText += "\n\n" + "OAFrame_AllCarvanMemberGetXP".Translate(modEx_SkillHelp.requireSkill.Named(KeyLibrary_FormatArgName.SKILL), 6000.Named(KeyLibrary_FormatArgName.Count));

        OrderLetterUtility.ReceiveLetter(
            label: "OARO_JointPatrolCaravanIncident_ThankLabel".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
            text: failedThankText,
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Neutral);

        incidentSite.SafeDestroy();
    }
}