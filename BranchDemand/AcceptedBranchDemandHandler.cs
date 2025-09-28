using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemandHandler : IExposable, IOnRatkinOrderRemoved, IOnBranchDestroyed
{
    private List<AcceptedBranchDemand> acceptedBranchDemands = new(2);
    public IReadOnlyList<AcceptedBranchDemand> AcceptedBranchDemands => acceptedBranchDemands;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref acceptedBranchDemands, "acceptedBranchDemands", LookMode.Deep);
        if (acceptedBranchDemands.RemoveAll(ac => ac is null || ac.Branch is null) > 0)
        {
            Log.Error($"Some AcceptedBranchDemand were null or invalided after loading and have been removed.");
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemands.RemoveAll(ac => ac.Branch is null || ac.Branch.RatkinOrder == order);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        acceptedBranchDemands.RemoveAll(ac => ac.Branch is null || ac.Branch == branch);
    }

    public bool CanAcceptDemand(Branch branch, BranchDemand demand)
    {
        if (acceptedBranchDemands.Count >= 2)
        {
            return false;
        }

        return BranchDemandUtility.CanAcceptDemand(branch, demand);
    }

    public void AcceptDemand(Branch branch, BranchDemand demand)
    {
        AcceptedBranchDemand acceptedDemand = new(branch, demand);
        demand.OnAccepted(branch);

        if (demand.CurState == BranchDemand.DemandState.Ongoing)
        {
            acceptedBranchDemands.Add(acceptedDemand);
        }
    }

    public void Notify_DemandQuestClean(Quest quest)
    {
        AcceptedBranchDemand toRmove = null;
        foreach (AcceptedBranchDemand acceptedDemand in acceptedBranchDemands)
        {
            if (quest == acceptedDemand.Demand.RelatedQuest)
            {
                toRmove = acceptedDemand;
                acceptedDemand.Branch.DemandHandler.Notify_DemandQuestClean(acceptedDemand.IsCritical);
                if (quest?.State == QuestState.EndedSuccess)
                {
                    GlobalOrderInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchDemandCompleted, 1, addIfMiss: true);
                    if (acceptedDemand.IsCritical)
                    {
                        GlobalOrderInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.CriticalDemandCompleted, 1, addIfMiss: true);
                    }
                    else
                    {
                        GlobalOrderInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.NormalDemandCompleted, 1, addIfMiss: true);
                    }
                    acceptedDemand.Branch.BranchManager.Notify_DemandQuestCompleted(acceptedDemand.IsCritical);
                }

                break;
            }
        }

        if (toRmove is not null)
        {
            acceptedBranchDemands.Remove(toRmove);
        }
    }

}