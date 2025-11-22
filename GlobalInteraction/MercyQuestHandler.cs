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
        Scribe_Values.Look(ref mercyQuestBaseChance, "mercyQuestBaseChance", 0f);
    }

    public void Notify_MercyQuestSucceed(Quest quest)
    {
        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.MercyQuestSucceed, 1, addIfMiss: true);
        float letterChance = 0.2f;

        if (ResidentKnightsManager.Instance.TryGetKnightOfRole(OARO_ModDefOf.OARO_Orderly, out ResidentKnightRecord record))
        {
            letterChance += (OARO_ModDefOf.OARO_Orderly.RoleWorker as ResidentKnightRoleWorker_Orderly).ExtraMercyQuestLetterChance(record.Knight);
        }
        if (Rand.Chance(letterChance))
        {

        }

        ResidentKnightsManager.Instance.Notify_MercyQuestSucceed();
    }

    public void PeriodicTriggerMercyQuest()
    {
        if (OrderHallHandler.Instance.OrderHallRoom is null || GlobalInteractionManager.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.MercyQuestTryTriggered))
        {
            return;
        }

        GlobalInteractionManager.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.MercyQuestTryTriggered, cdTicks: 3 * 60000, shouldRemoveWhenExpired: true);

        Map map;
        if (Rand.Chance(1f - GetMercyQuestChance(mercyQuestBaseChance)) || (map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false)) is null)
        {
            mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance + 0.1f, 0.8f);
            return;
        }

        foreach (QuestScriptDef scriptDef in OrderDefDataBase.MercyQuestsList.TakeRandomElements(3).InRandomOrder())
        {
            Slate slate = new();
            slate.Set("map", map);
            slate.Set(KeyLibrary_SlateStoreAs.MercyQuest, scriptDef);

            // 善行任务的派系Test时未生成，只好强制触发了
            if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_MercyPre_HelpSeeker, slate, forced: true, target: map))
            {
                mercyQuestBaseChance = 0f;
                return;
            }
        }

        mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance + 0.1f, 0.8f);
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