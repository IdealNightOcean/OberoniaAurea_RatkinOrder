using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
            if (records.RemoveAll(r => r is null || !r.IsValid) > 0)
            {
                Log.Error($"[OARO] 部分 {nameof(AcceptedBranchDemand)} 在加载后为null或无效，已被移除。");
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
                errorDesc: $"处理 {nameof(PostDemandAccepted)}",
                typeName: nameof(AcceptedBranchDemandHandler),
                methodName: nameof(OnAcceptDemand),
                needStackTrace: true);
        }
    }

    public void Notify_DemandQuestPreCleanup(Quest quest)
    {
        AcceptedBranchDemand acceptedDemand = null;
        foreach (AcceptedBranchDemand demand in records)
        {
            if (quest == demand.Demand.RelatedQuest)
            {
                acceptedDemand = demand;
                break;
            }
        }
        if (acceptedDemand is null)
        {
            return;
        }

        Branch branch = acceptedDemand.Branch;
        BranchDemand relatedDemand = acceptedDemand.Demand;

        records.Remove(acceptedDemand);
        branch.DemandHandler.RemoveDemand(acceptedDemand.IsCritical);
        if (quest.State != QuestState.EndedSuccess)
            return;

        branch.BranchManager.Notify_DemandQuestCompleted(acceptedDemand.IsCritical);

        KnightsVirtuesReward(relatedDemand, quest);

        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchDemandCompleted, 1, addIfMiss: true);
        if (acceptedDemand.IsCritical)
        {
            GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.CriticalDemandCompleted, 1, addIfMiss: true);
        }
        else
        {
            GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.NormalDemandCompleted, 1, addIfMiss: true);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order) => records.RemoveAll(r => r is null || !r.Branch.IsValid() || r.Branch.RatkinOrder == order);

    public void Notify_BranchDestroyed(Branch branch) => records.RemoveAll(r => r is null || !r.Branch.IsValid() || r.Branch == branch);


    private static void KnightsVirtuesReward(BranchDemand demand, Quest quest)
    {
        if (demand.DemandTypeValue != BranchDemand.DemandType.Critical)
            return;


        if (!OARO_QuestUtility.TryGetCliquesManager(quest, addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager))
            return;

        int knight = Mathf.FloorToInt(cliquesManager.TotalPotency.NewestValue / 0.6f);
        if (knight <= 0)
            return;

        IEnumerable<ResidentKnight> targetKnights = ResidentPawnsManager.Instance.ResidentKnights.Where(r => r.KnightVirtueHandler.HasUpgradableVirtue)
                                                                                                 .TakeRandomElements(knight);

        string reason = "OARO_VirtueUpgradeReason_BranchDemandCompleted".Translate(quest.name.Named(KeyLibrary_FormatArgName.QuestName));
        foreach (ResidentKnight residentKnight in targetKnights)
        {
            KnightVirtueDef targetVirtue = KnightVirtueUtility.GetRandomUpgradableVirtue(residentKnight);

            residentKnight.KnightVirtueHandler.UpgradeVirtue(targetVirtue, upgrade: 1, reason: reason);
        }
    }

}