using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestHandler : IExposable
{
    private readonly Dictionary<MercyQuestDef, LinkedListNode<MercyQuestDef>> lruDict = [];
    private readonly LinkedList<MercyQuestDef> lruLinkedList = [];
    private List<MercyQuestDef> tempLRUListForSave;

    private static int LRUCapacity => DefDatabase<MercyQuestDef>.DefCount / 4;

    public static MercyQuestHandler Instance { get; private set; }

    private float lastMercyQuestTriggerChange;
    private float mercyQuestBaseChance;

    public MercyQuestHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(MercyQuestHandler));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Values.Look(ref mercyQuestBaseChance, nameof(mercyQuestBaseChance), 0f);
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            tempLRUListForSave = lruLinkedList.ToList();
        }

        Scribe_Collections.Look(ref tempLRUListForSave, nameof(lruLinkedList), LookMode.Def);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            foreach (MercyQuestDef mercyQuestDef in tempLRUListForSave)
            {
                if (mercyQuestDef is not null)
                {
                    LinkedListNode<MercyQuestDef> linkedNoed = new(mercyQuestDef);
                    lruDict[mercyQuestDef] = linkedNoed;
                    lruLinkedList.AddLast(linkedNoed);
                }
            }
        }
        if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tempLRUListForSave = null;
        }
    }

    public void Notify_MercyQuestSucceed(Quest quest, MercyQuestDef mercyQuestDef)
    {
        if (quest is null)
            return;

        ResidentKnightsManager.Instance.AllResidentKnightsGainMeditation(200f, directly: false);

        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.MercyQuestSucceed, 1, addIfMiss: true);
        float letterChance = 0.2f;

        if (ResidentKnightsManager.Instance.TryGetKnightOfRole(OARO_ModDefOf.OARO_Orderly, out ResidentKnightRecord record))
        {
            letterChance += (OARO_ModDefOf.OARO_Orderly.RoleWorker as ResidentKnightRoleWorker_Orderly).ExtraMercyQuestLetterChance(record.Knight);
        }
        if (Rand.Chance(letterChance))
        {
            Branch branch = RatkinOrderManager.Instance.AllRatkinOrders.RandomElementWithFallback(null)?.BranchManager.AllBranches.RandomElementWithFallback(null);
            if (!branch.IsValid())
                return;

            if (RatkinOrderSettings.EnableAIContent)
            {
                AIInteractionUtility.SendMercyQuestAdmireLetter(branch, quest, mercyQuestDef);
            }
            else
            {
                OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
                    label: "OARO_LetterLabel_MercyQuestAdmire".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                    text: "OARO_Letter_MercyQuestAdmire".Translate(
                              branch.NameColored.Named(KeyLibrary_FormatArgName.BranchName),
                              branch.RatkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName),
                              quest.name.Named("QuestName")),
                    def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
                    relatedOrder: branch.RatkinOrder,
                    relatedBranch: branch,
                    sender: branch.NameColored,
                    relatedLetterType: OrderLetter.RelatedLetterType.Positive);
                OrderRecommendation orderRecommendation = RecommendationUtility.MakeRecommendationForPlayer(count: 1);
                orderLetter.AddAttachment(orderRecommendation);
                OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays: Rand.Range(1, 5));
            }
        }
    }

    public void PeriodicTriggerMercyQuest()
    {
        if (!TryPeriodicTriggerMercyQuest())
        {
            mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance + 0.05f, 0.8f);
        }
    }

    private bool TryPeriodicTriggerMercyQuest()
    {
        if (OrderHallHandler.Instance.OrderHallRoom is null || GlobalInteractionManager.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.MercyQuestTryTriggered))
            return false;

        GlobalInteractionManager.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.MercyQuestTryTriggered, cdTicks: 3 * 60000, removeWhenExpired: true);

        if (Rand.Chance(1f - GetMercyQuestChance()))
            return false;

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is null)
            return false;

        foreach (MercyQuestDef mercyQuestDef in GetPotentialMercies())
        {
            if (mercyQuestDef is not null && TryTriggerMercyQuest(mercyQuestDef, map))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryTriggerMercyQuest(MercyQuestDef mercyQuestDef, Map map)
    {
        Slate slate = new();
        slate.Set("map", map);
        if (!mercyQuestDef.TrySetQuestSlateValue(slate))
        {
            return false;
        }
        // 善行任务的派系Test时未生成，只好强制触发了
        if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(
            quest: out Quest quest,
            scriptDef: mercyQuestDef.needPreQuest ? mercyQuestDef.preQuestDef : mercyQuestDef.mainQuestDef,
            slate: slate,
            forced: true,
            target: map))
        {
            PostMercyQuestTriggered(quest, mercyQuestDef);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Notify_MercyQuestInterrupted()
    {
        mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance, lastMercyQuestTriggerChange / 2f);
    }

    private IEnumerable<MercyQuestDef> GetPotentialMercies()
    {
        List<MercyQuestDef> allMercyQuestDefs = DefDatabase<MercyQuestDef>.AllDefsListForReading;
        int potentialCount = Mathf.Clamp(allMercyQuestDefs.Count / 3, 5, 10);
        PriorityQueue<MercyQuestDef, double> reservoir = new(potentialCount);
        System.Random rng = new();

        foreach (MercyQuestDef mercyQuestDef in allMercyQuestDefs)
        {
            if (mercyQuestDef.selectWeight <= 0f || lruDict.ContainsKey(mercyQuestDef))
            {
                continue;
            }

            double u = 1 - rng.NextDouble();
            double key = Math.Log(u) / mercyQuestDef.selectWeight;
            if (reservoir.Count < potentialCount)
            {
                reservoir.Enqueue(mercyQuestDef, key);
            }
            else if (reservoir.TryPeek(out _, out double topKey))
            {
                if (key > topKey)
                {
                    reservoir.Dequeue();
                    reservoir.Enqueue(mercyQuestDef, key);
                }
            }
        }

        while (reservoir.Count > 0)
        {
            yield return reservoir.Dequeue();
        }
    }

    private void PostMercyQuestTriggered(Quest quest, MercyQuestDef mercyQuestDef)
    {
        lastMercyQuestTriggerChange = GetMercyQuestChance();
        mercyQuestBaseChance = 0f;

        if (lruDict.TryGetValue(mercyQuestDef, out LinkedListNode<MercyQuestDef> curNode))
        {
            lruLinkedList.Remove(curNode);
            lruLinkedList.AddFirst(curNode);
            return;
        }

        if (lruDict.Count >= LRUCapacity)
        {
            LinkedListNode<MercyQuestDef> lastNode = lruLinkedList.Last;
            if (lastNode is not null)
            {
                lruDict.Remove(lastNode.Value);
                lruLinkedList.Remove(lastNode);
            }
        }

        LinkedListNode<MercyQuestDef> newNode = new(mercyQuestDef);
        lruDict[mercyQuestDef] = newNode;
        lruLinkedList.AddFirst(newNode);

        if (RatkinOrderSettings.EnableAIContent)
        {
            AIInteractionUtility.ReplaceMercyQuestTalkText(quest, mercyQuestDef);
        }
    }

    private float GetMercyQuestChance()
    {
        float chance = mercyQuestBaseChance;
        if (ResidentKnightsManager.Instance.TryGetKnightOfRole(OARO_ModDefOf.OARO_Orderly, out ResidentKnightRecord record))
        {
            chance *= (OARO_ModDefOf.OARO_Orderly.RoleWorker as ResidentKnightRoleWorker_Orderly)?.MercyQuestChaceFactor(record.Knight) ?? 1f;
        }
        return chance;
    }
}