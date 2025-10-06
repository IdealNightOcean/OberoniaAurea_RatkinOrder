using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderRaidWorker
{
    protected Branch branch;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;
    public Faction Faction => branch?.RatkinOrder.Faction;

    protected int memberCount;
    protected int commanderCount;

    protected bool IsFriendly => (Faction?.HostileTo(Faction.OfPlayer) is not true);

    public Map map;
    public RaidStrategyDef raidStrategy;
    public PawnsArrivalModeDef raidArrivalMode;

    public RatkinOrderRaidWorker(Branch branch, int memberCount, int commanderCount)
    {
        this.branch = branch;
        this.memberCount = memberCount;
        this.commanderCount = commanderCount;
    }

    public bool TryExecute()
    {
        if (branch is null)
        {
            return false;
        }

        Faction faction = Faction;
        IncidentParms incidentParms = new()
        {
            target = map,
            faction = faction,
            raidStrategy = raidStrategy,
            raidArrivalMode = raidArrivalMode,
        };
        if (!raidArrivalMode.Worker.TryResolveRaidSpawnCenter(incidentParms))
        {
            return false;
        }

        int memberCount = Mathf.Max(0, Mathf.Min(this.memberCount, branch.SquadStat.MemberCountInt));
        int commanderCount = Mathf.Max(0, Mathf.Min(this.commanderCount, branch.SquadStat.CommanderCountInt));
        bool isFriendly = IsFriendly;

        List<Pawn> combatPanws = SquadCombatPawnUtility.GenerateCombatPawns(branch, map, memberCount, commanderCount, isFriendly);
        if (combatPanws.NullOrEmpty())
        {
            return false;
        }
        incidentParms.pawnCount = combatPanws.Count;
        raidArrivalMode.Worker.Arrive(combatPanws, incidentParms);

        if (isFriendly)
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony_NeverFleeOrder(faction), map, combatPanws);
        }
        else
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, incidentParms.spawnCenter), map, combatPanws);
        }

        return true;
    }
}