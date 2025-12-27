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
            ColumnWidth = viewRect.width
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
}