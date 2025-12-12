using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_AssistanceQuest(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    protected override void ApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        OrderInteraction_AssistanceQuestExtension modEx_AssistanceQuest = Def.GetModExtension<OrderInteraction_AssistanceQuestExtension>();
        if (modEx_AssistanceQuest is null || modEx_AssistanceQuest.assistanceQuest is null)
        {
            Log.Error($"[OARO] Assistance quest data is missing ({nameof(OrderInteraction_AssistanceQuestExtension)} or {nameof(OrderInteraction_AssistanceQuestExtension)}.{nameof(OrderInteraction_AssistanceQuestExtension.assistanceQuest)} null)");
            return;
        }
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_OrderInteraction_AssistanceQuest_Confirm".Translate(
                ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName),
                modEx_AssistanceQuest.assistantPawnkind.Named("PAWNKIND"),
                modEx_AssistanceQuest.assistantCount.Named(KeyLibrary_FormatArgName.Count)),
            ratkinOrder: ratkinOrder,
            acceptAction: () => base.ApplyInteraction(ratkinOrder, map));
        Find.WindowStack.Add(nodeTree);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        OrderInteraction_AssistanceQuestExtension modEx_AssistanceQuest = Def.GetModExtension<OrderInteraction_AssistanceQuestExtension>();
        if (modEx_AssistanceQuest is null || modEx_AssistanceQuest.assistanceQuest is null)
        {
            return (false, false);
        }

        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        modEx_AssistanceQuest.SetSlateValue(slate);
        slate.Set("map", map);
        bool succeeded = OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, modEx_AssistanceQuest.assistanceQuest, slate, forced: true);
        return (succeeded, true);
    }
}