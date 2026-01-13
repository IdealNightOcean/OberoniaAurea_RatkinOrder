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

using static ServerResponse;

internal class AIInteractionHandler
{
    private static HttpClient httpClient;
    public static HttpClient HttpClient => httpClient ??= new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(90d)
    };

    public static AIInteractionHandler Instance { get; private set; }

    public bool IsBusy { get; private set; }
    private Game HandleGame { get; }

    private Client Client { get; set; }

    public static int GameTotalTokensUsed { get; set; }
    public int TotalTokensUsed { get; set; }

    public bool CurGameValid => Current.Game is not null && Current.Game == HandleGame;

    public AIInteractionHandler()
    {
        Instance = this;
        HandleGame = Current.Game;
    }
    public static void ClearStaticCache() => Instance = null;

    private async Task<Client> GetAIClient()
    {
        if (Client is null)
        {
            try
            {
                Client = new Client(
                    endPointURL: RatkinOrderSettings.AIServiceUrl,
                    model: RatkinOrderSettings.AIModelName,
                    apiKey: RatkinOrderSettings.APIKey);
            }
            catch (Exception ex1)
            {
                Log.Error($"[OARO] AI客户端更新失败：{ex1.Message}\n{ex1.StackTrace}");
                Messages.Message("OARO_Message_AIConfigError".Translate(), MessageTypeDefOf.NegativeEvent, historical: false);
                Client = null;
            }
        }
        else
        {
            try
            {
                Client.SetClientConfig(
                    endPointURL: RatkinOrderSettings.AIServiceUrl,
                    model: RatkinOrderSettings.AIModelName,
                    apiKey: RatkinOrderSettings.APIKey
                );
            }
            catch (Exception ex2)
            {
                Log.Error($"[OARO] AI客户端更新失败：{ex2.Message}\n{ex2.StackTrace}");
                Messages.Message("OARO_Message_AIConfigError".Translate(), MessageTypeDefOf.NegativeEvent, historical: false);
                Client = null;
            }
        }

        return Client;
    }

    public async Task ReplaceMercyQuestTalkText(Quest quest, MercyQuestDef mercyQuestDef)
    {
        if (quest is null || mercyQuestDef is null)
            return;

        QuestPart_LordJob_HelpSeeker helpSeekerPart = quest.PartsListForReading.OfType<QuestPart_LordJob_HelpSeeker>().FirstOrFallback(fallback: null);
        if (helpSeekerPart is null)
            return;

        List<ClientMessage> mercyQuestTalkPrompts = DecoratePrompt.GetMercyQuestTalkPrompt(mercyQuestDef).ToList();
        if (mercyQuestTalkPrompts.NullOrEmpty())
            return;

        ServerResponse serverResponse = await TryGetStreamChatCompletionsAsync(mercyQuestTalkPrompts);
        if (!serverResponse.IsUsable)
        {
            return;
        }

        QuestState? questState = helpSeekerPart?.quest?.State;
        if (questState == QuestState.Ongoing || questState == QuestState.NotYetAccepted)
        {
            Log.Message("[OARO] 已使用AI生成善行求助对话");
            helpSeekerPart.SetRawTalkText(serverResponse.Content);
        }

        Log.Message("[OARO] 异步任务完成");
    }

    public async Task SendIncidentConcernLetter(Branch branch, IncidentDef incidentDef, IncidentParms parms)
    {
        if (!branch.IsValid() || incidentDef is null || parms is null)
            return;

        int delayDays = Rand.Range(1, 3);
        List<ClientMessage> incidentConcernPrompts = DecoratePrompt.GetIncidentConcernPrompt(branch, incidentDef, parms, delayDays).ToList();
        if (incidentConcernPrompts.NullOrEmpty())
            return;

        ServerResponse serverResponse = await TryGetStreamChatCompletionsAsync(incidentConcernPrompts);
        if (!serverResponse.IsUsable)
        {
            return;
        }

        if (branch.IsValid())
        {
            Log.Message("[OARO] 已使用AI生成来自友好分部的慰问");
            OrderLetterUtility.ReceiveLetter(
                label: "OARO_LetterLabel_IncidentConcern".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
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

    private async Task<ServerResponse> TryGetStreamChatCompletionsAsync(IEnumerable<ClientMessage> clientMessages)
    {
        Log.Message("[OARO] 获取AI客户端");
        Client client = await GetAIClient();
        if (client is null)
            return ServerResponse.Invalid("[OARO] 获取AI客户端失败。");

        Log.Message("[OARO] 呼唤大语言API");
        ServerResponse serverResponse;
        try
        {
            serverResponse = await client.StreamChatCompletionsAsync(HttpClient, clientMessages).ConfigureAwait(continueOnCapturedContext: true);
        }
        catch (Exception ex)
        {
            Log.Warning($"[OARO] AI内容流式生成失败：{ex.Message}\n{ex.StackTrace}");
            serverResponse = Invalid(ex.Message);
        }

        if (serverResponse is null)
            return ServerResponse.Invalid("[OARO] 服务器回复为null");

        if (serverResponse.TotalTokensUsed > 0)
        {
            GameTotalTokensUsed += serverResponse.TotalTokensUsed;
            TotalTokensUsed += serverResponse.TotalTokensUsed;
        }

        if (!CurGameValid)
        {
            string warning = "[OARO] 收到响应前游戏已切换。";
            Log.Warning(warning);
            serverResponse.LastErrorMessage ??= warning;
            return serverResponse;
        }

        if (serverResponse.Status == ResponseStatus.Abort || serverResponse.Status == ResponseStatus.Invalid)
        {
            Log.Warning($"[OARO] AI回复被废弃，原因：{serverResponse.LastErrorMessage}");
            return serverResponse;
        }

        return serverResponse;
    }
}

public static class DecoratePrompt
{
    private const string Space = "    ";

    public static void GetOrderPrompt(StringBuilder promptBuilder, RatkinOrder ratkinOrder)
    {
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrder".Translate(ratkinOrder.Name));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrderFunds".Translate(ratkinOrder.Funds.ToStringPercent("0.##")));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrderRelationship".Translate($"OARO_Relationship_{ratkinOrder.Relationship}".Translate()));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrderEsteem".Translate(ratkinOrder.Esteem));
    }

    public static void GetBranchPrompt(StringBuilder promptBuilder, Branch branch)
    {
        promptBuilder.AppendLine("OARO_Prompt_Branch".Translate(branch.Name));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchWorkStateDesc".Translate(branch.CurWorkStateDesc));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchSupplyState".Translate(branch.SupplyState, branch.Supply.ToStringPercent("0.##")));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchPotency".Translate(branch.Potency.ToString("0.##")));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchPopulation".Translate(branch.PopulationHandler.Population));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchPublicSecurity".Translate(branch.PopulationHandler.PublicSecurityLabel, branch.PopulationHandler.PublicSecurity.ToStringPercent("0.##")));

        if (branch.TaskHandler.HasTask)
        {
            promptBuilder.Append(Space);
            promptBuilder.AppendLine("OARO_Prompt_BranchSupplyTask".Translate(branch.TaskHandler.CurTask.Label));
        }

        if (branch.IsOnJointPatrol())
        {
            promptBuilder.Append(Space);
            promptBuilder.AppendLine("OARO_Prompt_BranchOnJointPatrol".Translate());
        }
    }

    public static IEnumerable<ClientMessage> GetMercyQuestTalkPrompt(MercyQuestDef mercyQuestDef)
    {
        if (mercyQuestDef is null)
        {
            yield break;
        }

        StringBuilder promptBuilder = new(RatkinOrderSettings.MainAIPrompt);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("OARO_Setting_MercyQuestPrompt_System".Translate());

        yield return new ClientMessage(ClientMessage.RoleEnum.system, promptBuilder.ToString());


        string example = string.IsNullOrEmpty(mercyQuestDef.fixedHelpDesc) ? "OARO_Setting_MercyQuestPrompt_DefaultExample".Translate()
                                                                           : mercyQuestDef.fixedHelpDesc;

        yield return new ClientMessage(ClientMessage.RoleEnum.user, "OARO_Setting_MercyQuestPrompt_User".Translate(mercyQuestDef.Named("MERCYQUEST"), example.Named("Example")));
    }

    public static IEnumerable<ClientMessage> GetIncidentConcernPrompt(Branch branch, IncidentDef incidentDef, IncidentParms parms, int delayDays)
    {
        if (branch is null || incidentDef is null || parms is null)
        {
            yield break;
        }

        StringBuilder promptBuilder = new(RatkinOrderSettings.MainAIPrompt);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("OARO_Prompt_IncidentConcern_System".Translate());
        GetOrderPrompt(promptBuilder, branch.RatkinOrder);
        promptBuilder.AppendLine();
        GetBranchPrompt(promptBuilder, branch);
        yield return new ClientMessage(ClientMessage.RoleEnum.system, promptBuilder.ToString());

        promptBuilder.Clear();
        List<NamedArgument> arguments =
            [
                 incidentDef.LabelCap.Named("incidentLabel"),
                 delayDays.Named("delayDays")
            ];
        arguments.Add(GenerateNamedArgument(incidentDef.category?.LabelCap, "category"));
        arguments.Add(GenerateNamedArgument(parms.faction?.Name, "relatedFaction"));
        arguments.Add(GenerateNamedArgument(parms.points.ToString("F0"), "points"));
        arguments.Add(GenerateNamedArgument(parms.raidStrategy?.LabelCap, "raidStrategy"));
        arguments.Add(GenerateNamedArgument(parms.raidArrivalMode?.LabelCap, "raidArrivalMode"));

        promptBuilder.AppendLine("OARO_Prompt_IncidentConcern_User".Translate(arguments.ToArray()));
        arguments.Clear();
        yield return new ClientMessage(ClientMessage.RoleEnum.user, promptBuilder.ToString());
    }

    private static NamedArgument GenerateNamedArgument(string argument, string name)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "OARO_Prompt_NotAvailable".Translate().Named(name);
        }
        else
        {
            return argument.Named(name);
        }
    }
}