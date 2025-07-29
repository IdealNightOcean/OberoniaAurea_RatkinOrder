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
    public static bool NoramlDemandShowMess = true;
    public static bool CriticalDemandShowMess = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref NoramlDemandShowMess, "noramlDemandShowMess", defaultValue: true);
        Scribe_Values.Look(ref CriticalDemandShowMess, "criticalDemandShowMess", defaultValue: true);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {

    }

}