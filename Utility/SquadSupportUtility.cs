using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class SquadSupportUtility
{
    public enum SupportLevel : byte
    {
        Quarter,
        Half,
        Entire
    }

    public static AcceptanceReport CanBombard(Branch branch, Map map, bool resultOnly = false)
    {
        if (!branch.SupportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();

        }
        if (branch.RatkinOrder.Relationship < RelationshipKind.Friendly)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Friendly));
        }
        if (branch.SquadStat.Supply < 0.25f)
        {
            return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("25%");
        }
        if (branch.EffectTags.HasActiveTag(KeyLibrary_EffectTag.BlockBombard))
        {
            return resultOnly ? false : "OARO_BranchBlockBombard".Translate();
        }
        if (!branch.IsInAffectedRange(map.Tile))
        {
            return resultOnly ? false : "OARO_OutOfBranchAffectedRange".Translate();
        }
        return true;
    }

    public static AcceptanceReport CanSupport(Branch branch, SupportLevel level, Map map, bool resultOnly = false)
    {
        if (!branch.SupportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();

        }
        if (branch.Squad.BlockSupport)
        {
            return resultOnly ? false : "OARO_SquadSupportBeBlocked".Translate();
        }
        if (branch.RatkinOrder.Relationship < RelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Trustworthy));
        }

        SquadStat squadStat = branch.SquadStat;

        if (squadStat.MemberPercentage < 0.5f)
        {
            return resultOnly ? false : "OARO_Insufficient_MemberPercentage".Translate("50%");
        }

        switch (level)
        {
            case SupportLevel.Quarter:
                if (squadStat.Supply < 0.25f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("25%");
                }
                break;

            case SupportLevel.Half:
                if (squadStat.Supply < 0.4f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("40%");
                }
                break;

            case SupportLevel.Entire:
                if (squadStat.MemberPercentage < 0.9f)
                {
                    return resultOnly ? false : "OARO_Insufficient_MemberPercentage".Translate("90%");
                }
                if (squadStat.Supply < 0.5f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("50%");
                }
                break;

            default: break;
        }

        if (!branch.IsBranchOfType(BranchType.Mobile) && !branch.IsInAffectedRange(map.Tile))
        {
            return resultOnly ? false : "OARO_OutOfBranchAffectedRange".Translate();
        }

        return true;
    }

    public static void DoBombard(Branch branch, Map map)
    {
        int bombCount = Mathf.FloorToInt(BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_BombardSupportCount));
        if (bombCount <= 0)
        {
            return;
        }

        if (map.ThreatsCountOfPlayer() <= 0)
        {
            return;
        }
        BombardSupportMaker bombMaker = (BombardSupportMaker)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_BombardSupportMaker);
        bombMaker.SetBombardCount(bombCount);
        GenPlace.TryPlaceThing(bombMaker, IntVec3.Zero, map, ThingPlaceMode.Near);
        branch.SquadStat.Supply -= 0.25f;
    }

    public static void DoCombatSupport(Branch branch, SupportLevel level, Map map)
    {
        if (map.ThreatsCountOfPlayer() <= 0)
        {
            return;
        }

        int memberCount;
        int commanderCount;
        float supplyCost;
        SquadStat squadStat = branch.SquadStat;

        switch (level)
        {
            case SupportLevel.Quarter:
                memberCount = Mathf.FloorToInt(squadStat.MemberCount * 0.25f);
                commanderCount = Mathf.Min(1, squadStat.CommanderCountInt);
                supplyCost = 0.25f;
                break;
            case SupportLevel.Half:
                memberCount = Mathf.FloorToInt(squadStat.MemberCount * 0.5f);
                commanderCount = Mathf.Min(1, squadStat.CommanderCountInt);
                supplyCost = 0.5f;
                break;
            case SupportLevel.Entire:
                memberCount = squadStat.MemberCountInt;
                commanderCount = squadStat.CommanderCountInt;
                supplyCost = 1f;
                break;
            default: return;
        }

        if (memberCount <= 0)
        {
            return;
        }

        RatkinOrderRaidWorker ratkinOrderRaidWorker = new(branch, memberCount, commanderCount)
        {
            map = map
        };

        if (ratkinOrderRaidWorker.TryExecute())
        {
            squadStat.MemberCount -= memberCount;
            squadStat.CommanderCount -= commanderCount;
            squadStat.Supply -= supplyCost;
        }
    }
}