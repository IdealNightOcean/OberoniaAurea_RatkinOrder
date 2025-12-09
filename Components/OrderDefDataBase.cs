using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderDefDataBase
{
    private static readonly Dictionary<KnightPersonality, List<ResidentKnightAcademicDef>> knightAcademicByPersonality = [];

    private static readonly Dictionary<ThingDef, KnightPersonality> preferredBuildingToKnightPersonality = [];
    private static readonly Dictionary<KnightPersonality, List<ThingDef>> knightPersonalityToPreferredBuilding = [];
    public static IEnumerable<ThingDef> AllResidentPreferredBuildings => preferredBuildingToKnightPersonality.Keys;

    public static void ClearStaticCache()
    {
        knightAcademicByPersonality.Clear();
        preferredBuildingToKnightPersonality.Clear();
        knightPersonalityToPreferredBuilding.Clear();
    }

    public static bool GetKnightPersonalityForPreferredBuilding(ThingDef thingDef, out KnightPersonality personality)
    {
        return preferredBuildingToKnightPersonality.TryGetValue(thingDef, out personality);
    }
    public static bool GetPreferredBuildingForKnightPersonality(KnightPersonality personality, out List<ThingDef> joyBuildings)
    {
        return knightPersonalityToPreferredBuilding.TryGetValue(personality, out joyBuildings);
    }

    public static ResidentKnightAcademicDef GetRandomKnightAcademicOfPersonality(KnightPersonality personality)
    {
        if (knightAcademicByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> defsList))
        {
            return defsList.RandomElementWithFallback(null);
        }
        return null;
    }

    public static void AddResidentKnightPreferBuilding(ThingDef buildingDef, KnightPersonality personality)
    {
        if (buildingDef is null)
        {
            Log.Error($"[OARO] Failed to add building to to {nameof(OrderDefDataBase)}.{nameof(preferredBuildingToKnightPersonality)}: buildingDef cannot be null.");
            return;
        }
        if (personality == KnightPersonality.None)
        {
            Log.Error($"[OARO] Failed to add building to {nameof(OrderDefDataBase)}.{nameof(preferredBuildingToKnightPersonality)}: KnightPersonality cannot be None.");
            return;
        }

        preferredBuildingToKnightPersonality[buildingDef] = personality;
        if (knightPersonalityToPreferredBuilding.TryGetValue(personality, out List<ThingDef> buildings))
        {
            buildings.Add(buildingDef);
        }
        else
        {
            knightPersonalityToPreferredBuilding.Add(personality, [buildingDef]);
        }
    }

    public static void AddKnightAcademicByPersonality(KnightPersonality personality, ResidentKnightAcademicDef academicDef)
    {
        if (personality == KnightPersonality.None)
        {
            Log.Error($"[OARO] Failed to add building to {nameof(OrderDefDataBase)}.{nameof(preferredBuildingToKnightPersonality)}: KnightPersonality cannot be None.");
            return;
        }
        if (academicDef is null)
        {
            Log.Error($"[OARO] Failed to add building to to {nameof(OrderDefDataBase)}.{nameof(preferredBuildingToKnightPersonality)}: academicDef cannot be null.");
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