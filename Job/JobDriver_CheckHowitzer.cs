using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_CheckHowitzer : JobDriver_InteractWithThing
{
    protected override void InteractionResult(Pawn pawn)
    {
        Target.TryGetComp<CompSuperHeavyHowitzer>()?.CheckHowitzer(pawn);
    }
}