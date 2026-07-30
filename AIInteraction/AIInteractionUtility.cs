using NightOcean.SimpleAIClient;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class AIInteractionUtility
{
    public static async Task ReplaceMercyQuestTalkText(Quest quest, MercyQuestDef mercyQuestDef)
    {
        if (quest is null || mercyQuestDef is null)
            return;
        if (AIInteractionHandler.Instance is null)
            return;

        QuestPart_LordJob_HelpSeeker helpSeekerPart = quest.PartsListForReading.OfType<QuestPart_LordJob_HelpSeeker>().FirstOrFallback(fallback: null);
        if (helpSeekerPart is null)
            return;

        List<ClientMessage> mercyQuestTalkPrompts = DecoratePromptUtility.GetMercyQuestTalkPrompt(mercyQuestDef).ToList();
        if (mercyQuestTalkPrompts.NullOrEmpty())
            return;

        ServerResponse serverResponse = await AIInteractionHandler.Instance.TryGetStreamChatCompletionsAsync(mercyQuestTalkPrompts);
        if (!serverResponse.IsUsable)
            return;

        QuestState? questState = helpSeekerPart?.quest?.State;
        if (questState == QuestState.Ongoing || questState == QuestState.NotYetAccepted)
        {
            Log.Message("[OARO] 已使用AI生成善行求助对话");
            helpSeekerPart.SetRawTalkText(serverResponse.Content);
        }

        Log.Message("[OARO] 异步任务完成");
    }

    public static async Task SendIncidentConcernLetter(Branch branch, IncidentDef incidentDef, IncidentParms parms)
    {
        if (!branch.IsValid() || incidentDef is null || parms is null)
            return;
        if (AIInteractionHandler.Instance is null)
            return;

        int delayDays = Rand.Range(1, 3);
        List<ClientMessage> incidentConcernPrompts = DecoratePromptUtility.GetIncidentConcernPrompt(branch, incidentDef, parms, delayDays).ToList();
        if (incidentConcernPrompts.NullOrEmpty())
            return;

        ServerResponse serverResponse = await AIInteractionHandler.Instance.TryGetStreamChatCompletionsAsync(incidentConcernPrompts);
        if (!serverResponse.IsUsable)
            return;

        if (branch.IsValid())
        {
            Log.Message("[OARO] 已使用AI生成来自友好分部的慰问");
            OrderLetterUtility.ReceiveLetter(
                label: "OARO_LetterLabel_IncidentConcern".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
                text: serverResponse.Content,
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: branch.RatkinOrder,
                relatedBranch: branch,
                sender: branch.Name,
                delayDays: delayDays,
                relatedLetterType: OrderLetter.RelatedLetterType.Positive
                );
        }

        Log.Message("[OARO] 异步任务完成");
    }

    public static async Task SendMercyQuestAdmireLetter(Branch branch, Quest quest, MercyQuestDef mercyQuestDef)
    {
        if (branch is null || quest is null || mercyQuestDef is null)
            return;
        if (AIInteractionHandler.Instance is null)
            return;

        int delayDays = Rand.Range(1, 5);
        List<ClientMessage> mercyQuestAdmirePrompts = DecoratePromptUtility.GetMercyQuestAdmirePrompt(branch, quest, mercyQuestDef, delayDays).ToList();
        if (mercyQuestAdmirePrompts.NullOrEmpty())
            return;

        ServerResponse serverResponse = await AIInteractionHandler.Instance.TryGetStreamChatCompletionsAsync(mercyQuestAdmirePrompts);
        if (!serverResponse.IsUsable)
            return;

        if (branch.IsValid())
        {
            Log.Message("[OARO] 已使用AI生成来自随机分部的善行赞赏");
            OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
                label: "OARO_LetterLabel_MercyQuestAdmire".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
                text: serverResponse.Content,
                def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
                relatedOrder: branch.RatkinOrder,
                relatedBranch: branch,
                sender: branch.NameColored,
                relatedLetterType: OrderLetter.RelatedLetterType.Positive);
            OrderRecommendation orderRecommendation = RecommendationUtility.MakeRecommendationForPlayer(count: 1);
            orderLetter.AddAttachment(orderRecommendation);
            OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays: delayDays);
        }

        Log.Message("[OARO] 异步任务完成");
    }
}