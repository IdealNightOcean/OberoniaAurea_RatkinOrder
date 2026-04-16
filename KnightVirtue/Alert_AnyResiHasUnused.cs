using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_AnyResidentKnightHasUnusedTraitSlot : Alert
{
    public Alert_AnyResidentKnightHasUnusedTraitSlot()
    {
        defaultLabel = "OARO_Alert_SomeResidentKnightHasUnusedTraitSlot".Translate();
    }

    public override AlertReport GetReport()
    {
        throw new NotImplementedException();
    }
}
