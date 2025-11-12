using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class BranchSupportUtility
{
    public enum SupportLevel : byte
    {
        Quarter,
        Half,
        Entire
    }

    public static AcceptanceReport CanBombard(Branch branch, Map map, bool resultOnly = false)
    {
        if (!branch.HasSupportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();
        }
        if (branch.RatkinOrder.Relationship < RelationshipKind.Friendly)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Friendly));
        }
        if (branch.Supply < 0.25f)
        {
            return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("25%");
        }
        if (branch.EffectTags.HasTag(KeyLibrary_EffectTag.BlockBombard))
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
        if (!branch.HasSupportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();

        }
        if (branch.EffectTags.HasTag(KeyLibrary_EffectTag.BlockSupport))
        {
            return resultOnly ? false : "OARO_SquadSupportBeBlocked".Translate();
        }
        if (branch.RatkinOrder.Relationship < RelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Trustworthy));
        }


        if (branch.Squad.MemberPercentage < 0.5f)
        {
            return resultOnly ? false : "OARO_Insufficient_MemberPercentage".Translate("50%");
        }

        switch (level)
        {
            case SupportLevel.Quarter:
                if (branch.Supply < 0.25f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("25%");
                }
                break;

            case SupportLevel.Half:
                if (branch.Supply < 0.4f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("40%");
                }
                break;

            case SupportLevel.Entire:
                if (branch.Supply < 0.5f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("50%");
                }
                if (branch.Squad.MemberPercentage < 0.9f)
                {
                    return resultOnly ? false : "OARO_Insufficient_MemberPercentage".Translate("90%");
                }
                break;

            default: break;
        }

        if (!branch.IsBranchOfType(Branch.BranchType.Mobile) && !branch.IsInAffectedRange(map.Tile))
        {
            return resultOnly ? false : "OARO_OutOfBranchAffectedRange".Translate();
        }

        return true;
    }

    public static void DoBombard(Branch branch, Map map)
    {
        int bombCount = Mathf.FloorToInt(BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_BombardSupportCeiling));
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
        branch.Supply -= 0.25f;
    }

    public static bool DoCombatSupport(Branch branch, SupportLevel level, Map map)
    {
        if (GenerateCombatRaidWorker(branch, level, map)?.TryExecute() ?? false)
        {
            if (Rand.Chance(0.15f))
            {
                (bool added, BranchDemandDef demandDef) = BranchDemandUtility.TryAddRandomDemandToBranch(branch, BranchDemand.DemandType.Supplementary);
                if (added)
                {
                    OrderLetter orderLetter = OrderLetterUtility.MakeOrderLetter(
                        label: "OARO_BranchDemand_SupportTriggerLabel".Translate(),
                        text: "OARO_BranchDemand_SupportTriggerText".Translate(branch.Name.Named("BRANCHNAME"), demandDef.label.Named("DEMAND")),
                        letterType: OrderLetter.LetterType.Official,
                        relatedOrder: branch.RatkinOrder,
                        sender: branch.Name);

                    OrderLetterBox.Instance.ReceiveLetter(orderLetter);
                }
            }
            return true;
        }
        return false;
    }

    public static RatkinOrderCombatParameter GenerateCombatRaidWorker(Branch branch, SupportLevel level, Map map)
    {
        int memberCount;
        int commanderCount;
        float supplyCost;
        BranchSquad squad = branch.Squad;

        switch (level)
        {
            case SupportLevel.Quarter:
                memberCount = Mathf.FloorToInt(squad.MemberCount * 0.25f);
                commanderCount = Mathf.Min(1, squad.CommanderCountInt);
                supplyCost = 0.25f;
                break;
            case SupportLevel.Half:
                memberCount = Mathf.FloorToInt(squad.MemberCount * 0.5f);
                commanderCount = Mathf.Min(1, squad.CommanderCountInt);
                supplyCost = 0.5f;
                break;
            case SupportLevel.Entire:
                memberCount = squad.MemberCountInt;
                commanderCount = squad.CommanderCountInt;
                supplyCost = 1f;
                break;
            default: return null;
        }
        int nonKnightCount = 0;
        if (branch.FacilityHandler.GetFacilityLevel(OARO_ModDefOf.OARO_SupportFacility) >= BranchFacilityLevel.Good)
        {
            nonKnightCount += 2;
        }
        BranchBuilding building = branch.BuildingHandler.GetBuilding(BranchBuildingDefOf.OARO_Church);
        if (building is not null)
        {
            nonKnightCount += (building.HasUpgraded ? 10 : 6);
        }

        if (memberCount <= 0 && commanderCount <= 0 && nonKnightCount <= 0)
        {
            Log.Error($"No valid members to generate in {nameof(BranchSupportUtility)}.{nameof(GenerateCombatRaidWorker)}: all counts are zero or negative.");
            return null;
        }

        RatkinOrderCombatParameter ratkinOrderRaidWorker = new(branch, map)
        {
            MemberCount = memberCount,
            CommanderCount = commanderCount,
            NonKnightCount = nonKnightCount,
            SupplyCost = supplyCost
        };

        return ratkinOrderRaidWorker;
    }
}