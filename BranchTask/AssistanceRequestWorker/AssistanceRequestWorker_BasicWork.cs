using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_BasicWork : AssistanceRequestWorker
{
    public override AssistanceRequest.RequestType RequestType => AssistanceRequest.RequestType.BasicWork;

    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        request.Initialize(
            type: RequestType,
            title: "OARO_DutyAssistance_BasicWork".Translate(),
            reqDesc: "OARO_DutyAssistance_BasicWorkDesc".Translate(),
            ceiling: 100,
            daily: 0f
        );
    }

    public override string GenerateRequirementDesc(AssistanceRequest request)
    {
        return "OARO_DutyAssistance_BasicWorkDesc".Translate();
    }

    public override float CalculateDailyProgress(FixedCaravan caravan, AssistanceRequest request)
    {
        float progress = 0f;
        float virtueStat = 0f;
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            virtueStat += pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue);
            progress += 10f;
        }
        progress += virtueStat * 3f;
        return progress;
    }
}
