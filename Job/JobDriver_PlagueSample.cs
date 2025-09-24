using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_PlagueSample : JobDriver_InteractWithThing
{
    [Unsaved] private CompPlagueSample sampleComp;
    private CompPlagueSample SampleComp => sampleComp ??= job.targetA.Thing?.TryGetComp<CompPlagueSample>();

    protected override float GetTotalWorkAmount(float baseWorkAmount)
    {
        return SampleComp?.MaxSamplePoints ?? 1f;
    }

    protected override void JobTickIntervalAction(int delta)
    {
        SampleComp?.AddSamplePoints(tickWorkAmount * delta);
        base.JobTickIntervalAction(delta);
    }

    protected override void InteractionResult(Pawn pawn)
    {
        SampleComp?.InteractionResult(pawn);
    }
}
