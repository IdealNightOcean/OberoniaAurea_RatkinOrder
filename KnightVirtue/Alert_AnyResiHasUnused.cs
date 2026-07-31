using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_AnyResidentKnightHasUnusedTraitSlot : Alert
{
    private List<Pawn> KnightsHasUnusedTraitSlot => ResidentPawnsManager.CacheManager?.KnightsHasUnusedTraitSlot.Value;

    public Alert_AnyResidentKnightHasUnusedTraitSlot()
    {
        defaultLabel = "OARO_Alert_SomeResidentKnightHasUnusedTraitSlot".Translate();
    }

    public override AlertReport GetReport()
    {
        AlertReport alertReport = new()
        {
            active = !KnightsHasUnusedTraitSlot.NullOrEmpty(),
            culpritsPawns = !KnightsHasUnusedTraitSlot.NullOrEmpty() ? [.. KnightsHasUnusedTraitSlot] : null
        };

        return alertReport;
    }

    public override TaggedString GetExplanation()
    {
        TaggedString explanation = "OARO_Alert_SomeResidentKnightHasUnusedTraitSlotExp".Translate(GenLabel.ThingsLabel(KnightsHasUnusedTraitSlot).Named(KeyLibrary_FormatArgName.PawnsInfo));
        return explanation;
    }


}
