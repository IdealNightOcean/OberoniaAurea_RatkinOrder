using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OberoniaAureaRatkinOrder : Mod
{
    public static RatkinOrderSettings Settings;

    public OberoniaAureaRatkinOrder(ModContentPack content) : base(content)
    {
        Settings = GetSettings<RatkinOrderSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "Mod.OberoniaAurea.RatkinOrder".Translate();
    }
}

public class RatkinOrderSettings : ModSettings
{
    private Vector2 scrollPosition;
    private float viewRectHeight;

    /// <summary>
    /// 普通需求刷出时是否显示消息
    /// </summary>
    public static bool NoramlDemandShowMess = true;
    /// <summary>
    /// 关键需求刷出时显示信息
    /// </summary>
    public static bool CriticalDemandShowMess = true;

    /// <summary>
    /// 最多同时接取需求数
    /// </summary>
    public static int MaxConcurrentAcceptedDemand = 2;

    /// <summary>
    /// 每个分部最多同时存在的合约
    /// </summary>
    public static int MaxConcurrentContractPerBranch = 5;

    /// <summary>
    /// 是否启用信件上限
    /// </summary>
    public static bool HasMaxLetterLimit = true;
    /// <summary>
    /// 收件箱最多存储的信件数
    /// </summary>
    public static int MaxLetterCount = 100;
    [Unsaved] private static string maxLetterCountStr;
    /// <summary>
    /// 是否启用信件过期时间
    /// </summary>
    public static bool HasLetterRetentionLimit = true;
    /// <summary>
    /// 信件最长保留时间（天）
    /// </summary>
    public static int MaxLetterRetentionDays = 300;
    [Unsaved] private static string maxLetterRetentionDaysStr;

    /// <summary>
    /// 每种巡逻互动类型的最大累积次数
    /// </summary>
    public static int MaxAcquiredPatrolInteractionPreType = 3;

    /// <summary>
    /// 是否启用AI相关内容
    /// </summary>
    public static bool EnableAIContent = false;

    /// <summary>
    /// AI服务URL
    /// </summary>
    public static string AIServiceUrl = "https://api.siliconflow.cn/v1/chat/completions";

    /// <summary>
    /// AI模型名称
    /// </summary>
    public static string AIModelName = "deepseek-ai/DeepSeek-V3.2";

    /// <summary>
    /// API密钥
    /// </summary>
    public static string APIKey = "";

    /// <summary>
    /// AI Prompt文本
    /// </summary>
    private static string mainAIPrompt;
    public static string MainAIPrompt
    {
        get => mainAIPrompt ??= "OARO_Prompt_DefaultMainAIPrompt".Translate();
        private set => mainAIPrompt = value ?? string.Empty;
    }

    [Unsaved] private static string promptBuffer;
    [Unsaved] private static bool promptBufferInitialized;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref NoramlDemandShowMess, nameof(NoramlDemandShowMess), defaultValue: true);
        Scribe_Values.Look(ref CriticalDemandShowMess, nameof(CriticalDemandShowMess), defaultValue: true);
        Scribe_Values.Look(ref MaxConcurrentAcceptedDemand, nameof(MaxConcurrentAcceptedDemand), 2);
        Scribe_Values.Look(ref MaxConcurrentContractPerBranch, nameof(MaxConcurrentContractPerBranch), 5);

        Scribe_Values.Look(ref HasMaxLetterLimit, nameof(HasMaxLetterLimit), defaultValue: true);
        Scribe_Values.Look(ref MaxLetterCount, nameof(MaxLetterCount), 100);
        Scribe_Values.Look(ref HasLetterRetentionLimit, nameof(HasLetterRetentionLimit), defaultValue: true);
        Scribe_Values.Look(ref MaxLetterRetentionDays, nameof(MaxLetterRetentionDays), 300);

        Scribe_Values.Look(ref MaxAcquiredPatrolInteractionPreType, nameof(MaxAcquiredPatrolInteractionPreType), 3);

        // AI相关设置
        Scribe_Values.Look(ref EnableAIContent, nameof(EnableAIContent), defaultValue: false);
        Scribe_Values.Look(ref AIServiceUrl, nameof(AIServiceUrl), "https://api.openai.com/v1/chat/completions");
        Scribe_Values.Look(ref AIModelName, nameof(AIModelName), "gpt-3.5-turbo");
        Scribe_Values.Look(ref APIKey, nameof(APIKey), "");
        Scribe_Values.Look(ref mainAIPrompt, nameof(mainAIPrompt));
    }

    private static void Reset()
    {
        NoramlDemandShowMess = true;
        CriticalDemandShowMess = true;
        MaxConcurrentAcceptedDemand = 2;
        MaxConcurrentContractPerBranch = 5;

        HasMaxLetterLimit = true;
        MaxLetterCount = 100;
        maxLetterCountStr = MaxLetterCount.ToString();
        HasLetterRetentionLimit = true;
        MaxLetterRetentionDays = 300;
        maxLetterRetentionDaysStr = MaxLetterRetentionDays.ToString();

        MaxAcquiredPatrolInteractionPreType = 3;

        EnableAIContent = false;

    }

    private static void ResetAISettings()
    {
        AIServiceUrl = "https://api.siliconflow.cn/v1/chat/completions";
        AIModelName = "deepseek-ai/DeepSeek-V3.2";
        APIKey = string.Empty;
        MainAIPrompt = "OARO_Prompt_DefaultMainAIPrompt".Translate();
        promptBuffer = MainAIPrompt;
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect outRect = new(inRect.x, inRect.y, inRect.width * 0.6f, inRect.height);
        outRect = outRect.CenteredOnXIn(inRect);
        float viewRectX = outRect.x + 8f;
        Rect viewRect = new(viewRectX, outRect.y, outRect.width - 16f, viewRectHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        Listing_Standard listing_Rect = new()
        {
            ColumnWidth = viewRect.width,
            maxOneColumn = true
        };
        listing_Rect.Begin(viewRect);

        listing_Rect.CheckboxLabeled($"OARO_Setting_{nameof(NoramlDemandShowMess)}".Translate(), ref NoramlDemandShowMess);
        listing_Rect.CheckboxLabeled($"OARO_Setting_{nameof(CriticalDemandShowMess)}".Translate(), ref CriticalDemandShowMess);

        MaxConcurrentAcceptedDemand = (int)listing_Rect.SliderLabeled($"OARO_Setting_{nameof(MaxConcurrentAcceptedDemand)}".Translate(MaxConcurrentAcceptedDemand.ToString()), MaxConcurrentAcceptedDemand, 1f, 20f);
        MaxConcurrentContractPerBranch = (int)listing_Rect.SliderLabeled($"OARO_Setting_{nameof(MaxConcurrentContractPerBranch)}".Translate(MaxConcurrentContractPerBranch.ToString()), MaxConcurrentContractPerBranch, 1f, 20f);

        listing_Rect.CheckboxLabeled($"OARO_Setting_{nameof(HasMaxLetterLimit)}".Translate(), ref HasMaxLetterLimit);
        if (HasMaxLetterLimit)
        {
            listing_Rect.TextFieldNumericLabeled(label: $"OARO_Setting_{nameof(MaxLetterCount)}".Translate(), ref MaxLetterCount, ref maxLetterCountStr, 1f, 500f);
        }

        listing_Rect.CheckboxLabeled($"OARO_Setting_{nameof(HasLetterRetentionLimit)}".Translate(), ref HasLetterRetentionLimit);
        if (HasLetterRetentionLimit)
        {
            listing_Rect.TextFieldNumericLabeled(label: $"OARO_Setting_{nameof(MaxLetterRetentionDays)}".Translate(), ref MaxLetterRetentionDays, ref maxLetterRetentionDaysStr, 1f, 600f);
        }

        listing_Rect.Gap(12f);
        MaxAcquiredPatrolInteractionPreType = (int)listing_Rect.SliderLabeled($"OARO_Setting_{nameof(MaxAcquiredPatrolInteractionPreType)}".Translate(MaxAcquiredPatrolInteractionPreType.ToString()), MaxAcquiredPatrolInteractionPreType, 1f, 20f);

        // AI相关设置
        listing_Rect.Gap(12f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.CheckboxLabeled($"OARO_Setting_{nameof(EnableAIContent)}".Translate(), ref EnableAIContent);
        Text.Font = GameFont.Small;
        if (EnableAIContent)
        {
            DrawAISettings(listing_Rect);
        }

        if (listing_Rect.ButtonText("OAFrame_Reset".Translate()))
        {
            Reset();
        }

        listing_Rect.End();
        if (Event.current.type == EventType.Layout)
        {
            viewRectHeight = listing_Rect.MaxColumnHeightSeen + 50f;
        }
        Widgets.EndScrollView();
    }

    private static void DrawAISettings(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"OARO_Setting_AISettingDesc".Translate().Colorize(ColorLibrary.Orange));
        listing_Rect.Gap(6f);
        listing_Rect.Label($"OARO_Setting_{nameof(AIServiceUrl)}".Translate());
        AIServiceUrl = listing_Rect.TextEntry(AIServiceUrl);
        listing_Rect.Label($"OARO_Setting_{nameof(AIModelName)}".Translate());
        AIModelName = listing_Rect.TextEntry(AIModelName);
        listing_Rect.Label($"OARO_Setting_{nameof(APIKey)}".Translate());
        APIKey = listing_Rect.TextEntry(APIKey);

        listing_Rect.Gap(6f);

        if (!promptBufferInitialized)
        {
            promptBuffer = MainAIPrompt;
            promptBufferInitialized = true;
        }
        listing_Rect.Label($"OARO_Setting_{nameof(MainAIPrompt)}".Translate());
        Rect rect = listing_Rect.GetRect(300f);
        string newText = Widgets.TextArea(rect, promptBuffer);
        if (newText != promptBuffer)
        {
            promptBuffer = newText;
            MainAIPrompt = newText;
        }
        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("OARO_ResetAISettings".Translate()))
        {
            ResetAISettings();
        }
    }
}