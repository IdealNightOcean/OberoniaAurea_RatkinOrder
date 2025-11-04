using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class IconLibrary
{
    public static readonly Texture2D Medal_Courage = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Courage");
    public static readonly Texture2D Medal_Tenacity = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Tenacity");
    public static readonly Texture2D Medal_Rescue = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Rescue");
    public static readonly Texture2D Medal_Justice = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Justice");

    public static readonly Texture2D BranchSummaryBackground = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BranchSummaryBackground");

    public static readonly Texture2D BigStrangeIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigStrangeIcon");
    public static readonly Texture2D SmallStrangeIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallStrangeIcon");

    public static readonly Texture2D BigFriendlyIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigFriendlyIcon");
    public static readonly Texture2D SmallFriendlyIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallFriendlyIcon");

    public static readonly Texture2D BigIdleIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigIdleIcon");
    public static readonly Texture2D SmallIdleIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallIdleIcon");

    public static readonly Texture2D BigOutdoorIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigOutdoorIcon");
    public static readonly Texture2D SmallOutdoorIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallOutdoorIcon");

    public static readonly Texture2D BigIndoorIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigIndoorIcon");
    public static readonly Texture2D SmallIndoorIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallIndoorIcon");

    public static readonly Texture2D SmallGeneralBranchIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallGeneralBranchIcon");
    public static readonly Texture2D BigGeneralBranchIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigGeneralBranchIcon");
}
