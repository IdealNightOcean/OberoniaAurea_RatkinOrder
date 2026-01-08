using NightOcean.SimpleAIClient;
using NightOcean.SimpleAIClient.OpenAI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class AIInteractionHandler
{
    public static AIInteractionHandler Instance { get; private set; }

    public bool IsBusy { get; private set; }
    private Game HandleGame { get; }

    private Client Client { get; }

    private static HttpClient httpClient;
    public static HttpClient HttpClient => httpClient ??= new HttpClient();

    public bool CurGameValid => Current.Game is not null && Current.Game == HandleGame;

    public AIInteractionHandler()
    {
        Instance = this;
        HandleGame = Current.Game;
        Client = new(endPointURL: "https://api.siliconflow.cn/v1/chat/completions",
                     model: "Qwen/Qwen3-235B-A22B-Instruct-2507",
                     apiKey: "");
    }
    public static void ClearStaticCache() => Instance = null;

    public async Task ReplaceMercyQuestTalkText(Quest quest, MercyQuestDef mercyQuestDef)
    {
        if (quest is null || mercyQuestDef is null)
        {
            return;
        }

        QuestPart_LordJob_HelpSeeker helpSeekerPart = quest.PartsListForReading.OfType<QuestPart_LordJob_HelpSeeker>().FirstOrFallback(fallback: null);
        if (helpSeekerPart is null)
        {
            return;
        }

        List<RequestMessage> mercyQuestTalkPrompts = DecoratePrompt.MercyQuestTalkPrompt(mercyQuestDef);
        Log.Message("呼唤API");
        TextResponse textResponse = await Client.StreamChatCompletionsAsync(HttpClient, mercyQuestTalkPrompts).ConfigureAwait(continueOnCapturedContext: true);

        if (!CurGameValid)
        {
            Log.Error("Game已经切换");
            return;
        }
        if(string.IsNullOrEmpty(textResponse.ContentReceived))
        {
            Log.Error("回复无效");
            return;
        }
        QuestState? questState = helpSeekerPart?.quest?.State;
        if (questState == QuestState.Ongoing || questState == QuestState.NotYetAccepted)
        {
            Log.Message("已使用AI生成重设对话");
            helpSeekerPart.SetRawTalkText(textResponse.ContentReceived);
        }
    }
}

public static class DecoratePrompt
{
    public static List<RequestMessage> MercyQuestTalkPrompt(MercyQuestDef mercyQuestDef)
    {
        if (mercyQuestDef is null)
        {
            return null;
        }
        List<RequestMessage> messages = new(2);
        StringBuilder promptBuilder = new(256);
        promptBuilder.AppendLine(@"
你是一个背景故事生成器，请以“求助村民”的第一人称视角，根据user提供的类型和示例，生成一段简短、恳切的求助背景描述。

设定如下：
1. 求助者来自村庄 {SUBFACTION_name}；
2. 他/她误以为玩家派系是“善良爱民的骑士”，因此鼓起勇气前来求助；
3. 此时，求助者已抵达玩家基地附近，正焦急等待回应。

写作要求：
1. 内容需包含：  
   - 简要说明遭遇的困境（如野兽侵扰、粮食短缺、亲人失踪等）；  
   - 表达对玩家的信赖与恳求帮助的急迫心情。
2. 语言简朴平实，句式短促，避免复杂修辞、方言或俚语；
3. 语气应真诚，带有 desperation，但不夸张煽情；
4. 全文严格控制在 **300个汉字以内**。".Trim());

        promptBuilder.AppendLine("占位符（可选择性使用列出的占位符，不可使用未列出的占位符）：");
        promptBuilder.AppendLine("1. {HELPSEEKER_nameDef}：求助者的姓名");
        promptBuilder.AppendLine("2. {SUBFACTION_name}：求助村庄名称");
        if (mercyQuestDef.hasParentFaction)
        {
            promptBuilder.AppendLine("3. {PARENTFACTION_name}：主要参与派系名称");
        }
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("请直接输出生成的文本，不要添加解释、标题或额外内容。");

        messages.Add(new RequestMessage(RequestMessage.RoleEnum.system, promptBuilder.ToString()));

        promptBuilder.Clear();
        promptBuilder.AppendLine($"求助类型: {mercyQuestDef.LabelCap}");
        if (string.IsNullOrEmpty(mercyQuestDef.fixedHelpDesc))
        {
            promptBuilder.AppendLine("通用示例: ");
            promptBuilder.AppendLine("骑士大人们，我是{SUBFACTION_name}的{HELPSEEKER_nameDef}，求求你们帮帮我们吧！[求助原因]……听说你们是善良爱民的骑士，才会鼓起勇气来找你们。我……我真怕撑不到明天了。求你们去看看吧，哪怕只派一个人也好！");
        }
        else
        {
            promptBuilder.AppendLine($"示例: ");
            promptBuilder.AppendLine(mercyQuestDef.fixedHelpDesc);
        }

        messages.Add(new RequestMessage(RequestMessage.RoleEnum.user, promptBuilder.ToString()));

        return messages;
    }
}