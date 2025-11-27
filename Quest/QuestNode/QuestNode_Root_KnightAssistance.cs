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

public class QuestNode_Root_KnightAssistance_Craftsman : QuestNode_Root_KnightAssistanceCommon
{
    protected override bool InitQuestParameter()
    {
        if (!base.InitQuestParameter())
        {
            return false;
        }

        questParameter.LodgerCount = 2;
        return true;
    }
}

public class QuestNode_Root_KnightAssistance_PigeonStationKnight : QuestNode_Root_KnightAssistanceCommon
{ }

public class QuestNode_Root_KnightAssistance_Noble : QuestNode_Root_KnightAssistanceCommon
{
    protected override void PostPawnGenerated(Pawn pawn, string lodgerRecruitedSignal)
    {
        base.PostPawnGenerated(pawn, lodgerRecruitedSignal);

        pawn.abilities.GainAbility(AbilityDefOf.SludgeSpew);
    }
}