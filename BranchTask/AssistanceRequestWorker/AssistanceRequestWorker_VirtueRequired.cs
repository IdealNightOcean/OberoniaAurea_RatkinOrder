using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_VirtueRequired : AssistanceRequestWorker
{
    public override AssistanceRequest.RequestType RequestType => AssistanceRequest.RequestType.VirtueRequired;

    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        KnightVirtueDef virtue = null;
        if (dutyAcademics is not null)
        {
            for (int i = 0; i < dutyAcademics.Count; i++)
            {
                KnightAcademicDef academic = dutyAcademics[i];
                if (academic?.chivalry is not null)
                {
                    List<KnightVirtueDef> allVirtues = DefDatabase<KnightVirtueDef>.AllDefsListForReading;
                    for (int j = 0; j < allVirtues.Count; j++)
                    {
                        if (allVirtues[j].chivalry == academic.chivalry)
                        {
                            virtue = allVirtues[j];
                            break;
                        }
                    }
                    if (virtue is not null) break;
                }
            }
        }
        if (virtue is null)
        {
            List<KnightVirtueDef> allVirtues = DefDatabase<KnightVirtueDef>.AllDefsListForReading;
            if (allVirtues.Count > 0)
            {
                virtue = allVirtues[Rand.Range(0, allVirtues.Count)];
            }
        }
        request.Initialize(
            type: RequestType,
            title: "OARO_DutyAssistance_VirtueRequired".Translate(virtue?.LabelCap ?? ""),
            reqDesc: "OARO_DutyAssistance_VirtueRequiredDesc".Translate(virtue?.LabelCap ?? ""),
            ceiling: 100,
            daily: 0f,
            virtue: virtue
        );
    }

    public override string GenerateRequirementDesc(AssistanceRequest request)
    {
        return "OARO_DutyAssistance_VirtueRequiredDesc".Translate(request.RelatedVirtue?.LabelCap ?? "");
    }

    public override float CalculateDailyProgress(FixedCaravan caravan, AssistanceRequest request)
    {
        float progress = 0f;
        float virtueStat = 0f;
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            virtueStat += pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue);
            progress += 3f;
            if (request.RelatedVirtue is not null && ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight knight))
            {
                KnightVirtue virtue = null;
                IReadOnlyList<KnightVirtue> virtues = knight.KnightVirtueHandler.Virtues;
                for (int i = 0; i < virtues.Count; i++)
                {
                    if (virtues[i].Def == request.RelatedVirtue)
                    {
                        virtue = virtues[i];
                        break;
                    }
                }
                if (virtue is not null)
                {
                    progress += 100f;
                    if (virtue.Level >= virtue.Def.maxLevel)
                    {
                        progress += 100f;
                    }
                }
            }
        }
        progress += virtueStat * 1f;
        return progress;
    }
}
