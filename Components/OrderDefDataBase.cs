using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderDefDataBase
{
    private static readonly Dictionary<JointPatrolIncidentDef.IncidentType, List<JointPatrolIncidentDef>> jointPatrolIncidentGruopByType = [];


    private static readonly Dictionary<KnightPersonality, List<ResidentKnightAcademicDef>> residentKnightAcademicGroupByPersonality = [];

    private static readonly Dictionary<ThingDef, KnightPersonality> preferredBuildingToKnightPersonality = [];
    private static readonly Dictionary<KnightPersonality, List<ThingDef>> preferredBuildingGroupByPersonality = [];
    public static IEnumerable<ThingDef> AllResidentPreferredBuildings => preferredBuildingToKnightPersonality.Keys;

    public static void ClearStaticCache()
    {
        jointPatrolIncidentGruopByType.Clear();

        residentKnightAcademicGroupByPersonality.Clear();
        preferredBuildingToKnightPersonality.Clear();
        preferredBuildingGroupByPersonality.Clear();
    }

    public static bool TryGetAllJointPatrolIncidentsByType(JointPatrolIncidentDef.IncidentType incidentType, out List<JointPatrolIncidentDef> incidents)
    {
        return jointPatrolIncidentGruopByType.TryGetValue(incidentType, out incidents);
    }
    public static bool TryGetKnightPersonalityByBuilding(ThingDef thingDef, out KnightPersonality personality)
    {
        return preferredBuildingToKnightPersonality.TryGetValue(thingDef, out personality);
    }
    public static bool TryGetAllPreferredBuildingsByPersonality(KnightPersonality personality, out List<ThingDef> joyBuildings)
    {
        return preferredBuildingGroupByPersonality.TryGetValue(personality, out joyBuildings);
    }

    public static ResidentKnightAcademicDef GetRandomKnightAcademicOfPersonality(KnightPersonality personality)
    {
        if (residentKnightAcademicGroupByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> defsList))
        {
            return defsList.RandomElementWithFallback(null);
        }
        return null;
    }

    public static void AddJointPatrolIncident(JointPatrolIncidentDef incidentDef)
    {
        if (incidentDef is null)
        {
            Log.Error($"[OARO] Failed to add {nameof(JointPatrolIncidentDef)} to to {nameof(OrderDefDataBase)}: {nameof(incidentDef)} cannot be null.");
            return;
        }

        if (jointPatrolIncidentGruopByType.TryGetValue(incidentDef.incidentType, out List<JointPatrolIncidentDef> incidents))
        {
            incidents.Add(incidentDef);
        }
        else
        {
            jointPatrolIncidentGruopByType.Add(incidentDef.incidentType, [incidentDef]);
        }
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

    public static void AddKnightAcademic(ResidentKnightAcademicDef academicDef, KnightPersonality personality)
    {
        if (personality == KnightPersonality.None)
        {
            Log.Error($"[OARO] Failed to add {nameof(ResidentKnightAcademicDef)} to {nameof(OrderDefDataBase)}: {nameof(personality)} cannot be {nameof(KnightPersonality.None)}.");
            return;
        }
        if (academicDef is null)
        {
            Log.Error($"[OARO] Failed to add {nameof(ResidentKnightAcademicDef)} to to {nameof(OrderDefDataBase)}: {nameof(academicDef)}  cannot be null.");
            return;
        }

        if (residentKnightAcademicGroupByPersonality.TryGetValue(personality, out List<ResidentKnightAcademicDef> academicList))
        {
            academicList.Add(academicDef);
        }
        else
        {
            residentKnightAcademicGroupByPersonality.Add(personality, [academicDef]);
        }
    }
}