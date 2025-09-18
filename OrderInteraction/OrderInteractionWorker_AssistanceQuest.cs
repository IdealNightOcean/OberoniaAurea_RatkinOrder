using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_AssistanceQuest(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    public override void InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        QuestScriptDef scriptDef = Def.GetModExtension<OrderInteraction_AssistanceQuestExtension>()?.assistanceQuest;
        if (scriptDef is null)
        {
            return;
        }

        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        slate.Set("map", map);
        OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, scriptDef, slate, forced: true);
    }
}