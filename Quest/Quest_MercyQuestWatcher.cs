using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_MercyQuestWatcher : QuestNode
{
    public SlateRef<Faction> subFaction;
    public SlateRef<Faction> parentFaction;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            SubFaction = subFaction.GetValue(slate) ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.SubFaction),
            ParentFaction = parentFaction.GetValue(slate) ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentFaction),
        };
        QuestGen.quest.AddPart(questPart_MercyQuestWatcher);
    }
}

public class QuestPart_MercyQuestWatcher : QuestPart
{
    public Faction SubFaction;
    public Faction ParentFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref SubFaction, "SubFaction");
        Scribe_References.Look(ref ParentFaction, "ParentFaction");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (quest.State == QuestState.EndedSuccess)
        {
            SendSucceedRatkinOrderLetter();
        }
        SubFaction = null;
        ParentFaction = null;
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