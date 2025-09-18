using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_BookcaseReading : JobDriver_WatchBuilding
{
    private const TargetIndex BookcaseInd = TargetIndex.A;
    private const TargetIndex CellInd = TargetIndex.B;
    private const TargetIndex BedInd = TargetIndex.C;

    private Book curReadingBook;
    private float readingBonus;

    protected Building Bookcase => (Building)job.targetA.Thing;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.EndOnDespawnedOrNull(BookcaseInd);
        Toil watch;
        if (TargetC.HasThing && TargetC.Thing is Building_Bed)
        {
            this.KeepLyingDown(BedInd);
            yield return Toils_Bed.ClaimBedIfNonMedical(BedInd);
            yield return Toils_Bed.GotoBed(BedInd);
            watch = Toils_LayDown.LayDown(BedInd, hasBed: true, lookForOtherJobs: false);
            watch.AddFailCondition(() => !watch.actor.Awake());
        }
        else
        {
            yield return Toils_Goto.GotoCell(CellInd, PathEndMode.OnCell);
            watch = ToilMaker.MakeToil("WatchToils");
        }

        watch.AddPreInitAction(delegate
        {
            ThingDef bookType = Rand.Bool ? ThingDefOf.Novel : ThingDefOf.TextBook;
            curReadingBook = BookUtility.MakeBook(bookType, ArtGenerationContext.Outsider, QualityGenerator.Gift);

            pawn.pather.StopDead();
            pawn.carryTracker.TryStartCarry(curReadingBook);
            curReadingBook.IsOpen = true;

            readingBonus = BookUtility.GetReadingBonus(pawn);
        });

        watch.AddPreTickIntervalAction(WatchTickAction);

        watch.AddFinishAction(delegate
        {
            curReadingBook?.Destroy();
            curReadingBook = null;
            readingBonus = 0f;

            JoyUtility.TryGainRecRoomThought(pawn);
        });
        watch.defaultCompleteMode = ToilCompleteMode.Delay;
        watch.defaultDuration = job.def.joyDuration;
        watch.handlingFacing = true;
        if (TargetA.Thing.def.building is not null && TargetA.Thing.def.building.effectWatching is not null)
        {
            watch.WithEffect(() => TargetA.Thing.def.building.effectWatching, EffectTargetGetter);
        }

        yield return watch;

        LocalTargetInfo EffectTargetGetter()
        {
            return TargetA.Thing.OccupiedRect().RandomCell + IntVec3.North.RotatedBy(TargetA.Thing.Rotation);
        }
    }

    protected override void WatchTickAction(int delta)
    {
        curReadingBook?.OnBookReadTick(pawn, delta, readingBonus);
        pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f);
        pawn.GainComfortFromCellIfPossible(delta);
        JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob, 1f, Bookcase);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref curReadingBook, "curReadingBook", false);
        Scribe_Values.Look(ref readingBonus, "readingBonus", 0f);
    }
}
