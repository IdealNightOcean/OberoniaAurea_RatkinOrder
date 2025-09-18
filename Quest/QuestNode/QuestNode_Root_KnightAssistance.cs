using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_KnightAssistance_Clergy : QuestNode_Root_KnightAssistanceCommon
{
    protected override void PostPawnGenerated(Pawn pawn)
    {
        base.PostPawnGenerated(pawn);

        Thing medicines = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
        medicines.stackCount = 15;
        pawn.inventory.TryAddAndUnforbid(medicines);
    }
}

public class QuestNode_Root_KnightAssistance_Craftsman : QuestNode_Root_KnightAssistanceCommon
{
    protected override void InitQuestParameter()
    {
        base.InitQuestParameter();
        questParameter.LodgerCount = 2;
    }
}

public class QuestNode_Root_KnightAssistance_PigeonStationKnight : QuestNode_Root_KnightAssistanceCommon
{ }

public class QuestNode_Root_KnightAssistance_Noble : QuestNode_Root_KnightAssistanceCommon
{
    protected override void PostPawnGenerated(Pawn pawn)
    {
        base.PostPawnGenerated(pawn);

        pawn.abilities.GainAbility(AbilityDefOf.SludgeSpew);
    }
}