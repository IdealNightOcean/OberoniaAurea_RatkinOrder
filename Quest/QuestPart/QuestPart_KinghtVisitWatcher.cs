using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_KnightVisitWatcher : QuestPart
{
    public int KnightCount;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref KnightCount, "KnightCount", 0);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (quest.State == QuestState.EndedSuccess)
        {
            GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.EntertainedKnights, KnightCount, addIfMiss: true);
        }
    }
}
