using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequestWorker_KnightVirtueRequired : AssistanceRequestWorker
{
    public override void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics)
    {
        KnightVirtueDef virtue = null;

        if (dutyAcademics is not null)
        {
            List<KnightVirtueDef> potentialVirtues = new(dutyAcademics.Count);
            for (int i = 0; i < dutyAcademics.Count; i++)
            {
                KnightChivalryDef academicChivalry = dutyAcademics[i].chivalry;
                if (academicChivalry is not null && !academicChivalry.AllKnightVirtues.NullOrEmpty())
                {
                    potentialVirtues.Add(academicChivalry.AllKnightVirtues.RandomElementWithFallback());
                }
            }

            virtue = potentialVirtues.RandomElementWithFallback();
        }

        virtue ??= DefDatabase<KnightVirtueDef>.GetRandom();

        request.Initialize(
            label: "OARO_DutyAssistance_VirtueRequired".Translate(virtue.Named(KeyLibrary_FormatArgName.DEF)),
            reqDesc: "OARO_DutyAssistance_VirtueRequiredDesc".Translate(virtue.Named(KeyLibrary_FormatArgName.DEF))
        );
        request.RelatedVirtue = virtue;
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
                IReadOnlyList<KnightVirtue> virtues = knight.VirtueHandler.Virtues;
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
                    if (virtue.Level >= virtue.Def.MaxLevel)
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
