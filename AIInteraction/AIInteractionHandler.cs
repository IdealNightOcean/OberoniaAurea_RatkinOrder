using NightOcean.SimpleAIClient;
using NightOcean.SimpleAIClient.OpenAI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Net.Http;
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

    public async Task<ServerResponse> TryGetStreamChatCompletionsAsync(IEnumerable<ClientMessage> clientMessages)
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