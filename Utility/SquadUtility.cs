using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class SquadUtility
{
    public static void ApplySquadMedalHediffs(Pawn pawn, IEnumerable<(HediffDef, float)> applyHediffs)
    {
        foreach ((HediffDef hediffDef, float severity) in applyHediffs)
        {
            Hediff hediff = pawn.health.GetOrAddHediff(hediffDef);
            hediff.Severity = severity;
        }
    }
}
