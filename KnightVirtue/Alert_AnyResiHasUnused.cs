using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_AnyResidentKnightHasUnusedTraitSlot : Alert
{
    private static readonly List<Pawn> knightsHasUnusedTraitSlot = new(4);
    private static int nextUpdateTick = -1;
    private static List<Pawn> KnightsHasUnusedTraitSlot
    {
        get
        {
            if (Find.TickManager.TicksGame > nextUpdateTick)
            {
                RefreshKnightsApproachingResignation();
            }
            return knightsHasUnusedTraitSlot;
        }
    }

    public Alert_AnyResidentKnightHasUnusedTraitSlot()
    {
        defaultLabel = "OARO_Alert_SomeResidentKnightHasUnusedTraitSlot".Translate();
    }

    public override AlertReport GetReport()
    {
        AlertReport alertReport = new()
        {
            active = KnightsHasUnusedTraitSlot.Count > 0,
            culpritsPawns = KnightsHasUnusedTraitSlot
        };

        return alertReport;
    }

    public override TaggedString GetExplanation()
    {
        TaggedString explanation = "OARO_Alert_SomeResidentKnightHasUnusedTraitSlotExp".Translate(GenLabel.ThingsLabel(KnightsHasUnusedTraitSlot).Named(KeyLibrary_FormatArgName.PawnsInfo));
        return explanation;
    }

    private static void RefreshKnightsApproachingResignation()
    {
        nextUpdateTick = Find.TickManager.TicksGame + 2500;
        knightsHasUnusedTraitSlot.Clear();

        IReadOnlyList<ResidentKnight> residentKnights = ResidentPawnsManager.Instance.ResidentKnights;
        if (residentKnights.Count <= 0)
            return;

        foreach (ResidentKnight record in residentKnights)
        {
            if (record.KnightVirtueHandler.HasUnusedTraitSlot)
            {
                knightsHasUnusedTraitSlot.Add(record.Pawn);
            }
        }
    }
}
