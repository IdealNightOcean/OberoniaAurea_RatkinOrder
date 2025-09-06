using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 不发芽种子村庄（特化类）
/// </summary>
public sealed class WorldObject_NonGerminatingSeeds : WorldObject_InteractWithFixedCaravan_Nameable
{
    private static readonly SimpleCurve successCurve = new([new CurvePoint(4, 0), new CurvePoint(12, 1)]);
    public override int TicksNeeded => 180000;
    public override string FixedCaravanName => "OARO_FixedCaravan_NonGerminatingSeeds".Translate();
    public override string FixedCaravanWorkDesc() => "OARO_NonGerminatingSeeds_TimeLeft".Translate(ticksRemaining.ToStringTicksToPeriod());

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Plants) < 0)
        {
            Messages.Message("OARO_NoOneCanDo".Translate(SkillDefOf.Plants.label), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            (Pawn maxPlantsPawn, int maxPlantsSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Plants);

            float successChance = successCurve.Evaluate(maxPlantsSkill);
            if (Rand.Chance(successChance))
            {
                SendWorkResolvedSignal();
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_NonGerminatingSeeds_Success".Translate(maxPlantsPawn)));
            }
            else
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_NonGerminatingSeeds_Fail".Translate()));
            }
        }

        if (!Destroyed)
        {
            Destroy();
        }
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_NonGerminatingSeeds_Interrupt".Translate()));
        if (!Destroyed)
        {
            Destroy();
        }
    }
}
