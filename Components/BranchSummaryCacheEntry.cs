using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

public class BranchSummaryCacheEntry
{
    public readonly Branch Branch;

    public readonly string SquadName = "----";
    public readonly string BaseSiteName = "----";
    public readonly float Distance = -1f;
    public readonly float AffectedRange = -1f;
    public bool IsInAffectedRange => AffectedRange >= Distance;
    public readonly int CurAllCrewCount = -1;
    public readonly float Potency;

    public readonly Texture2D HonorIcon;
    public readonly Texture2D HonorStripSmall;
    public readonly Texture2D HonorBackgroundSmall;
    public readonly Texture2D HonorDecorationSmall;

    public BranchSummaryCacheEntry() { }

    public BranchSummaryCacheEntry(Branch branch, Map map)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        _ = map ?? throw new ArgumentNullException(nameof(map));

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
        CurAllCrewCount = branch.Squad.AllCrewCountInt;

        if (branch.IsBranchOfType(BranchType.Honor))
        {
            HonorIcon = branch.HonorDef?.IconTexture;
            BranchMedalRecord.BranchMedalType primaryMedal = branch.MedalHandler.PrimaryMedal;
            if (primaryMedal != BranchMedalRecord.BranchMedalType.None)
            {
                HonorStripSmall = new CachedTexture($"UI/BranchSquad/OARO_HonorStripSmall_{primaryMedal}").Texture;
                HonorBackgroundSmall = new CachedTexture($"UI/BranchSquad/OARO_HonorBackgroundSmall_{primaryMedal}").Texture;
                HonorDecorationSmall = new CachedTexture($"UI/BranchSquad/OARO_HonorDecorationSmall_{primaryMedal}").Texture;
            }
        }
    }

    public class SquadWindowEntryComparer : IComparer<BranchSummaryCacheEntry>
    {
        public int Compare(BranchSummaryCacheEntry x, BranchSummaryCacheEntry y)
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
