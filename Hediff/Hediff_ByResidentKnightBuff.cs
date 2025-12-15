using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ByResidentKnightBuff : HediffWithComps
{
    public override HediffStage CurStage => ResidentKnightsManager.Instance.BuffHediffStage;
}