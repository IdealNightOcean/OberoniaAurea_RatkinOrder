using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_StatValueRequired : AssistanceRequestWorker
{
    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        StatDef stat = new List<StatDef> { StatDefOf.MeleeDPS, StatDefOf.ShootingAccuracyPawn, StatDefOf.MoveSpeed, StatDefOf.WorkSpeedGlobal, StatDefOf.MentalBreakThreshold }.RandomElement();
        float valueRequired = Rand.Range(1.0f, 1.5f);
        request.Initialize(
            label: "OARO_DutyAssistance_StatValueRequired".Translate(stat.Named(KeyLibrary_FormatArgName.DEF)),
            reqDesc: "OARO_DutyAssistance_StatValueRequiredDesc".Translate(stat.Named(KeyLibrary_FormatArgName.DEF), stat.ValueToString(valueRequired).Named(KeyLibrary_FormatArgName.Value))
        );
        request.RelatedStat = stat;
        request.StatValueRequired = valueRequired;
    }

    public override float CalculateDailyProgress(FixedCaravan caravan, AssistanceRequest request)
    {
        float progress = 0f;
        float virtueStat = 0f;
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            virtueStat += pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue);
            progress += 3f;
            if (request.RelatedStat is not null)
            {
                float statValue = pawn.GetStatValue(request.RelatedStat);
                if (statValue >= request.StatValueRequired)
                {
                    progress += 25f;
                    if (statValue >= request.StatValueRequired * 1.5f)
                    {
                        progress += 25f;
                    }
                }
            }
        }
        progress += virtueStat;
        return progress;
    }
}
