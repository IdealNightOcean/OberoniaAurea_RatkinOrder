using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_MercyQuestWatcher : QuestNode
{
    public SlateRef<MercyQuestDef> mercyQuestDef;
    public SlateRef<Faction> subFaction;
    public SlateRef<Faction> parentFaction;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            MercyQuestDef = mercyQuestDef.GetValue(slate) ?? slate.Get<MercyQuestDef>(KeyLibrary_SlateStoreAs.mercyQuestDef),
            SubFaction = subFaction.GetValue(slate) ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.subFaction),
            ParentFaction = parentFaction.GetValue(slate) ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.parentFaction),
        };
        QuestGen.quest.AddPart(questPart_MercyQuestWatcher);
    }
}

public class QuestPart_MercyQuestWatcher : QuestPart
{
    private MercyQuestDef mercyQuestDef;
    public MercyQuestDef MercyQuestDef
    {
        get => mercyQuestDef;
        set
        {
            if (value is not null)
            {
                Log.Message(value.defName);
            }
            mercyQuestDef = value;
        }
    }
    public Faction SubFaction;
    public Faction ParentFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        // Scribe_Defs.Look(ref MercyQuestDef, nameof(MercyQuestDef));
        Scribe_References.Look(ref SubFaction, nameof(SubFaction));
        Scribe_References.Look(ref ParentFaction, nameof(ParentFaction));
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (quest.State == QuestState.EndedSuccess)
        {
            MercyQuestHandler.Instance.Notify_MercyQuestSucceed(quest, MercyQuestDef);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        MercyQuestDef = null;
        SubFaction = null;
        ParentFaction = null;
    }
}