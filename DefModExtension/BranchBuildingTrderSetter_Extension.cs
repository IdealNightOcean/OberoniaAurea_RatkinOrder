using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingTrderSetter_Extension : DefModExtension
{
    public List<TraderKindDef> potentialTraders = [];
    public int refreshIntervalDays = -1;
}