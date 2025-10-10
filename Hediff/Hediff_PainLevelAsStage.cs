using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_PainLevelAsStage : Hediff
{
    public override int CurStageIndex => pawn.health.hediffSet.PainTotal switch
    {
        < 0.15f => 0,
        < 0.4f => 1,
        < 0.8f => 2,
        _ => 3
    };
}