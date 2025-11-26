using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_AssistanceQuest(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        QuestScriptDef scriptDef = Def.GetModExtension<OrderInteraction_AssistanceQuestExtension>()?.assistanceQuest;
        if (scriptDef is null)
        {
            return (false, false);
        }

        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        slate.Set("map", map);
        bool succeeded = OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, scriptDef, slate, forced: true);
        return (false, true);
    }
}