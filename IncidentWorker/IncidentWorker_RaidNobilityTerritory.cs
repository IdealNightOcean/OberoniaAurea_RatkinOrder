using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 叛乱镇压 - 攻击贵族时触发的特定事件（特化类）
/// </summary>
internal sealed class IncidentWorker_RaidNobilityTerritory : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (parms.target is not Map map || map.Parent is not MapParent_NobilityTerritory nobilityTerritory)
        {
            return false;
        }
        Branch branch = nobilityTerritory.Parent?.Branch;
        if (!branch.IsValid())
        {
            return false;
        }
        return true;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Map map || map.Parent is not MapParent_NobilityTerritory nobilityTerritory)
        {
            return false;
        }
        Branch branch = nobilityTerritory.Parent?.Branch;
        if (!branch.IsValid())
        {
            return false;
        }
        if (nobilityTerritory.BranchJoin)
        {
            if (nobilityTerritory.AssociatedQuest.TryGetCliquesManager(addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager))
            {
                foreach (QuestClique clique in cliquesManager.AllCliques.Values)
                {
                    if (clique.IsBranchClique && clique.IsActive)
                    {
                        if (clique.IsFriendlyBranchClique)
                        {
                            BranchSupportUtility.DoCombatKnightSupport(clique.RelatedBranch, map, BranchSupportUtility.DeploymentLevel.Entire, sendStandardLetter: true);
                        }
                        else
                        {
                            BranchSupportUtility.DoCombatKnightSupport(clique.RelatedBranch, map, BranchSupportUtility.DeploymentLevel.Half, sendStandardLetter: true);
                        }
                    }
                }
            }
            else
            {
                BranchSupportUtility.DoCombatKnightSupport(branch, map, BranchSupportUtility.DeploymentLevel.Entire, sendStandardLetter: true);
            }
        }

        Faction enemyFaction = nobilityTerritory.Faction;
        IEnumerable<Pawn> hostilePawns = map.mapPawns.AllHumanlikeSpawned.Where(p => p.Faction == enemyFaction);
        switch (nobilityTerritory.AssaultTypeValue)
        {
            case MapParent_NobilityTerritory.AssaultType.BePounced:
                {
                    foreach (Pawn p in hostilePawns)
                    {
                        p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryInHeat);
                    }

                    Faction playerFaction = Faction.OfPlayer;
                    Faction branchFaction = branch.RatkinOrder.Faction;

                    IEnumerable<Pawn> friendlyPawns = map.mapPawns.AllHumanlikeSpawned.Where(p => p.Faction == playerFaction || p.Faction == branchFaction);
                    foreach (Pawn p in friendlyPawns)
                    {
                        p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryPouncePlayer);
                    }

                    break;
                }
            case MapParent_NobilityTerritory.AssaultType.Normal:
                {
                    foreach (Pawn p in hostilePawns)
                    {
                        p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryInHeat);
                    }

                    break;
                }
            case MapParent_NobilityTerritory.AssaultType.Pounce:
                {
                    foreach (Pawn p in hostilePawns)
                    {
                        p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryPounce);
                    }
                    break;
                }
            case MapParent_NobilityTerritory.AssaultType.DeadlyPounce:
                {
                    foreach (Pawn p in hostilePawns)
                    {
                        Hediff hediff = p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryPounce);
                        hediff.Severity = 2f;

                        if (Rand.Chance(0.2f))
                        {
                            p.mindState.mentalStateHandler.TryStartMentalState(
                                stateDef: MentalStateDefOf.PanicFlee,
                                reason: "OARO_NobilityTerritory_DeadlyPounceScare".Translate(),
                                forced: true,
                                forceWake: true);
                        }
                    }
                    break;
                }
            default: break;
        }

        if (nobilityTerritory.AssaultTypeValue != MapParent_NobilityTerritory.AssaultType.BePounced
            && (nobilityTerritory.Parent.CliquesManager?.IsCliqueActive(nobilityTerritory.Parent.NobilityCivilianCliqueKey) ?? false))
        {
            IncidentParms civilianRaidParms = new()
            {
                target = map,
                faction = nobilityTerritory.Faction,
                pawnGroupKind = PawnGroupKindDefOf.Peaceful,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                points = 5000,
                sendLetter = true,
                customLetterDef = LetterDefOf.ThreatBig,
                customLetterLabel = "OARO_NobilityTerritory_CivilianRaidLabel".Translate(),
                customLetterText = "OARO_NobilityTerritory_CivilianRaidText".Translate(),
                forced = true
            };
            OAFrame_MiscUtility.AddNewQueuedIncident(IncidentDefOf.RaidEnemy, delayTicks: Rand.RangeInclusive(1250, 2500), civilianRaidParms);
        }

        return true;
    }
}