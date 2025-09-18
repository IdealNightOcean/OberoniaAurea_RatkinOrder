using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadSupportHandler(Squad squad) : IExposable
{
    public enum SupportLevel : byte
    {
        Quarter,
        Half,
        Entire
    }

    [Unsaved] public readonly Squad Squad = squad ?? throw new System.ArgumentNullException(nameof(squad));

    private bool supportAuthority; //是否有支援权限
    public bool SupportAuthority => supportAuthority;

    public AcceptanceReport CanBombard(Map map, bool resultOnly = false)
    {
        if (!supportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();

        }
        if (Squad.RatkinOrder.Relationship < OrderRelationshipKind.Friendly)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(OrderRelationshipKind.Friendly));
        }
        if (Squad.SquadStat.Supply < 0.25f)
        {
            return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("25%");
        }
        if (Squad.Branch.EffectTags.HasActiveTag(KeyLibrary_EffectTag.BlockBombard))
        {
            return resultOnly ? false : "OARO_BranchBlockBombard".Translate();
        }
        if (!Squad.Branch.IsInAffectedRange(map.Tile))
        {
            return resultOnly ? false : "OARO_OutOfBranchAffectedRange".Translate();
        }
        return true;
    }

    public AcceptanceReport CanSupport(SupportLevel level, Map map, bool resultOnly = false)
    {
        if (!supportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();

        }
        if (Squad.BlockSupport)
        {
            return resultOnly ? false : "OARO_SquadSupportBeBlocked".Translate();
        }
        if (Squad.RatkinOrder.Relationship < OrderRelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(OrderRelationshipKind.Trustworthy));
        }

        SquadStat squadStat = Squad.SquadStat;

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

        if (!Squad.Branch.IsBranchOfType(BranchType.Mobile) && !Squad.Branch.IsInAffectedRange(map.Tile))
        {
            return resultOnly ? false : "OARO_OutOfBranchAffectedRange".Translate();
        }

        return true;
    }


    public void DoBombard(Map map)
    {
        int bombCount = Mathf.FloorToInt(BranchStatUtility.GetStatValue(Squad.Branch, BranchStatDefOf.OARO_BombardSupportCount));
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
        Squad.SquadStat.Supply -= 0.25f;
    }

    public void DoCombatSupport(SupportLevel level, Map map)
    {
        if (map.ThreatsCountOfPlayer() <= 0)
        {
            return;
        }

        int memberCount;
        int commanderCount;
        float supplyCost;
        SquadStat squadStat = Squad.SquadStat;

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

        if (SquadCombatPawnUtility.TryAssistSupport(Squad, map, memberCount, commanderCount))
        {
            squadStat.MemberCount -= memberCount;
            squadStat.CommanderCount -= commanderCount;
            squadStat.Supply -= supplyCost;
        }
    }



    public void ExposeData()
    {
        Scribe_Values.Look(ref supportAuthority, "supportAuthority", defaultValue: false);
    }
}
