using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderDefDataBase
{
    private static Dictionary<JointPatrolIncidentDef.IncidentType, List<JointPatrolIncidentDef>> jointPatrolIncidentGruopByType;
    public static Dictionary<JointPatrolIncidentDef.IncidentType, List<JointPatrolIncidentDef>> JointPatrolIncidentGruopByType
    {
        get
        {
            return jointPatrolIncidentGruopByType ??= DefDatabase<JointPatrolIncidentDef>.AllDefsListForReading
                    .GroupBy(d => d.incidentType)
                    .ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    private static Dictionary<KnightPersonality, List<ResidentKnightAcademicDef>> residentKnightAcademicGroupByPersonality;
    public static Dictionary<KnightPersonality, List<ResidentKnightAcademicDef>> ResidentKnightAcademicGroupByPersonality
    {
        get
        {
            return residentKnightAcademicGroupByPersonality ??= DefDatabase<ResidentKnightAcademicDef>.AllDefsListForReading
                .GroupBy(d => d.personality)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    private static Dictionary<BranchTaskType, List<BranchMedalDef>> branchMedalDefGruopByTaskType = [];

    private static readonly Dictionary<ThingDef, KnightPersonality> preferredBuildingToKnightPersonality = [];
    private static readonly Dictionary<KnightPersonality, List<ThingDef>> preferredBuildingGroupByPersonality = [];
    public static IEnumerable<ThingDef> AllResidentPreferredBuildings => preferredBuildingToKnightPersonality.Keys;

    public static void ClearStaticCache()
    {
        jointPatrolIncidentGruopByType = null;
        residentKnightAcademicGroupByPersonality = null;

        branchMedalDefGruopByTaskType.Clear();

        preferredBuildingToKnightPersonality.Clear();
        preferredBuildingGroupByPersonality.Clear();
    }

    public static IReadOnlyList<BranchMedalDef> TryGetAllBranchMedalDefsByTaskType(BranchTaskType taskType)
    {
        if (branchMedalDefGruopByTaskType.TryGetValue(taskType, out List<BranchMedalDef> defs))
        {
            return defs;
        }

        List<BranchMedalDef> medalDefs = [];
        foreach (BranchMedalDef def in DefDatabase<BranchMedalDef>.AllDefsListForReading)
        {
            if (def.focusedTaskType == taskType)
            {
                medalDefs.Add(def);
            }
        }
        branchMedalDefGruopByTaskType.Add(taskType, medalDefs);
        return medalDefs;
    }

    public static bool TryGetKnightPersonalityByBuilding(ThingDef thingDef, out KnightPersonality personality)
    {
        return preferredBuildingToKnightPersonality.TryGetValue(thingDef, out personality);
    }

    public static bool TryGetAllJointPatrolIncidentsByType(JointPatrolIncidentDef.IncidentType incidentType, out List<JointPatrolIncidentDef> incidents)
    {
        return JointPatrolIncidentGruopByType.TryGetValue(incidentType, out incidents);
    }

    public static bool TryGetAllPreferredBuildingsByPersonality(KnightPersonality personality, out List<ThingDef> joyBuildings)
    {
        return preferredBuildingGroupByPersonality.TryGetValue(personality, out joyBuildings);
    }

    public static ResidentKnightAcademicDef GetRandomKnightAcademicOfPersonality(KnightPersonality personality)
    {
        if (ResidentKnightAcademicGroupByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> defsList))
        {
            return defsList.RandomElementWithFallback(null);
        }
        return null;
    }

    public static void AddKnightPreferBuilding(ThingDef buildingDef, KnightPersonality personality)
    {
        if (buildingDef is null)
        {
            Log.Error($"[OARO] Failed to add {nameof(ThingDef)} to to {nameof(OrderDefDataBase)}: {nameof(buildingDef)} cannot be null.");
            return;
        }
        if (personality == KnightPersonality.None)
        {
            Log.Error($"[OARO] Failed to add {nameof(ThingDef)} to {nameof(OrderDefDataBase)}: {nameof(personality)} cannot be {nameof(KnightPersonality.None)}.");
            return;
        }

        preferredBuildingToKnightPersonality[buildingDef] = personality;
        if (preferredBuildingGroupByPersonality.TryGetValue(personality, out List<ThingDef> buildings))
        {
            buildings.Add(buildingDef);
        }
        else
        {
            preferredBuildingGroupByPersonality.Add(personality, [buildingDef]);
        }
    }
}