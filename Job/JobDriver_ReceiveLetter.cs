using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_ReceiveLetter : JobDriver
{
    private const TargetIndex LetterBoxInd = TargetIndex.A;
    private const int Duration = 300;

    protected Building_OrderLetterBox LetterBox => (Building_OrderLetterBox)job.GetTarget(LetterBoxInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(LetterBox, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(LetterBoxInd);
        this.FailOnBurningImmobile(LetterBoxInd);
        this.FailOn(() => !OrderLetterBox.Instance.HasUnreadLetters);
        yield return Toils_Goto.GotoThing(LetterBoxInd, PathEndMode.Touch);
        yield return Toils_General
                        .Wait(Duration).FailOnDestroyedNullOrForbidden(LetterBoxInd).FailOnCannotTouch(LetterBoxInd, PathEndMode.Touch)
                        .FailOn(() => !OrderLetterBox.Instance.HasUnreadLetters)
                        .WithProgressBarToilDelay(LetterBoxInd);
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return toil;
        yield return Toils_General.Do(delegate
        {
            OrderLetterBox.Instance.ReadAllUnreadLetters(LetterBox, false);
        });
    }
}
