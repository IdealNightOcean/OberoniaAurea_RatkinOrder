using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_HelpSeeker : HediffWithComps
{
    public QuestScriptDef mercyQuest;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref mercyQuest, "mercyQuest");
    }
}
