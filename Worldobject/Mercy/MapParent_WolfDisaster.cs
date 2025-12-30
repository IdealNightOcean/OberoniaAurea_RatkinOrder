using OberoniaAurea_Frame;
using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MapParent_WolfDisaster : MapParent_Enterable
{
    private Pawn wolf;
    private bool WolfDeadMarked { get; set; }
    private int TicksToNextCheck { get; set; }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref wolf, nameof(wolf));
    }

    public override void PostMapGenerate()
    {
        try
        {
            Map map = Map;
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(map.Center, map, out IntVec3 spawnCell, extraValidator: SpawnCellValidator))
            {
                spawnCell = CellFinder.RandomNotEdgeCell(40, map);
            }
            PawnKindDef timberDef = DefDatabase<PawnKindDef>.GetNamed("Wolf_Timber");
            PawnGenerationRequest request = new(timberDef, tile: map.Tile, forceGenerateNewPawn: true);
            wolf = PawnGenerator.GeneratePawn(request);
            wolf.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_WolfDisaster);
            GenSpawn.Spawn(wolf, spawnCell, map);
            wolf.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, forced: true, forceWake: true);
            Find.LetterStack.ReceiveLetter(
                label: "OARO_Enter_WolfDisasterLabel".Translate(),
                text: "OARO_Enter_WolfDisasterText".Translate(),
                textLetterDef: LetterDefOf.ThreatSmall,
                lookTargets: wolf,
                quest: AssociatedQuest);

            bool SpawnCellValidator(IntVec3 c)
            {
                return c.Standable(map) && map.reachability.CanReachBiggestMapEdgeDistrict(c);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "spawn disaster wolf",
                typeName: nameof(MapParent_WolfDisaster),
                methodName: nameof(PostMapGenerate),
                needStackTrace: true);
        }
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        bool result = base.ShouldRemoveMapNow(out _);
        alsoRemoveWorldObject = result;
        return result;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (WolfDeadMarked)
        {
            return;
        }

        if ((TicksToNextCheck -= delta) <= 0)
        {
            TicksToNextCheck = 250;
            if (!HasMap)
            {
                return;
            }

            CheckWolfState();
        }
    }

    public override void Notify_MyMapAboutToBeRemoved()
    {
        base.Notify_MyMapAboutToBeRemoved();
        CheckWolfState();
    }

    private void CheckWolfState()
    {
        if (WolfDeadMarked) return;

        if (wolf.DestroyedOrNull() || wolf.Dead)
        {
            WolfDeadMarked = true;
            QuestUtility.SendQuestTargetSignals(questTags, "WolfDead");
            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("OARO_Thought_KillDisasterWolf");
            if (thoughtDef is not null)
            {
                foreach (Pawn p in Map.mapPawns.FreeColonistsSpawned)
                {
                    p.needs.mood?.thoughts.memories.TryGainMemory(thoughtDef);
                }
            }
        }
    }
}