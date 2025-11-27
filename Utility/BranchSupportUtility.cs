using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class BranchSupportUtility
{
    public enum DeploymentLevel : byte
    {
        Quarter,
        Half,
        Entire
    }

    public static AcceptanceReport CanBombard(Branch branch, Map map, bool resultOnly)
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

    public static AcceptanceReport CanCombatKnightSupport(Branch branch, Map map, DeploymentLevel level, bool resultOnly)
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
            case DeploymentLevel.Quarter:
                if (branch.Supply < 0.25f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("25%");
                }
                break;

            case DeploymentLevel.Half:
                if (branch.Supply < 0.4f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate("40%");
                }
                break;

            case DeploymentLevel.Entire:
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

    /// <summary>
    /// 分部战斗骑士支援
    /// </summary>
    public static bool DoCombatKnightSupport(Branch branch, Map map, DeploymentLevel level, bool sendStandardLetter)
    {
        if (!GenerateCombatKnightGenerateParmsByDeploymentLevel(branch, map, level, out CombatKnightGenerateParms parms))
        {
            return false;
        }

        if (TryDeployCombatKnight(parms, sendStandardLetter))
        {
            if (Rand.Chance(0.15f))
            {
                (bool added, BranchDemandDef demandDef) = BranchDemandUtility.TryAddRandomDemandToBranch(branch, BranchDemand.DemandType.Supplementary);
                if (added)
                {
                    OrderLetter orderLetter = OrderLetterUtility.MakeOrderLetter(
                        label: "OARO_BranchDemand_SupportTriggerLabel".Translate(),
                        text: "OARO_BranchDemand_SupportTriggerText".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), demandDef.label.Named("DEMAND")),
                        def: OrderLetterDefOf.OARO_OfficialLetter,
                        relatedOrder: branch.RatkinOrder,
                        sender: branch.Name,
                        relatedLetterType: OrderLetter.RelatedLetterType.Neutral);

                    OrderLetterBox.Instance.ReceiveLetter(orderLetter);
                }
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 根据部署等级创建战斗人员生成参数<paramref name="parms"/>（<see cref="CombatKnightGenerateParms"/>）
    /// </summary>
    public static bool GenerateCombatKnightGenerateParmsByDeploymentLevel(Branch branch, Map map, DeploymentLevel level, out CombatKnightGenerateParms parms)
    {
        parms = default;

        int memberCount;
        int commanderCount;
        float supplyCost;
        BranchSquad squad = branch.Squad;

        switch (level)
        {
            case DeploymentLevel.Quarter:
                memberCount = Mathf.FloorToInt(squad.MemberCount * 0.25f);
                commanderCount = Mathf.Min(1, squad.CommanderCountInt);
                supplyCost = 0.25f;
                break;
            case DeploymentLevel.Half:
                memberCount = Mathf.FloorToInt(squad.MemberCount * 0.5f);
                commanderCount = Mathf.Min(1, squad.CommanderCountInt);
                supplyCost = 0.5f;
                break;
            case DeploymentLevel.Entire:
                memberCount = squad.MemberCountInt;
                commanderCount = squad.CommanderCountInt;
                supplyCost = 1f;
                break;
            default: return false;
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

        int totalCount = memberCount + commanderCount + nonKnightCount;
        if (totalCount <= 0)
        {
            Log.Error($"[OARO] No valid members to generate in {nameof(BranchSupportUtility)}.{nameof(GenerateCombatKnightGenerateParmsByDeploymentLevel)}: all counts are zero or negative.");
            return false;
        }

        parms = new(branch, map)
        {
            MemberCount = memberCount,
            CommanderCount = commanderCount,
            NonKnightCount = nonKnightCount,
            SupplyCost = supplyCost
        };

        return true;
    }

    /// <summary>
    /// 部署战斗人员
    /// </summary>
    public static bool TryDeployCombatKnight(CombatKnightGenerateParms parms, bool sendStandardLetter)
    {
        if (!parms.IsValid)
        {
            return false;
        }

        Faction faction = parms.Faction;
        bool isFriendly = parms.IsFriendly;
        parms.RaidArrivalMode ??= (isFriendly ? PawnsArrivalModeDefOf.EdgeDrop : PawnsArrivalModeDefOf.EdgeWalkIn);
        parms.RaidStrategy ??= (isFriendly ? RaidStrategyDefOf.ImmediateAttackFriendly : RaidStrategyDefOf.ImmediateAttack);
        IncidentParms incidentParms = new()
        {
            target = parms.Map,
            faction = faction,
            raidStrategy = parms.RaidStrategy,
            raidArrivalMode = parms.RaidArrivalMode,
        };
        if (!parms.RaidArrivalMode.Worker.TryResolveRaidSpawnCenter(incidentParms))
        {
            return false;
        }
        parms.MemberCount = parms.Branch is null ? parms.MemberCount : Mathf.Min(parms.MemberCount, parms.Branch.Squad.MemberCountInt);
        parms.CommanderCount = parms.Branch is null ? parms.CommanderCount : Mathf.Min(parms.CommanderCount, parms.Branch.Squad.CommanderCountInt);

        List<Pawn> combatPanws = KnightGenerateUtility.GenerateCombatantKnights(parms);
        if (combatPanws.NullOrEmpty())
        {
            return false;
        }
        incidentParms.pawnCount = combatPanws.Count;
        parms.RaidArrivalMode.Worker.Arrive(combatPanws, incidentParms);

        if (isFriendly)
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony_NeverFleeOrder(faction), parms.Map, combatPanws);
        }
        else
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, incidentParms.spawnCenter), parms.Map, combatPanws);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            parms.Map.StoryState.lastRaidFaction = faction;
        }

        if (parms.Branch is not null)
        {
            parms.Branch.Squad.AdjustCrew(member: -parms.MemberCount, commander: -parms.CommanderCount);
            parms.Branch.Supply -= parms.SupplyCost;
        }


        if (sendStandardLetter)
        {
            TaggedString label = (isFriendly ? parms.RaidStrategy.letterLabelFriendly : parms.RaidStrategy.letterLabelEnemy)
                               + ": "
                               + (parms.Branch is null ? parms.RatkinOrder.Name : parms.Branch.Name);

            ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                label: label,
                text: CombatDeployGetLetterText(parms, combatPanws),
                def: isFriendly ? OARO_LetterDefOf.OARO_Order_PositiveLetter : OARO_LetterDefOf.OARO_Order_ThreatBigLetter,
                relatedFaction: faction,
                lookTargets: combatPanws);
            letter.relatedOrder = parms.RatkinOrder;
            Find.LetterStack.ReceiveLetter(letter);
        }

        return true;
    }

    private static TaggedString CombatDeployGetLetterText(CombatKnightGenerateParms parms, List<Pawn> pawns)
    {
        Faction faction = parms.Faction;
        StringBuilder textSB = new();
        if (parms.IsFriendly)
        {
            textSB.AppendLine(string.Format(parms.RaidArrivalMode.textFriendly, faction.def.pawnsPlural, faction.Name.ApplyTag(faction)).CapitalizeFirst());
            textSB.AppendLine();
            if (parms.Branch is null)
            {
                textSB.AppendLine("OARO_CombatDeployText_RatkinOrderInfo".Translate(parms.RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName), parms.CommanderCount));
            }
            else
            {
                textSB.AppendLine("OARO_CombatDeployText_BranchInfo".Translate(parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName), parms.CommanderCount));
            }
            textSB.AppendLine();
            textSB.AppendLine(parms.RaidStrategy.arrivalTextFriendly);
            Pawn pawn = pawns.Find(p => p.Faction.leader == p);
            if (pawn is not null)
            {
                textSB.AppendLine();
                textSB.AppendLine("FriendlyRaidLeaderPresent".Translate(faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER")).Resolve());
            }
        }
        else
        {
            textSB.AppendLine(string.Format(parms.RaidArrivalMode.textEnemy, faction.def.pawnsPlural, faction.Name.ApplyTag(faction)).CapitalizeFirst());
            textSB.AppendLine();
            if (parms.Branch is null)
            {
                textSB.AppendLine("OARO_CombatDeployText_RatkinOrderInfo".Translate(parms.RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName), parms.CommanderCount));
            }
            else
            {
                textSB.AppendLine("OARO_CombatDeployText_BranchInfo".Translate(parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName), parms.CommanderCount));
            }
            textSB.AppendLine();
            textSB.AppendLine(parms.RaidStrategy.arrivalTextEnemy);
            Pawn pawn = pawns.Find(p => p.Faction.leader == p);
            if (pawn is not null)
            {
                textSB.AppendLine();
                textSB.AppendLine("EnemyRaidLeaderPresent".Translate(faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER")).Resolve());
            }
        }
        return textSB.ToTaggedString();
    }
}