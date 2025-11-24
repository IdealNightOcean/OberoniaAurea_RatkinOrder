using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderCombatParameter
{
    private Branch branch;
    private Map map;
    public Branch Branch => branch;
    public Map Map => map;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;
    public Faction Faction => branch?.RatkinOrder.Faction;

    public int MemberCount;
    public int CommanderCount;
    public int NonKnightCount;
    public float SupplyCost;

    public bool IsFriendly => (Faction?.HostileTo(Faction.OfPlayer) is not true);

    public RaidStrategyDef RaidStrategy;
    public PawnsArrivalModeDef RaidArrivalMode;
    public bool SendStandardLetter = true;

    public RatkinOrderCombatParameter() { }
    public RatkinOrderCombatParameter(Branch branch, Map map)
    {
        this.branch = branch;
        this.map = map;
    }

    public bool TryExecute()
    {
        if (branch is null)
        {
            return false;
        }

        Faction faction = Faction;
        bool isFriendly = IsFriendly;
        PawnsArrivalModeDef raidArrivalMode = RaidArrivalMode ?? (isFriendly ? PawnsArrivalModeDefOf.EdgeDrop : PawnsArrivalModeDefOf.EdgeWalkIn);
        IncidentParms incidentParms = new()
        {
            target = Map,
            faction = faction,
            raidStrategy = RaidStrategy ?? (isFriendly ? RaidStrategyDefOf.ImmediateAttackFriendly : RaidStrategyDefOf.ImmediateAttack),
            raidArrivalMode = raidArrivalMode,
        };
        if (!raidArrivalMode.Worker.TryResolveRaidSpawnCenter(incidentParms))
        {
            return false;
        }

        MemberCount = Mathf.Max(0, Mathf.Min(MemberCount, branch.Squad.MemberCountInt));
        CommanderCount = Mathf.Max(0, Mathf.Min(CommanderCount, branch.Squad.CommanderCountInt));

        List<Pawn> combatPanws = SquadCombatPawnUtility.GenerateCombatPawns(this);
        if (combatPanws.NullOrEmpty())
        {
            return false;
        }
        incidentParms.pawnCount = combatPanws.Count;
        raidArrivalMode.Worker.Arrive(combatPanws, incidentParms);

        if (isFriendly)
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony_NeverFleeOrder(faction), Map, combatPanws);
        }
        else
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, incidentParms.spawnCenter), Map, combatPanws);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            Map.StoryState.lastRaidFaction = faction;
        }

        branch.Squad.AdjustCrew(member: -MemberCount, commander: -CommanderCount);
        branch.Supply -= SupplyCost;

        if (SendStandardLetter)
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
            text += "OARO_RaidText_BranchInfo".Translate(branch.Name, CommanderCount).Resolve();
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
            text += "OARO_RaidText_BranchInfo".Translate(branch.Name, CommanderCount).Resolve();
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextEnemy;
            Pawn pawn = pawns.Find(p => p.Faction.leader == p);
            if (pawn is not null)
            {
                text += "\n\n";
                text += "EnemyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER")).Resolve();
            }
            if (parms.raidAgeRestriction != null && !string.IsNullOrEmpty(parms.raidAgeRestriction.arrivalTextExtra))
            {
                text += "\n\n";
                text += parms.raidAgeRestriction.arrivalTextExtra.Formatted(parms.faction.def.pawnsPlural.Named("PAWNSPLURAL")).Resolve();
            }
        }
        return text;
    }
}