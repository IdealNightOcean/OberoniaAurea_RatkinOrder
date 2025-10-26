using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestHandler : IExposable
{
    private float mercyQuestBaseChance;

    public void ExposeData()
    {
        Scribe_Values.Look(ref mercyQuestBaseChance, "mercyQuestBaseChance", 0f);
    }

    public void PeriodicTriggerMercyQuest()
    {
        if (GlobalOrderInteractionManager.RatkinOrderHall is null || GlobalOrderInteractionManager.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.MercyQuestTryTriggered))
        {
            return;
        }

        GlobalOrderInteractionManager.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.MercyQuestTryTriggered, cdTicks: 3 * 60000, shouldRemoveWhenExpired: true);

        Map map;
        if (Rand.Chance(1f - GetMercyQuestChance()) || (map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false)) is null)
        {
            mercyQuestBaseChance = Mathf.Max(mercyQuestBaseChance + 0.1f, 0.8f);
            return;
        }

        foreach (QuestScriptDef scriptDef in MercyQuestDataBase.AllDefsListForReading.InRandomOrder().Take(3))
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

    private float GetMercyQuestChance()
    {
        float chance = mercyQuestBaseChance;
        if (GlobalOrderInteractionManager.ResidentKnightsManager.TryGetKnightOfRole(OARO_ModDefOf.OARO_Orderly, out Pawn knight))
        {
            chance *= (OARO_ModDefOf.OARO_Orderly.RoleWorker as ResidentKnightRoleWorker_Orderly)?.MercyQuestChaceFactor(knight) ?? 1f;
        }
        return chance;
    }
}