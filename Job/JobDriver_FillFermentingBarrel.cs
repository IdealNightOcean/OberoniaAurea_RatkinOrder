using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_FillFermentingBarrel : JobDriver
{
    private const TargetIndex BarrelInd = TargetIndex.A;

    private const TargetIndex RawInd = TargetIndex.B;

    private const int Duration = 200;

    protected Building_OrderFermentingBarrel Barrel => (Building_OrderFermentingBarrel)job.GetTarget(BarrelInd).Thing;

    protected Thing RawMaterial => job.GetTarget(RawInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(Barrel, job, 1, -1, null, errorOnFailed))
        {
            return pawn.Reserve(RawMaterial, job, 1, -1, null, errorOnFailed);
        }
        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(BarrelInd);
        this.FailOnBurningImmobile(BarrelInd);
        AddEndCondition(() => (Barrel.SpaceLeftForRaw > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);
        yield return Toils_General.DoAtomic(delegate
        {
            job.count = Barrel.SpaceLeftForRaw;
        });
        Toil reserveWort = Toils_Reserve.Reserve(RawInd);
        yield return reserveWort;
        yield return Toils_Goto.GotoThing(RawInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(RawInd).FailOnSomeonePhysicallyInteracting(RawInd);
        yield return Toils_Haul.StartCarryThing(RawInd, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(RawInd);
        yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveWort, RawInd, TargetIndex.None, takeFromValidStorage: true);
        yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.Touch);
        yield return Toils_General.Wait(Duration)
                                  .FailOnDestroyedNullOrForbidden(RawInd)
                                  .FailOnDestroyedNullOrForbidden(BarrelInd)
                                  .FailOnCannotTouch(BarrelInd, PathEndMode.Touch)
                                  .WithProgressBarToilDelay(BarrelInd);
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.initAction = delegate
        {
            Barrel.AddRawMaterial(RawMaterial);
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return toil;
    }
}
