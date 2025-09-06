using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_TakeProductOutOfFermentingBarrel : JobDriver
{
    private const TargetIndex BarrelInd = TargetIndex.A;
    private const TargetIndex ProductToHaulInd = TargetIndex.B;
    private const TargetIndex StorageCellInd = TargetIndex.C;
    private const int Duration = 200;

    protected Building_OrderFermentingBarrel Barrel => (Building_OrderFermentingBarrel)job.GetTarget(BarrelInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Barrel, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(BarrelInd);
        this.FailOnBurningImmobile(BarrelInd);
        yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.Touch);
        yield return Toils_General.Wait(Duration).FailOnDestroyedNullOrForbidden(BarrelInd).FailOnCannotTouch(BarrelInd, PathEndMode.Touch)
            .FailOn(() => !Barrel.Fermented)
            .WithProgressBarToilDelay(BarrelInd);
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.initAction = delegate
        {
            Thing thing = Barrel.TakeOutProduct();
            GenPlace.TryPlaceThing(thing, pawn.Position, Map, ThingPlaceMode.Near);
            StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(thing);
            if (StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, Map, currentPriority, pawn.Faction, out IntVec3 foundCell))
            {
                job.SetTarget(StorageCellInd, foundCell);
                job.SetTarget(ProductToHaulInd, thing);
                job.count = thing.stackCount;
            }
            else
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return toil;
        yield return Toils_Reserve.Reserve(ProductToHaulInd);
        yield return Toils_Reserve.Reserve(StorageCellInd);
        yield return Toils_Goto.GotoThing(ProductToHaulInd, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(ProductToHaulInd);
        Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carryToCell, storageMode: true);
    }
}
