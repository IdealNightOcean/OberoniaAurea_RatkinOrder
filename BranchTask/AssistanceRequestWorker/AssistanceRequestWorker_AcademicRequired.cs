using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_AcademicRequired : AssistanceRequestWorker
{

    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        KnightAcademicDef academic = dutyAcademics?.RandomElementWithFallback(null)
                                     ?? DefDatabase<KnightAcademicDef>.AllDefsListForReading.RandomElement();
        request.Initialize(
            label: "OARO_DutyAssistance_AcademicRequired".Translate(academic.Named(KeyLibrary_FormatArgName.DEF)),
            reqDesc: "OARO_DutyAssistance_AcademicRequiredDesc".Translate(academic.Named(KeyLibrary_FormatArgName.DEF))
        );

        request.RelatedAcademic = academic;
    }

    public override float CalculateDailyProgress(FixedCaravan fixedCaravan, AssistanceRequest request)
    {
        float progress = 0f;
        float virtueStat = 0f;
        foreach (Pawn pawn in fixedCaravan.PawnsListForReading)
        {
            virtueStat += pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue);
            progress += 3f;
            if (request.RelatedAcademic is not null && ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight knight))
            {
                bool hasAcademic = false;
                bool hasCompleted = false;
                IReadOnlyList<KnightVirtue> virtues = knight.VirtueHandler.Virtues;
                for (int i = 0; i < virtues.Count; i++)
                {
                    if (virtues[i].Def.relatedAcademicDef == request.RelatedAcademic)
                    {
                        hasAcademic = true;
                        if (virtues[i].Level >= virtues[i].Def.maxLevel)
                        {
                            hasCompleted = true;
                        }
                    }
                }
                if (hasAcademic)
                {
                    progress += 25f;
                }
                if (hasCompleted)
                {
                    progress += 25f;
                }
            }
        }
        progress += virtueStat;
        return progress;
    }
}
