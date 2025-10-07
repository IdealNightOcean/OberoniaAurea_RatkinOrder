using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTaskStartChecker_JurisdictionDutyPerp : SquadTaskStartChecker
{
    public override AcceptanceReport CanStartNow(Squad squad, bool resultOnly = false)
    {
        if (squad.SquadStat.MemberPercentage < 0.75f)
        {
            return resultOnly ? false : "OARO_Insufficient_MemberPercentage".Translate("75%");
        }
        if (squad.SquadStat.Supply < 0.8f)
        {
            return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("80%"); ;
        }
        return true;
    }
}

public class SquadTask_JurisdictionDutyPerp : SquadTask
{

    public override int SquadRestTick(Squad squad, bool interrupt)
    {
        return (int)(base.SquadRestTick(squad, interrupt) * (squad.IsBranchSquadOfType(Branch.BranchType.Mobile) ? 0.25f : 1f));
    }

    public override void TaskStart(Squad squad)
    {
        Map map = Find.AnyPlayerHomeMap;
        if (squad.Branch.IsInAffectedRange(map.Tile))
        {
            if (squad.IsBranchSquadOfType(Branch.BranchType.Friendly))
            {
                Messages.Message("OARO_Message_JurisdictionDutyStart".Translate(squad.Name), MessageTypeDefOf.NeutralEvent, historical: false);
            }
            else
            {
                Find.LetterStack.ReceiveLetter("OARO_LetterLabel_JurisdictionDutyStart".Translate(),
                                               "OARO_Letter_JurisdictionDutyStart".Translate(squad.Name),
                                               LetterDefOf.NeutralEvent,
                                               null,
                                               squad.RatkinOrder.Faction);
            }
        }
    }
}

public class SquadTask_JurisdictionDuty : SquadTask
{
    public override void TaskEnd(Squad squad, bool interrupt)
    {
        squad.SquadStat.Supply -= 0.5f;

        if (Rand.Chance(0.1f) && Find.TickManager.TicksGame > squad.SquadManager.lastSquadBeAttackedTick + 15 * 60000)
        {
            squad.SquadStat.Supply = 0f;
            // squadStat.MemberCount -= (Rand.Range(0.1f, 0.75f) * squadStat.MemberCount);

            squad.SquadManager.lastSquadBeAttackedTick = Find.TickManager.TicksGame;
        }
    }
}

