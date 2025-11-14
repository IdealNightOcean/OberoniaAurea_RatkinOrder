using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemandHandler : IExposable, IOnRatkinOrderRemoved
{
    private static List<AcceptedBranchDemand> records = [];
    public static IReadOnlyList<AcceptedBranchDemand> Records => records;
    public static int AcceptanceCount => records.Count;
    public static void ClearStaticCache() => records.Clear();
    public AcceptedBranchDemandHandler() => ResetStaticValue();

    public static void ResetStaticValue()
    {
        records.Clear();
    }
    public void ExposeData()
    {
        Scribe_Collections.Look(ref records, "records", LookMode.Deep);
        if (records.RemoveAll(r => r is null || r.Branch is null) > 0)
        {
            Log.Error($"Some {nameof(AcceptedBranchDemand)} were null or invalided after loading and have been removed.");
        }
    }

    public static void OnAcceptDemand(Branch branch, bool isCritical)
    {
        records.Add(new AcceptedBranchDemand(branch, isCritical));
    }

    public static void Notify_DemandQuestClean(Quest quest)
    {
        AcceptedBranchDemand toRmove = null;
        foreach (AcceptedBranchDemand acceptedDemand in records)
        {
            if (quest == acceptedDemand.Demand.RelatedQuest)
            {
                toRmove = acceptedDemand;
                toRmove.Notify_DemandQuestClean(quest.State);
                break;
            }
        }

        if (toRmove is not null)
        {
            records.Remove(toRmove);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order) => records.RemoveAll(r => r is null || r.Branch is null || r.Branch.RatkinOrder == order);

    public void Notify_BranchDestroyed(Branch branch) => records.RemoveAll(r => r is null || r.Branch is null || r.Branch == branch);
}