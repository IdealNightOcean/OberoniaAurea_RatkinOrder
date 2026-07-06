using RimWorld;
using System;
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
    private static readonly DeploymentLevel[] deploymentLevelArr = (DeploymentLevel[])Enum.GetValues(typeof(DeploymentLevel));

    public static AcceptanceReport CanBombard(Branch branch, Map map, bool resultOnly)
    {
        if (!branch.IsValid() || map is null)
        {
            return false;
        }
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
        if (!branch.IsValid() || map is null)
        {
            return false;
        }
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
        IntVec3 placeCell = CellFinder.RandomEdgeCell(map);
        BombardSupportMaker bombMaker = (BombardSupportMaker)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_BombardSupportMaker);
        bombMaker.SetBombardCount(branch);
        GenPlace.TryPlaceThing(bombMaker, placeCell, map, ThingPlaceMode.Near);
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

        if (!TryDeployCombatKnight(parms, sendStandardLetter))
        {
            return false;
        }

        try
        {
            if (Rand.Chance(0.15f) && BranchDemandUtility.TryAddRandomDemandToBranch(out BranchDemandDef demandDef, branch, BranchDemand.DemandType.Supplementary))
            {
                OrderLetter orderLetter = OrderLetterUtility.MakeOrderLetter(
                    label: "OARO_BranchDemand_SupportTriggerLabel".Translate(),
                    text: "OARO_BranchDemand_SupportTriggerText".Translate(branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName), demandDef.label.Named("DEMAND")),
                    def: OrderLetterDefOf.OARO_OfficialLetter,
                    relatedOrder: branch.RatkinOrder,
                    sender: branch.NameColored,
                    relatedLetterType: OrderLetter.RelatedLetterType.Neutral);

                OrderLetterBox.Instance.ReceiveLetter(orderLetter);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "triger supplementary branch demand",
                typeName: nameof(BranchSupportUtility),
                methodName: nameof(DoCombatKnightSupport),
                needStackTrace: true);
        }

        return true;
    }

    public static void CombatKnightSupportFloatMenu(Branch branch, Map map)
    {
        List<FloatMenuOption> options = [];
        foreach (DeploymentLevel level in deploymentLevelArr)
        {
            AcceptanceReport acceptance = CanCombatKnightSupport(branch, map, level, resultOnly: false);
            if (acceptance)
            {
                options.Add(new FloatMenuOption($"OARO_DeploymentLevel_{level}".Translate(), action: delegate
                {
                    DoCombatKnightSupport(branch, map, level, sendStandardLetter: true);
                }));
            }
            else
            {
                string optText = $"OARO_DeploymentLevel_{level}".Translate() + $" ({acceptance.Reason})";
                options.Add(new FloatMenuOption(optText, action: null));
            }
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    /// <summary>
    /// 根据部署等级创建战斗人员生成参数<paramref name="parms"/> (<see cref="CombatKnightGenerateParms"/> )
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
            Log.Error($"[OARO] 在 {nameof(BranchSupportUtility)}.{nameof(GenerateCombatKnightGenerateParmsByDeploymentLevel)} 中没有有效的成员可生成：所有计数为零或负数。");
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
            letter.RelatedOrder = parms.RatkinOrder;
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
                textSB.AppendLine("OARO_CombatDeployText_RatkinOrderInfo".Translate(
                    parms.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    parms.CommanderCount.Named("CommanderCount")));
            }
            else
            {
                textSB.AppendLine("OARO_CombatDeployText_BranchInfo".Translate(
                    parms.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    parms.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                    parms.CommanderCount.Named("CommanderCount")));
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
                textSB.AppendLine("OARO_CombatDeployText_RatkinOrderInfo".Translate(
                    parms.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    parms.CommanderCount.Named("CommanderCount")));
            }
            else
            {
                textSB.AppendLine("OARO_CombatDeployText_BranchInfo".Translate(
                    parms.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    parms.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                    parms.CommanderCount.Named("CommanderCount")));
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