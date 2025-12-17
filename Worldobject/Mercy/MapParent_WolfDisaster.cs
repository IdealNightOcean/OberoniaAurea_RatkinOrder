using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MapParent_WolfDisaster : MapParent_Enterable
{
    private Pawn wolf;
    private bool WolfDead { get; set; }
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
            IntVec3 spawnCell = CellFinder.RandomSpawnCellForPawnNear(Map.Center, Map);
            PawnGenerationRequest request = new(OARO_RimWorldDefOf.Wolf_Timber, tile: Map.Tile, forceGenerateNewPawn: true);
            wolf = PawnGenerator.GeneratePawn(request);
            wolf.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_WolfDisaster);
            GenSpawn.Spawn(wolf, spawnCell, Map);
            wolf.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, forced: true, forceWake: true);
            Find.LetterStack.ReceiveLetter(
                label: "OARO_Enter_WolfDisasterLabel".Translate(),
                text: "OARO_Enter_WolfDisasterText".Translate(),
                textLetterDef: LetterDefOf.ThreatSmall,
                lookTargets: wolf,
                quest: AssociatedQuest);
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
        alsoRemoveWorldObject = true;
        if (base.Map.mapPawns.AnyPawnBlockingMapRemoval)
        {
            Log.Message("111");
            return false;
        }

        foreach (PocketMapParent item in Find.World.pocketMaps.ToList())
        {
            if (item.sourceMap == base.Map && item.Map.mapPawns.AnyPawnBlockingMapRemoval)
            {
                Log.Message("222");
                return false;
            }
        }

        if (ModsConfig.OdysseyActive && base.Map.listerThings.AnyThingWithDef(ThingDefOf.GravAnchor))
        {
            Log.Message("333");
            return false;
        }

        if (TransporterUtility.IncomingTransporterPreventingMapRemoval(base.Map))
        {
            Log.Message("444");
            return false;
        }
        Log.Message("555");
        return true;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (WolfDead)
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

            if (wolf.DestroyedOrNull() || wolf.Dead)
            {
                WolfDead = true;
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
}