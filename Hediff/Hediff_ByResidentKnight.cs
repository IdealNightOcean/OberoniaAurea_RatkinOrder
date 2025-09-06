using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ByResidentKnight : HediffWithComps
{
    public override HediffStage CurStage => OrderInteractionHandler.ResidentKnightHandler.BuffHediffStage;
}