using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderDefDataBase
{
    private static readonly List<QuestScriptDef> mercyQuestsList = [];
    public static IReadOnlyList<QuestScriptDef> MercyQuestsList => mercyQuestsList;

    private static readonly Dictionary<KnightPersonality, List<ResidentKnightAcademicDef>> knightAcademicByPersonality = [];

    private static readonly Dictionary<ThingDef, KnightPersonality> joyBuildingToKnightPersonality = [];


    public static void ClearStaticCache()
    {
        mercyQuestsList.Clear();
        knightAcademicByPersonality.Clear();
        joyBuildingToKnightPersonality.Clear();
    }

    public static bool GetKnightPersonalityForJoyBuilding(ThingDef thingDef, out KnightPersonality personality)
    {
        return joyBuildingToKnightPersonality.TryGetValue(thingDef, out personality);
    }

    public static ResidentKnightAcademicDef GetRandomKnightAcademicOfPersonality(KnightPersonality personality)
    {
        if (knightAcademicByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> defsList))
        {
            return defsList.RandomElementWithFallback(null);
        }
        return null;
    }

    public static void AddMercyQuests(QuestScriptDef scriptDef)
    {
        if (scriptDef is null)
        {
            Log.Error($"[OARO] Failed to add building to to {nameof(OrderDefDataBase)}.{nameof(joyBuildingToKnightPersonality)}: scriptDef cannot be null.");
            return;
        }
        mercyQuestsList.Add(scriptDef);
    }
    public static void AddKnightJoyBuilding(ThingDef buildingDef, KnightPersonality personality)
    {
        if (buildingDef is null)
        {
            Log.Error($"[OARO] Failed to add building to to {nameof(OrderDefDataBase)}.{nameof(joyBuildingToKnightPersonality)}: buildingDef cannot be null.");
            return;
        }
        if (personality == KnightPersonality.None)
        {
            Log.Error($"[OARO] Failed to add building to {nameof(OrderDefDataBase)}.{nameof(joyBuildingToKnightPersonality)}: KnightPersonality cannot be None.");
            return;
        }
        joyBuildingToKnightPersonality[buildingDef] = personality;
    }

    public static void AddKnightAcademicByPersonality(KnightPersonality personality, ResidentKnightAcademicDef academicDef)
    {
        if (personality == KnightPersonality.None)
        {
            Log.Error($"[OARO] Failed to add building to {nameof(OrderDefDataBase)}.{nameof(joyBuildingToKnightPersonality)}: KnightPersonality cannot be None.");
            return;
        }
        if (academicDef is null)
        {
            Log.Error($"[OARO] Failed to add building to to {nameof(OrderDefDataBase)}.{nameof(joyBuildingToKnightPersonality)}: academicDef cannot be null.");
            return;
        }

        if (knightAcademicByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> academicList))
        {
            academicList.Add(academicDef);
        }
        else
        {
            knightAcademicByPersonality.Add(personality, [academicDef]);
        }
    }
}