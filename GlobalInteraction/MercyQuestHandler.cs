using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestHandler : IExposable
{
    public static MercyQuestHandler Instance { get; private set; }

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
    }

    public void Notify_MercyQuestSucceed(Quest quest)
    {
        ResidentKnightsManager.Instance.AllResidentKnightsGainMeditation(200f, directly: false);

        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.MercyQuestSucceed, 1, addIfMiss: true);
        float letterChance = 0.2f;

        if (ResidentKnightsManager.Instance.TryGetKnightOfRole(OARO_ModDefOf.OARO_Orderly, out ResidentKnightRecord record))
        {
            letterChance += (OARO_ModDefOf.OARO_Orderly.RoleWorker as ResidentKnightRoleWorker_Orderly).ExtraMercyQuestLetterChance(record.Knight);
        }
        if (Rand.Chance(letterChance))
        {
            RatkinOrder ratkinOrder = RatkinOrderManager.Instance.AllRatkinOrders.RandomElementWithFallback(null);
            if (!ratkinOrder.IsValid())
            {
                return;
            }
            OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
                  label: "OARO_Offical_MercyQuestSuccessLabel".Translate(ratkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)),
                  text: "OARO_Offical_MercyQuestSuccessText".Translate(ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName), quest.name.Named("QuestName")),
                  def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
                  relatedOrder: ratkinOrder,
                  sender: ratkinOrder.NameColored,
                  relatedLetterType: OrderLetter.RelatedLetterType.Positive);
            OrderRecommendation orderRecommendation = RecommendationUtility.MakeRecommendationForPlayer(count: 1);
            orderLetter.AddAttachment(orderRecommendation);
            OrderLetterBox.Instance.ReceiveLetter(orderLetter);
        }
    }

    public void PeriodicTriggerMercyQuest()
    {
        if (TryPeriodicTriggerMercyQuest())
        {
            mercyQuestBaseChance = 0f;
        }
        else
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

        int potentialCount = Mathf.Clamp(DefDatabase<MercyQuestDef>.DefCount / 3, 5, 10);
        List<MercyQuestDef> potentialMercies = DefDatabase<MercyQuestDef>.AllDefsListForReading.TakeRandom(potentialCount).Where(m => m.secondSelectWeight > 0f).ToList();
        while (potentialMercies.Count > 0)
        {
            MercyQuestDef mercyQuestDef = potentialMercies.RandomElementByWeight(m => m.secondSelectWeight);
            if (mercyQuestDef is not null && TryTriggerMercyQuest(mercyQuestDef, map))
            {
                mercyQuestBaseChance = 0f;
                return true;
            }
            potentialMercies.Remove(mercyQuestDef);
        }

        return false;
    }

    public static bool TryTriggerMercyQuest(MercyQuestDef mercyQuestDef, Map map)
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
            if (RatkinOrderSettings.EnableAIContent)
            {
                AIInteractionHandler.Instance?.ReplaceMercyQuestTalkText(quest, mercyQuestDef);
            }
            return true;
        }
        else
        {
            return false;
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