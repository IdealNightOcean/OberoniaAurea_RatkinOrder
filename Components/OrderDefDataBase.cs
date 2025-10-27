using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderDefDataBase
{
    private static bool initialized;
    public static bool Initialized => initialized;

    private static readonly List<QuestScriptDef> mercyQuestsList = [];
    public static IReadOnlyList<QuestScriptDef> MercyQuestsList
    {
        get
        {
            if (!initialized)
            {
                Log.Error($"Attempted to use {nameof(MercyQuestsList)} before {nameof(OrderDefDataBase)} was initialized.");
                return null;
            }
            return mercyQuestsList;
        }
    }

    private static Dictionary<KnightRecord.PersonalityType, List<ResidentKnightAcademicDef>> knightAcademicByPersonality = [];

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        InitMercyQuests();
        InitResidentKnightAcademicDefs();
        initialized = true;
    }

    public static ResidentKnightAcademicDef GetRandomKnightAcademicOfPersonality(KnightRecord.PersonalityType personality)
    {
        if (!initialized)
        {
            Log.Error($"Attempted to use {nameof(knightAcademicByPersonality)} before {nameof(OrderDefDataBase)} was initialized.");
            return null;
        }
        if (knightAcademicByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> defsList))
        {
            return defsList.RandomElementWithFallback(null);
        }
        return null;
    }

    private static void InitMercyQuests()
    {
        mercyQuestsList.Clear();
        List<QuestScriptDef> allQuestDefs = DefDatabase<QuestScriptDef>.AllDefsListForReading;
        for (int i = 0; i < allQuestDefs.Count; i++)
        {
            if (allQuestDefs[i].GetModExtension<MercyQuestFlag>() is not null)
            {
                mercyQuestsList.Add(allQuestDefs[i]);
            }
        }
        Log.Message("Mercy Quests list initialized".Colorize(Color.cyan));
    }

    private static void InitResidentKnightAcademicDefs()
    {
        knightAcademicByPersonality = DefDatabase<ResidentKnightAcademicDef>.AllDefsListForReading.GroupBy(d => d.knightPersonality)
                                                                                                  .ToDictionary(g => g.Key, g => g.ToList());

        knightAcademicByPersonality ??= [];
        Log.Message("KnightAcademic-Personality dictionary initialized".Colorize(Color.cyan));
    }
}