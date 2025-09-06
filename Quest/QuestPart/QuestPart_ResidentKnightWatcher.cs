using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_ResidentKnightWatcher : QuestPart
{
    public Pawn Knight;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Knight, "Knight");
    }

    public override void Cleanup()
    {
        Knight?.RemoveFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_ResidentKnight);
        Knight = null;
    }
}