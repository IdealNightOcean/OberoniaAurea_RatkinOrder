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
        return "OberoniaAurea.RatkinOrder".Translate();
    }
}

public class RatkinOrderSettings : ModSettings
{
    private Vector2 scrollPosition;
    private float viewRectHeight;

    public static bool NoramlDemandShowMess = true; //普通需求刷出时显示信息
    public static bool CriticalDemandShowMess = true; //关键需求刷出时显示信息

    public static int MaxConcurrentAcceptedDemand = 2; //最多同时接取需求数

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref NoramlDemandShowMess, "NoramlDemandShowMess", defaultValue: true);
        Scribe_Values.Look(ref CriticalDemandShowMess, "CriticalDemandShowMess", defaultValue: true);
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

        listing_Rect.CheckboxLabeled("OARO_Setting_NoramlDemandShowMess".Translate(), ref NoramlDemandShowMess);
        listing_Rect.CheckboxLabeled("OARO_CriticalDemandShowMess".Translate(), ref CriticalDemandShowMess);

        MaxConcurrentAcceptedDemand = (int)listing_Rect.SliderLabeled("OARO_MaxConcurrentAcceptedDemand".Translate(MaxConcurrentAcceptedDemand.ToString()), MaxConcurrentAcceptedDemand, 1f, 99f);

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

    private static void Reset()
    {
        NoramlDemandShowMess = true;
        CriticalDemandShowMess = true;

        MaxConcurrentAcceptedDemand = 2;
    }
}