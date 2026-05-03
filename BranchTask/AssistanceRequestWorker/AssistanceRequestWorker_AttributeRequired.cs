using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_AttributeRequired : AssistanceRequestWorker
{
    public override AssistanceRequest.RequestType RequestType => AssistanceRequest.RequestType.AttributeRequired;

    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        StatDef stat = new List<StatDef> { StatDefOf.MeleeDPS, StatDefOf.ShootingAccuracyPawn, StatDefOf.MoveSpeed, StatDefOf.WorkSpeedGlobal, StatDefOf.MentalBreakThreshold }.RandomElement();
        float valueRequired = Rand.Range(1.0f, 1.5f);
        request.Initialize(
            type: RequestType,
            title: "OARO_DutyAssistance_AttributeRequired".Translate(stat.LabelCap),
            reqDesc: "OARO_DutyAssistance_AttributeRequiredDesc".Translate(stat.LabelCap, valueRequired.ToStringPercent()),
            ceiling: 100,
            daily: 0f,
            stat: stat,
            statVal: valueRequired
        );
    }

    public override string GenerateRequirementDesc(AssistanceRequest request)
    {
        return "OARO_DutyAssistance_AttributeRequiredDesc".Translate(
            request.RelatedStat.Named(KeyLibrary_FormatArgName.DEF),
            request.StatValueRequired.ToStringPercent().Named(KeyLibrary_FormatArgName.Value));
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
        progress += virtueStat * 1f;
        return progress;
    }
}
