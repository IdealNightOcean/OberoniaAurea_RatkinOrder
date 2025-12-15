using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemandHandler : IExposable, IOnRatkinOrderRemoved
{
    public static AcceptedBranchDemandHandler Instance { get; private set; }

    private List<AcceptedBranchDemand> records = [];
    public IReadOnlyList<AcceptedBranchDemand> Records => records;
    public int AcceptanceCount => records.Count;

    public Action<Branch, bool> PostDemandAccepted { get; set; }

    public AcceptedBranchDemandHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AcceptedBranchDemandHandler));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref records, nameof(records), LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (records.RemoveAll(r => r is null || !r.Branch.IsValid()) > 0)
            {
                Log.Error($"[OARO] Some {nameof(AcceptedBranchDemand)} were null or invalided after loading and have been removed.");
            }
        }
    }

    public void OnAcceptDemand(Branch branch, bool isCritical)
    {
        records.Add(new AcceptedBranchDemand(branch, isCritical));
        try
        {
            PostDemandAccepted?.Invoke(branch, isCritical);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"process {nameof(PostDemandAccepted)} action",
                typeName: nameof(AcceptedBranchDemandHandler),
                methodName: nameof(OnAcceptDemand),
                needStackTrace: true);
        }
    }

    public void Notify_DemandQuestClean(Quest quest)
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

    public void Notify_RatkinOrderRemoved(RatkinOrder order) => records.RemoveAll(r => r is null || !r.Branch.IsValid() || r.Branch.RatkinOrder == order);

    public void Notify_BranchDestroyed(Branch branch) => records.RemoveAll(r => r is null || !r.Branch.IsValid() || r.Branch == branch);
}