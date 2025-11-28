using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_KnightAssistance_Clergy : QuestNode_Root_KnightAssistanceCommon
{
    protected override void PostPawnGenerated(Pawn pawn, string lodgerRecruitedSignal)
    {
        base.PostPawnGenerated(pawn, lodgerRecruitedSignal);

        Thing medicines = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
        medicines.stackCount = 15;
        pawn.inventory.TryAddAndUnforbid(medicines);
    }
}