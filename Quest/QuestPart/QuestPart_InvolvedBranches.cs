using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_InvolvedBranches : QuestPart, IOnBranchDestroyed
{
    public List<Branch> Branches = [];

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            if (Branches is null)
            {
                yield break;
            }
            foreach (Branch branch in Branches)
            {
                if (branch?.BaseSite is not null)
                {
                    yield return branch.BaseSite;
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Branches, "Branches", LookMode.Reference);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Branches = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        Branches?.RemoveAll(b => b.RatkinOrder == order);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        Branches?.Remove(branch);
    }

    public static void AddInvolvedBranch(Quest quest, Branch branch)
    {
        QuestPart_InvolvedBranches questPart_InvolvedBranches = quest.PartsListForReading.OfType<QuestPart_InvolvedBranches>()?.FirstOrFallback(null);
        if (questPart_InvolvedBranches is null)
        {
            questPart_InvolvedBranches = new QuestPart_InvolvedBranches();
            questPart_InvolvedBranches.Branches.Add(branch);
            quest.AddPart(questPart_InvolvedBranches);
        }
        else
        {
            questPart_InvolvedBranches.Branches.AddDistinct(branch);
        }
    }

    public static void AddInvolvedBranch(Quest quest, IEnumerable<Branch> branches)
    {
        QuestPart_InvolvedBranches questPart_InvolvedBranches = quest.PartsListForReading.OfType<QuestPart_InvolvedBranches>()?.FirstOrFallback(null);
        if (questPart_InvolvedBranches is null)
        {
            questPart_InvolvedBranches = new QuestPart_InvolvedBranches();
            foreach (Branch branch in branches)
            {
                questPart_InvolvedBranches.Branches.AddDistinct(branch);
            }
            quest.AddPart(questPart_InvolvedBranches);
        }
        else
        {
            foreach (Branch branch in branches)
            {
                questPart_InvolvedBranches.Branches.AddDistinct(branch);
            }
        }
    }
}