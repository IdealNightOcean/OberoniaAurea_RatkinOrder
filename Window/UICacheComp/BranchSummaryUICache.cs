using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

public class BranchSummaryUICache
{
    public Branch Branch { get; }

    public string SquadName { get; } = "----";
    public string BaseSiteName { get; } = "----";
    public float Distance { get; } = -1f;
    public float AffectedRange { get; } = -1f;
    public int CrewCeiling { get; } = -1;

    public bool IsInAffectedRange => AffectedRange >= Distance;
    public int AllCrewCount => Branch?.Squad.AllCrewCountInt ?? 0;

    public BranchSummaryUICache() { }

    public BranchSummaryUICache(Branch branch, Map map)
    {
        _ = map ?? throw new ArgumentNullException(nameof(map));

        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        SquadName = branch.Squad.Name;

        if (branch.BaseSite is INameableWorldObject nameSite)
        {
            BaseSiteName = nameSite.Name;
        }
        else
        {
            BaseSiteName = branch.BaseSite.Label;
        }

        Distance = branch.DistanceTo(map.Tile);
        AffectedRange = branch.GetStatValue(BranchStatDefOf.OARO_AffectRadius);
        CrewCeiling = (int)(branch.Squad.MemberCeiling + branch.Squad.CommanderCeiling);
    }

    public class UIEntryComparer : IComparer<BranchSummaryUICache>
    {
        public int Compare(BranchSummaryUICache x, BranchSummaryUICache y)
        {
            Branch xBranch = x?.Branch;
            Branch yBranch = y?.Branch;

            if (xBranch is null && yBranch is null) return 0;
            if (xBranch is not null && yBranch is null) return -1;
            if (xBranch is null && yBranch is not null) return 1;
            if (x.IsInAffectedRange != y.IsInAffectedRange)
            {
                return x.IsInAffectedRange ? -1 : 1;
            }

            bool xIsFriendly = xBranch.IsBranchOfType(BranchType.Friendly);
            bool yIsFriendly = yBranch.IsBranchOfType(BranchType.Friendly);
            if (xIsFriendly != yIsFriendly)
            {
                return xIsFriendly ? -1 : 1;
            }

            bool xIsHonor = xBranch.IsBranchOfType(BranchType.Honor);
            bool yIsHonor = yBranch.IsBranchOfType(BranchType.Honor);
            if (xIsHonor != yIsHonor)
            {
                return xIsHonor ? -1 : 1;
            }

            return x.Distance.CompareTo(y.Distance);
        }
    }
}