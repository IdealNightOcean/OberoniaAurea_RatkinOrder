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

    private static readonly Dictionary<BranchTaskType, List<BranchMedalDef>> branchMedalDefGruopByTaskType = [];

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
        jointPatrolIncidentGruopByType = null;
        allKnightPreferredBuildingsCached = null;

        branchMedalDefGruopByTaskType.Clear();
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

    public static bool TryGetAllJointPatrolIncidentsByType(JointPatrolIncidentDef.IncidentType incidentType, out List<JointPatrolIncidentDef> incidents)
    {
        return JointPatrolIncidentGruopByType.TryGetValue(incidentType, out incidents);
    }
}