using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTaskStartChecker_JurisdictionDutyPerp : BranchTaskStartChecker
{
    public override AcceptanceReport CanStartNow(Branch branch, bool resultOnly = false)
    {
        if (branch.Squad.MemberPercentage < 0.75f)
        {
            return resultOnly ? false : "OARO_Insufficient_MemberPercentage".Translate("75%");
        }
        if (branch.Supply < 0.8f)
        {
            return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("80%"); ;
        }
        return true;
    }
}

/// <summary>
/// 辖区执勤（准备）
/// </summary>
public class BranchTask_JurisdictionDutyPerp : BranchTask
{
    public override int BranchRestTick(Branch branch, bool interrupt)
    {
        return (int)(base.BranchRestTick(branch, interrupt) * (branch.IsBranchOfType(Branch.BranchType.Mobile) ? 0.25f : 1f));
    }

    public override void TaskStart(Branch branch)
    {
        Map map = Find.AnyPlayerHomeMap;
        if (branch.IsInAffectedRange(map.Tile))
        {
            if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            {
                Messages.Message("OARO_Message_JurisdictionDutyStart".Translate(branch.Name), MessageTypeDefOf.NeutralEvent, historical: false);
            }
            else
            {
                Find.LetterStack.ReceiveLetter("OARO_LetterLabel_JurisdictionDutyStart".Translate(),
                                               "OARO_Letter_JurisdictionDutyStart".Translate(branch.Name),
                                               LetterDefOf.NeutralEvent,
                                               null,
                                               branch.RatkinOrder.Faction);
            }
        }
    }
}

public class BranchTask_JurisdictionDuty : BranchTask
{
    public override void TaskEnd(Branch branch, bool interrupt)
    {
        branch.Supply -= 0.5f;

        if (Rand.Chance(0.1f) && !branch.RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AttackedOnTask))
        {
            branch.Supply = 0f;
            // squadStat.MemberCount -= (Rand.Range(0.1f, 0.75f) * squadStat.MemberCount);

            branch.RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.AttackedOnTask, cdTicks: 15 * 60000, shouldRemoveWhenExpired: true);
        }
    }
}

