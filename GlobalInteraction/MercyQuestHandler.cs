using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
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
            OrderRecommendation orderRecommendation = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
            orderRecommendation.SetRatkinOrder(ratkinOrder);
            orderLetter.AddAttachment(orderRecommendation);
            OrderLetterBox.Instance.ReceiveLetter(orderLetter);
        }
    }

    public void PeriodicTriggerMercyQuest()
    {
        if (OrderHallHandler.Instance.OrderHallRoom is null || GlobalInteractionManager.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.MercyQuestTryTriggered))
        {
            return;
        }

        GlobalInteractionManager.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.MercyQuestTryTriggered, cdTicks: 3 * 60000, removeWhenExpired: true);

        Map map;
        if (Rand.Chance(1f - GetMercyQuestChance(mercyQuestBaseChance)) || (map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false)) is null)
        {
            mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance + 0.1f, 0.8f);
            return;
        }

        foreach (MercyQuestDef mercyQuestDef in DefDatabase<MercyQuestDef>.AllDefsListForReading.TakeRandomElements(5))
        {
            if (TryTriggerMercyQuest(mercyQuestDef, map))
            {
                mercyQuestBaseChance = 0f;
                return;
            }
        }

        mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance + 0.1f, 0.8f);
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
        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(
            quest: out _,
            scriptDef: mercyQuestDef.needPreQuest ? mercyQuestDef.preQuestDef : mercyQuestDef.mainQuestDef,
            slate: slate,
            forced: true,
            target: map);
    }

    private static float GetMercyQuestChance(float baseChance)
    {
        float chance = baseChance;
        if (ResidentKnightsManager.Instance.TryGetKnightOfRole(OARO_ModDefOf.OARO_Orderly, out ResidentKnightRecord record))
        {
            chance *= (OARO_ModDefOf.OARO_Orderly.RoleWorker as ResidentKnightRoleWorker_Orderly)?.MercyQuestChaceFactor(record.Knight) ?? 1f;
        }
        return chance;
    }
}