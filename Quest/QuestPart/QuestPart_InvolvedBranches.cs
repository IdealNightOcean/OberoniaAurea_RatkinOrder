using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_InvolvedBranches : QuestPart, IBranchRelated
{
    public List<Branch> branches = [];

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            if (branches is null)
            {
                yield break;
            }
            foreach (Branch branch in branches)
            {
                if (branch?.WorldObject is not null)
                {
                    yield return branch.WorldObject;
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref branches, "branches", LookMode.Reference);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        branches = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        branches?.RemoveAll(b => b.RatkinOrder == order);
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        branches?.Remove(branch);
    }

    public static void AddInvolvedBranch(Quest quest, Branch branch)
    {
        QuestPart_InvolvedBranches questPart_InvolvedBranches = quest.PartsListForReading.OfType<QuestPart_InvolvedBranches>().FirstOrFallback(null);
        if (questPart_InvolvedBranches is null)
        {
            questPart_InvolvedBranches = new QuestPart_InvolvedBranches();
            questPart_InvolvedBranches.branches.Add(branch);
            quest.AddPart(questPart_InvolvedBranches);
        }
        else
        {
            questPart_InvolvedBranches.branches.AddDistinct(branch);
        }
    }

    public static void AddInvolvedBranch(Quest quest, IEnumerable<Branch> branches)
    {
        QuestPart_InvolvedBranches questPart_InvolvedBranches = quest.PartsListForReading.OfType<QuestPart_InvolvedBranches>().FirstOrFallback(null);
        if (questPart_InvolvedBranches is null)
        {
            questPart_InvolvedBranches = new QuestPart_InvolvedBranches();
            foreach (Branch branch in branches)
            {
                questPart_InvolvedBranches.branches.AddDistinct(branch);
            }
            quest.AddPart(questPart_InvolvedBranches);
        }
        else
        {
            foreach (Branch branch in branches)
            {
                questPart_InvolvedBranches.branches.AddDistinct(branch);
            }
        }
    }
}