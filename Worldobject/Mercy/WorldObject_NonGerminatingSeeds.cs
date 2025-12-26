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
        if (!caravan.PawnsListForReading.Any(p => p.skills is not null && !p.skills.GetSkill(SkillDefOf.Plants).TotallyDisabled))
        {
            Messages.Message("OAFrame_MissSkillAvailablePawn".Translate(SkillDefOf.Plants.Named(KeyLibrary_FormatArgName.SKILL)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    public override bool StartWork(Caravan caravan)
    {
        if (base.StartWork(caravan))
        {
            Messages.Message("OARO_NonGerminatingSeeds_Arrival".Translate(this.Named(KeyLibrary_FormatArgName.WORLDOBJECT)), MessageTypeDefOf.PositiveEvent);
            return true;
        }
        return false;
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            (Pawn maxPlantsPawn, int maxPlantsSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Plants);

            float successChance = successCurve.Evaluate(maxPlantsSkill);
            if (Rand.Chance(successChance))
            {
                this.SendWorkResolvedSignal();
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                    text: "OARO_NonGerminatingSeeds_Success".Translate(maxPlantsPawn.Named(KeyLibrary_FormatArgName.PAWN)),
                    faction: Faction));
            }
            else
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_NonGerminatingSeeds_Fail".Translate(), Faction));
            }
        }

        this.SafeDestroy();
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_NonGerminatingSeeds_Interrupt".Translate(), Faction));
        this.SafeDestroy();
    }
}
