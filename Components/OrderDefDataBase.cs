using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderDefDatabase
{
    private static List<KnightChivalryDef> medalChivalries;
    public static List<KnightChivalryDef> MedalChivalries
    {
        get
        {
            medalChivalries ??= DefDatabase<KnightChivalryDef>.AllDefsListForReading.Where(c => c.medal is not null).ToList();
            return medalChivalries;
        }
    }

    private static List<KnightChivalryDef> jointPatrolChivalries;

    public static List<KnightChivalryDef> JointPatrolChivalries
    {
        get
        {
            jointPatrolChivalries ??= DefDatabase<KnightChivalryDef>.AllDefsListForReading.Where(c => c.jointPatrol is not null).ToList();
            return jointPatrolChivalries;
        }
    }

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

    private static List<ThingDef> allKnightPreferredBuildingsCached;
    public static List<ThingDef> AllKnightPreferredBuildings
    {
        get
        {
            if (allKnightPreferredBuildingsCached is null)
            {
                allKnightPreferredBuildingsCached = [];
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.building is not null && def.HasModExtension<ResidentKnightPreferredBuildingExtension>())
                    {
                        allKnightPreferredBuildingsCached.Add(def);
                    }
                }
            }
            return allKnightPreferredBuildingsCached;
        }
    }

    public static void ClearStaticCache()
    {
        medalChivalries = null;
        jointPatrolChivalries = null;
        jointPatrolIncidentGruopByType = null;
        allKnightPreferredBuildingsCached = null;
    }


    public static bool TryGetAllJointPatrolIncidentsByType(JointPatrolIncidentDef.IncidentType incidentType, out List<JointPatrolIncidentDef> incidents)
    {
        return JointPatrolIncidentGruopByType.TryGetValue(incidentType, out incidents);
    }
}