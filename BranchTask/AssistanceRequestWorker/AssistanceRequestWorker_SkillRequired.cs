using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_SkillRequired : AssistanceRequestWorker
{
    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        SkillDef skill = DefDatabase<SkillDef>.AllDefsListForReading.RandomElement();
        int levelRequired = Rand.RangeInclusive(12, 20);
        request.Initialize(
            label: "OARO_DutyAssistance_SkillRequired".Translate(skill.Named(KeyLibrary_FormatArgName.SKILL)),
            reqDesc: "OARO_DutyAssistance_SkillRequiredDesc".Translate(skill.Named(KeyLibrary_FormatArgName.SKILL), levelRequired.Named(KeyLibrary_FormatArgName.Level))
        );

        request.RelatedSkill = skill;
        request.SkillLevelRequired = levelRequired;
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
        progress += virtueStat;
        return progress;
    }
}
