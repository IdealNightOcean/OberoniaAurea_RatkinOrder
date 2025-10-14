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
        if (nobilityTerritory.Parent is null)
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
        if (nobilityTerritory.Parent is null)
        {
            return false;
        }
        if (nobilityTerritory.BranchJoin)
        {
            BranchSupportUtility.DoCombatSupport(nobilityTerritory.Parent.Branch, BranchSupportUtility.SupportLevel.Entire, map);
        }

        if (nobilityTerritory.AssaultTypeValue == MapParent_NobilityTerritory.AssaultType.BePounced)
        {
            Faction playerFaction = Faction.OfPlayer;
            Faction branchFaction = nobilityTerritory.Parent.Branch.RatkinOrder.Faction;

            IEnumerable<Pawn> friendlyPawns = map.mapPawns.AllHumanlikeSpawned.Where(p => p.Faction == playerFaction || p.Faction == branchFaction);
            foreach (Pawn p in friendlyPawns)
            {
                p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryPouncePlayer);
            }
        }

        Faction enemyFaction = nobilityTerritory.Faction;
        IEnumerable<Pawn> hostilePawns = map.mapPawns.AllHumanlikeSpawned.Where(p => p.Faction == enemyFaction);
        if (nobilityTerritory.AssaultTypeValue == MapParent_NobilityTerritory.AssaultType.Pounce)
        {
            foreach (Pawn p in hostilePawns)
            {
                p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryPounce);
            }
        }

        if (nobilityTerritory.AssaultTypeValue == MapParent_NobilityTerritory.AssaultType.DeadlyPounce)
        {
            foreach (Pawn p in hostilePawns)
            {
                Hediff hediff = p.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_NobilityTerritoryPounce);
                hediff.Severity = 2f;

                if (Rand.Chance(0.2f))
                {
                    //  p.mindState.mentalBreaker.TryDoMentalBreak("OARO_NobilityTerritory_DeadlyPounceScare".Translate());
                }
            }
        }

        if (nobilityTerritory.AssaultTypeValue != MapParent_NobilityTerritory.AssaultType.BePounced && nobilityTerritory.Parent.CliquesManager.IsCliqueActive(nobilityTerritory.Parent.NobilityCivilianCliqueKey))
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