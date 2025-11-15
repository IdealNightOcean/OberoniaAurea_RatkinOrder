using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class IconLibrary
{
    public static readonly Texture2D colseX = ContentFinder<Texture2D>.Get("UI/Common/OARO_ColseX");

    public static readonly Texture2D RecommendationIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_RecommendationIcon");

    public static readonly Texture2D BranchSummaryBackground = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BranchSummaryBackground");
    public static readonly Texture2D ShadeTexture = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_Shade");

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


    public static readonly Texture2D BarTex_Green = SolidColorMaterials.NewSolidColorTexture(Color.green);
    public static readonly Texture2D BarTex_White = SolidColorMaterials.NewSolidColorTexture(Color.white);
    public static readonly Texture2D BarTex_Black = SolidColorMaterials.NewSolidColorTexture(Color.black);
}