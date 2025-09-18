using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_MercyQuestWatcher : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestGen.quest.AddPart(new QuestPart_MercyQuestWatcher());
    }
}

public class QuestPart_MercyQuestWatcher : QuestPart
{
    public override void Cleanup()
    {
        base.Cleanup();
        if (quest.State == QuestState.EndedSuccess)
        {
            SendSucceedRatkinOrderLetter();
        }
    }

    private static void SendSucceedRatkinOrderLetter()
    {
        GlobalOrderInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.MercyQuestSucceed, 1, addIfMiss: true);
        float letterChance = 0.2f;
        ResidentKnight residentKnight = GlobalOrderInteractionManager.ResidentKnightsManager.GetResidentKnightOfRole(OARO_ModDefOf.OARO_Orderly);
        if (residentKnight is not null)
        {
            letterChance += (residentKnight.RoleDef.RoleWorker as ResidentKnightRoleWorker_Orderly).ExtraMercyQuestLetterChance(residentKnight.Pawn);
        }
        if (Rand.Chance(letterChance))
        {

        }
    }
}