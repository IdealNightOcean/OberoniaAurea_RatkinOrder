using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTaskStartChecker_JurisdictionDutyPrep : BranchTaskStartChecker
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
public class BranchTask_JurisdictionDutyPrep : BranchTask
{
    public override int BranchRestTick()
    {
        return (int)(base.BranchRestTick() * (branch.IsBranchOfType(Branch.BranchType.Mobile) ? 0.25f : 1f));
    }

    protected override void PostTaskStart()
    {
        Map map = Find.AnyPlayerHomeMap;
        if (branch.IsInAffectedRange(map.Tile))
        {
            if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            {
                Messages.Message("OARO_Message_JurisdictionDutyStart".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)), MessageTypeDefOf.NeutralEvent, historical: false);
            }
            else
            {
                Find.LetterStack.ReceiveLetter(label: "OARO_LetterLabel_JurisdictionDutyStart".Translate(),
                                               text: "OARO_Letter_JurisdictionDutyStart".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                                               textLetterDef: LetterDefOf.NeutralEvent,
                                               lookTargets: branch.BaseSite,
                                               relatedFaction: branch.RatkinOrder.Faction);
            }
        }
    }
}