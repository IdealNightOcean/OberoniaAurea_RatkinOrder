using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_AssistanceQuest(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    protected override void ApplyEffect(RatkinOrder ratkinOrder, Map map)
    {
        OrderInteraction_AssistanceQuestExtension modEx_AssistanceQuest = Def.GetModExtension<OrderInteraction_AssistanceQuestExtension>();
        if (modEx_AssistanceQuest is null || modEx_AssistanceQuest.assistanceQuest is null)
        {
            Log.Error($"[OARO] 援助任务数据缺失（{nameof(OrderInteraction_AssistanceQuestExtension)} 或 {nameof(OrderInteraction_AssistanceQuestExtension)}.{nameof(OrderInteraction_AssistanceQuestExtension.assistanceQuest)} 为null）");
            return;
        }
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_OrderInteraction_AssistanceQuest_Confirm".Translate(
                ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName),
                modEx_AssistanceQuest.assistantPawnkind.Named("PAWNKIND"),
                modEx_AssistanceQuest.assistantCount.Named(KeyLibrary_FormatArgName.Count)),
            ratkinOrder: ratkinOrder,
            acceptAction: () => base.ApplyEffect(ratkinOrder, map));
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