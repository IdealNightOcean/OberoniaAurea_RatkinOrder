using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_SkillRequired : AssistanceRequestWorker
{
    public override AssistanceRequest.RequestType RequestType => AssistanceRequest.RequestType.SkillRequired;

    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        SkillDef skill = DefDatabase<SkillDef>.AllDefsListForReading.RandomElement();
        int levelRequired = Rand.RangeInclusive(12, 20);
        request.Initialize(
            type: RequestType,
            title: "OARO_DutyAssistance_SkillRequired".Translate(skill.LabelCap),
            reqDesc: "OARO_DutyAssistance_SkillRequiredDesc".Translate(skill.LabelCap, levelRequired),
            ceiling: 100,
            daily: 0f,
            skill: skill,
            skillLvl: levelRequired
        );
    }

    public override string GenerateRequirementDesc(AssistanceRequest request)
    {
        return "OARO_DutyAssistance_SkillRequiredDesc".Translate(request.RelatedSkill.Named(KeyLibrary_FormatArgName.SKILL),
            request.SkillLevelRequired.Named(KeyLibrary_FormatArgName.Level));
    }

    public override float CalculateDailyProgress(FixedCaravan caravan, AssistanceRequest request)
    {
        float progress = 0f;
        float virtueStat = 0f;
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            virtueStat += pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue);
            progress += 3f;
            if (request.RelatedSkill is not null)
            {
                int skillLevel = pawn.GetSkillLevel(request.RelatedSkill);
                if (skillLevel >= request.SkillLevelRequired)
                {
                    progress += 25f;
                    if (skillLevel >= 20)
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
