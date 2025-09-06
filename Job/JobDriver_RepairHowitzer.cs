using OberoniaAurea_Frame;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_RepairHowitzer : JobDriver_InteractWithThing
{
    private Thing Components => TargetThingB;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (base.TryMakePreToilReservations(errorOnFailed))
        {
            return pawn.Reserve(Components, job, 1, -1, null, errorOnFailed);
        }
        return false;
    }

    protected override IEnumerable<Toil> PreInteractToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B);
    }

    protected override void PostJobInitAction()
    {
        Components?.Destroy();
    }

    protected override void InteractionResult(Pawn pawn)
    {
        Target.TryGetComp<CompSuperHeavyHowitzer>()?.RepairHowitzer(pawn);
    }
}