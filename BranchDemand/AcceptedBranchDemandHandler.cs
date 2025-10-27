using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemandHandler : IExposable, IOnRatkinOrderRemoved, IOnBranchDestroyed
{
    private static List<AcceptedBranchDemand> acceptedBranchDemands = new(2);
    public static IReadOnlyList<AcceptedBranchDemand> AcceptedBranchDemands => acceptedBranchDemands;

    public AcceptedBranchDemandHandler() => ResetStaticValue();
    public static void ResetStaticValue() => acceptedBranchDemands.Clear();

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

    public static bool CanAcceptDemand(Branch branch, BranchDemand demand)
    {
        if (acceptedBranchDemands.Count >= 2)
        {
            return false;
        }

        return BranchDemandUtility.CanAcceptDemand(branch, demand);
    }

    public static void AcceptDemand(Branch branch, BranchDemand demand)
    {
        AcceptedBranchDemand acceptedDemand = new(branch, demand);
        demand.OnAccepted(branch);

        if (demand.IsOngoing)
        {
            acceptedBranchDemands.Add(acceptedDemand);
        }
    }

    public static void Notify_DemandQuestClean(Quest quest)
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
                    GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchDemandCompleted, 1, addIfMiss: true);
                    if (acceptedDemand.IsCritical)
                    {
                        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.CriticalDemandCompleted, 1, addIfMiss: true);
                    }
                    else
                    {
                        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.NormalDemandCompleted, 1, addIfMiss: true);
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