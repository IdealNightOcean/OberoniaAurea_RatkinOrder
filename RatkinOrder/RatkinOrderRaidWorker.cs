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
    protected float supplyCost;

    protected bool IsFriendly => (Faction?.HostileTo(Faction.OfPlayer) is not true);

    public Map map;
    public RaidStrategyDef raidStrategy;
    public PawnsArrivalModeDef raidArrivalMode;
    public bool sendStandardLetter = true;

    public List<Pawn> combatPanws;

    public RatkinOrderRaidWorker(Branch branch, int memberCount, int commanderCount, float supplyCost)
    {
        this.branch = branch;
        this.memberCount = memberCount;
        this.commanderCount = commanderCount;
        this.supplyCost = supplyCost;
    }

    public bool TryExecute()
    {
        if (branch is null)
        {
            return false;
        }

        Faction faction = Faction;
        bool isFriendly = IsFriendly;
        PawnsArrivalModeDef raidArrivalMode = this.raidArrivalMode ?? (isFriendly ? PawnsArrivalModeDefOf.EdgeDrop : PawnsArrivalModeDefOf.EdgeWalkIn);
        IncidentParms incidentParms = new()
        {
            target = map,
            faction = faction,
            raidStrategy = raidStrategy ?? (isFriendly ? RaidStrategyDefOf.ImmediateAttackFriendly : RaidStrategyDefOf.ImmediateAttack),
            raidArrivalMode = raidArrivalMode,
        };
        if (!raidArrivalMode.Worker.TryResolveRaidSpawnCenter(incidentParms))
        {
            return false;
        }

        memberCount = Mathf.Max(0, Mathf.Min(memberCount, branch.Squad.MemberCountInt));
        commanderCount = Mathf.Max(0, Mathf.Min(commanderCount, branch.Squad.CommanderCountInt));

        combatPanws = SquadCombatPawnUtility.GenerateCombatPawns(branch, map, memberCount, commanderCount, isFriendly);
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
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            map.StoryState.lastRaidFaction = faction;
        }

        branch.Squad.MemberCount -= memberCount;
        branch.Squad.CommanderCount -= commanderCount;
        branch.Supply -= supplyCost;

        if (sendStandardLetter)
        {
            ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                label: GetLetterLabel(incidentParms, isFriendly),
                text: GetLetterText(incidentParms, combatPanws, isFriendly),
                def: isFriendly ? OARO_LetterDefOf.OARO_Order_PositiveLetter : OARO_LetterDefOf.OARO_Order_ThreatBigLetter,
                relatedFaction: faction,
                lookTargets: combatPanws);
            letter.relatedOrder = branch.RatkinOrder;
            Find.LetterStack.ReceiveLetter(letter);
        }

        return true;
    }

    protected string GetLetterLabel(IncidentParms parms, bool isFriendly)
    {
        return (isFriendly ? parms.raidStrategy.letterLabelFriendly : RaidStrategyDefOf.ImmediateAttack.letterLabelEnemy)
               + ": " + branch.Name;
    }

    protected string GetLetterText(IncidentParms parms, List<Pawn> pawns, bool isFriendly)
    {
        string text;
        if (isFriendly)
        {
            text = string.Format(parms.raidArrivalMode.textFriendly, parms.faction.def.pawnsPlural, parms.faction.Name.ApplyTag(parms.faction));
            text += "\n\n";
            text += "OARO_RaidText_BranchInfo".Translate(branch.Name, commanderCount).Resolve();
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextFriendly;
            Pawn pawn = pawns.Find(p => p.Faction.leader == p);
            if (pawn is not null)
            {
                text += "\n\n";
                text += "FriendlyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER"));
            }
        }
        else
        {
            text = string.Format(parms.raidArrivalMode.textEnemy, parms.faction.def.pawnsPlural, parms.faction.Name.ApplyTag(parms.faction)).CapitalizeFirst();
            text += "\n\n";
            text += "OARO_RaidText_BranchInfo".Translate(branch.Name, commanderCount).Resolve();
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextEnemy;
            Pawn pawn = pawns.Find(p => p.Faction.leader == p);
            if (pawn is not null)
            {
                text += "\n\n";
                text += "EnemyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER")).Resolve();
            }
            if (parms.raidAgeRestriction != null && !parms.raidAgeRestriction.arrivalTextExtra.NullOrEmpty())
            {
                text += "\n\n";
                text += parms.raidAgeRestriction.arrivalTextExtra.Formatted(parms.faction.def.pawnsPlural.Named("PAWNSPLURAL")).Resolve();
            }
        }
        return text;
    }
}