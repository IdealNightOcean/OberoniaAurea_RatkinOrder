using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public static class SquadUtility
{
    public static List<(HediffDef, float)> GetSquadMedalHediffsToApply(Squad squad)
    {
        List<(HediffDef, float)> applyHediffs = [];
        IReadOnlyList<SquadStat.MedalRecord> medalRecords = squad.SquadStat.MedalRecords;
        HediffDef gainHediff = GetMedalHediffDef(medalRecords[0].type);
        if (gainHediff is not null)
        {
            applyHediffs.Add((gainHediff, 1.5f));
        }
        for (int i = 1; i < medalRecords.Count; i++)
        {
            gainHediff = GetMedalHediffDef(medalRecords[0].type);
            if (gainHediff is not null)
            {
                applyHediffs.Add((gainHediff, 0.5f));
            }
        }
        if (applyHediffs.Count == 0) { return null; }
        else { return applyHediffs; }

        static HediffDef GetMedalHediffDef(SquadStat.SquadMedal type)
        {
            return type switch
            {
                SquadStat.SquadMedal.Tenacity => HediffDefOf.CubeRage,
                SquadStat.SquadMedal.Courage => HediffDefOf.CubeRage,
                SquadStat.SquadMedal.Intervene => HediffDefOf.CubeRage,
                SquadStat.SquadMedal.Justice => HediffDefOf.CubeRage,
                _ => null,
            };
        }
    }

    public static void ApplySquadMedalHediffs(Pawn pawn, List<(HediffDef, float)> applyHediffs)
    {
        foreach ((HediffDef hediffDef, float severity) in applyHediffs)
        {
            Hediff hediff = pawn.health.GetOrAddHediff(hediffDef);
            hediff.Severity = severity;
        }
    }

    public static string GenerateSquadName(Branch branch)
    {
        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_ModDefOf.OARO_NamerOrderSquad }
        };
        grammarRequest.Rules.Add(new Rule_String("branchName", branch.Name));

        return GenText.CapitalizeAsTitle(GrammarResolver.Resolve("r_name", grammarRequest));
    }
}
