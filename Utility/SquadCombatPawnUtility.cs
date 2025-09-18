using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public static class SquadCombatPawnUtility
{
    public static bool TryAssistSupport(this Squad squad, Map map, int member, int commander)
    {
        (List<Pawn> members, List<Pawn> commanders) = GeneratePawns(squad, map, member, commander);
        if (members is null)
        {
            return false;
        }

        squad.PostSquadCombatPawnGenerate(members, commanders, friendly: true);

        if (members.NullOrEmpty())
        {
            return false;
        }

        CombatPawnsArrival(squad, map, friendly: true, members, commanders);

        GetLetterText(squad);

        return true;
    }
    private static (List<Pawn> members, List<Pawn> commanders) GeneratePawns(this Squad squad, Map map, int memberCount, int commanderCount)
    {
        OAFrame_PawnGenerateUtility.TryGetRandomPawnGroupMaker(PawnGroupKindDefOf.Combat, null, out PawnGroupMaker groupMaker);
        if (groupMaker is null || groupMaker.guards.NullOrEmpty())
        {
            return (null, null);
        }

        Faction faction = squad.RatkinOrder.Faction;
        Ideo ideo = faction.ideos.PrimaryIdeo;
        int tile = map.Tile;

        List<Pawn> members = [];

        for (int i = 0; i < memberCount; i++)
        {
            PawnKindDef pawnKind = groupMaker.guards.RandomElementByWeight(g => g.selectionWeight).kind; //改为Default
            PawnGenerationRequest request = PawnUtility.DefaultKnightGenerationRequest(pawnKind, faction, forceNew: false);
            request.FixedIdeo = ideo;
            request.Tile = tile;

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            pawn.SetRatkinOrder(squad.RatkinOrder);

            members.Add(pawn);
        }

        if (members.Count == 0)
        {
            return (null, null);
        }

        List<Pawn> commanders = [];
        if (commanderCount > 0 && !groupMaker.options.NullOrEmpty())
        {
            for (int i = 0; i < commanderCount; i++)
            {
                PawnKindDef pawnKind = groupMaker.options.RandomElementByWeight(g => g.selectionWeight).kind; //改为Default
                PawnGenerationRequest request = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction);
                request.FixedIdeo = ideo;
                request.Tile = tile;

                Pawn pawn = PawnGenerator.GeneratePawn(request);

                commanders.Add(pawn);
            }
        }

        if (commanders.Count == 0)
        {
            return (members, null);
        }
        else
        {
            return (members, commanders);
        }
    }

    private static void CombatPawnsArrival(this Squad squad, Map map, bool friendly, IEnumerable<Pawn> members, IEnumerable<Pawn> commanders)
    {
        Faction faction = squad.RatkinOrder.Faction;
        bool quickMilitaryAid = friendly || (faction is not null && !faction.HostileTo(Faction.OfPlayer));
        bool centerDrop = !quickMilitaryAid && Rand.Chance(0.2f);
        int podOpenDelay = quickMilitaryAid ? 120 : 240;
        IntVec3 spawnCenter;
        Rot4 spawnRotation = Rot4.Random;

        if (centerDrop)
        {
            if (Rand.Chance(0.4f) && map.listerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon))
            {
                spawnCenter = DropCellFinder.TradeDropSpot(map);
            }
            else if (!DropCellFinder.TryFindRaidDropCenterClose(out spawnCenter, map, canRoofPunch: true, allowIndoors: true))
            {
                spawnCenter = DropCellFinder.FindRaidDropCenterDistant(map);
            }
        }
        else
        {
            spawnCenter = DropCellFinder.FindRaidDropCenterDistant(map);
        }

        if (quickMilitaryAid)
        {
            MakeSupportJob(squad, map, spawnCenter, members, commanders);
        }
        else
        {
            MakeAttackJob(squad, map, spawnCenter, members, commanders);
        }

        IEnumerable<Pawn> pawns = members;
        if (commanders is not null)
        {
            pawns.Concat(commanders);
        }

        DropPodUtility.DropThingsNear(spawnCenter, map, pawns.Cast<Thing>(), podOpenDelay, canInstaDropDuringInit: false, leaveSlag: true, quickMilitaryAid, forbid: true, allowFogged: true, faction);
    }

    private static void MakeAttackJob(this Squad squad, Map map, IntVec3 spawnCenter, IEnumerable<Pawn> members, IEnumerable<Pawn> commanders)
    {
        IntVec3 originCell = spawnCenter.IsValid ? spawnCenter : members.FirstOrFallback().PositionHeld;
        RCellFinder.TryFindRandomSpotJustOutsideColony(originCell, map, out IntVec3 result);
        Faction faction = squad.RatkinOrder.Faction;

        LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, result, squad.Branch, isCommander: false), map, members);

        if (commanders is not null)
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, result, squad.Branch, isCommander: true), map, commanders);
        }
    }

    private static void MakeSupportJob(this Squad squad, Map map, IntVec3 spawnCenter, IEnumerable<Pawn> members, IEnumerable<Pawn> commanders)
    {
        IntVec3 originCell = spawnCenter.IsValid ? spawnCenter : members.FirstOrFallback().PositionHeld;
        RCellFinder.TryFindRandomSpotJustOutsideColony(originCell, map, out IntVec3 result);
        Faction faction = squad.RatkinOrder.Faction;

        LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, result, squad.Branch, isCommander: false), map, members);

        if (commanders is not null)
        {
            LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NeverFleeOrder(faction, result, squad.Branch, isCommander: true), map, commanders);
        }
    }

    private static string GetLetterText(this Squad squad)
    {
        Faction faction = squad.RatkinOrder.Faction;
        string text = string.Format(PawnsArrivalModeDefOf.EdgeWalkIn.textEnemy, faction.def.pawnsPlural, faction.Name.ApplyTag(faction)).CapitalizeFirst();
        text += "\n\n";
        text += RaidStrategyDefOf.ImmediateAttackFriendly.arrivalTextEnemy;
        return text;
    }

}