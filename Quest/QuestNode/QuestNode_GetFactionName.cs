using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetFactionName : QuestNode
{
    [NoTranslate]
    public SlateRef<string> storeAs;

    public SlateRef<Faction> faction;
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        string name = faction.GetValue(QuestGen.slate)?.Name;
        if (name is not null && storeAs.GetValue(QuestGen.slate) is not null)
        {
            QuestGen.slate.Set(storeAs.GetValue(QuestGen.slate), name);
        }
    }
}
