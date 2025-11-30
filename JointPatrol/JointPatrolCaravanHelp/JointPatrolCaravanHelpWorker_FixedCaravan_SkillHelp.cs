using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpWorker_FixedCaravan_SkillHelp : JointPatrolCaravanHelpWorker_FixedCaravan
{
    public override string HelpDescription(Branch branch)
    {
        JointPatrolCaravanHelp_SkillHelpExtension modEx_SkillHelp = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>();
        if (modEx_SkillHelp is null || modEx_SkillHelp.skillRequirement is null || modEx_SkillHelp.requireReason is null)
        {
            return base.HelpDescription(branch);
        }

        return modEx_SkillHelp.requireReason.Formatted(branch.Named(KeyLibrary_FormatArgName.BranchName),
                                                       modEx_SkillHelp.skillRequirement.skill.Named(KeyLibrary_FormatArgName.SKILL),
                                                       modEx_SkillHelp.skillRequirement.minLevel.Named(KeyLibrary_FormatArgName.Level),
                                                       Def.Named("CARAVANHELPDEF"));
    }

    public override bool Notify_CaravanArrived(Caravan caravan, Branch branch, WorldObject_InteractiveBase incidentSite)
    {
        SkillRequirement skillRequirement = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>()?.skillRequirement;
        if (skillRequirement is null)
        {
            Log.Error($"[OARO] {nameof(SkillRequirement)} from {nameof(JointPatrolCaravanHelp_SkillHelpExtension)} is null");
            return false;
        }
        else if (skillRequirement.minLevel > OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, skillRequirement.skill))
        {
            Messages.Message(
                text: "OAFrame_MissSkillSatisfiedPawn".Translate(skillRequirement.skill.label.Named(KeyLibrary_FormatArgName.SKILL),
                                                                 skillRequirement.minLevel.Named(KeyLibrary_FormatArgName.Level)),
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

    public override void InterruptWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        incidentSite.SafeDestroy();
    }

    public override string GetRewardText(Branch branch)
    {
        string rewardText = base.GetRewardText(branch);
        SkillRequirement skillRequirement = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>()?.skillRequirement;
        if (skillRequirement is not null)
        {
            rewardText += "\n\n";
            rewardText += "OARO_AllGetXp".Translate(skillRequirement.skill.Named(KeyLibrary_FormatArgName.SKILL), 6000.Named(KeyLibrary_FormatArgName.Count));
        }

        return rewardText;
    }

    public override void FinishWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        if (branch is null || fixedCaravan is null)
        {
            incidentSite.SafeDestroy();
            return;
        }
        SkillRequirement skillRequirement = Def.GetModExtension<JointPatrolCaravanHelp_SkillHelpExtension>()?.skillRequirement;
        if (skillRequirement is null)
        {
            incidentSite.SafeDestroy();
            return;
        }

        float successChance = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(fixedCaravan.PawnsListForReading, skillRequirement.skill) * 0.06f;
        if (Rand.Chance(successChance))
        {
            base.ApplyEffect(branch);
        }
        else
        {
            OrderLetterUtility.ReceiveLetter(
                label: "OARO_JointPatrolCaravanIncident_ThankLabel".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                text: "OARO_JointPatrolCaravanIncident_ThankText_Fail".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), Def.Named("CARAVANHELPDEF")),
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: branch.RatkinOrder,
                relatedBranch: branch,
                sender: branch.NameColored,
                relatedLetterType: OrderLetter.RelatedLetterType.Neutral);
        }

        incidentSite.SafeDestroy();
    }
}