using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.DataLibrary;

[StaticConstructorOnStartup]
public static class OARO_IconLibrary
{
    public static Texture2D Placeholer { get; } = ContentFinder<Texture2D>.Get("UI/Common/OARO_DefaultButton_Inactive");

    public static readonly Texture2D DefaultButton_Inactive = ContentFinder<Texture2D>.Get("UI/Common/OARO_DefaultButton_Inactive");
    public static readonly Texture2D DefaultButton_Active = ContentFinder<Texture2D>.Get("UI/Common/OARO_DefaultButton_Active");

    public static readonly Texture2D ColseX = ContentFinder<Texture2D>.Get("UI/Common/OARO_ColseX");
    public static readonly Texture2D BackArrow = ContentFinder<Texture2D>.Get("UI/Common/OARO_BackArrow");
    public static readonly Texture2D PlusSign = ContentFinder<Texture2D>.Get("UI/Common/OARO_PlusSign");

    public static readonly Texture2D SmallExclamation = ContentFinder<Texture2D>.Get("UI/Common/OARO_SmallExclamation");

    public static readonly Texture2D StarWhite = ContentFinder<Texture2D>.Get("UI/Common/OARO_StarWhite");
    public static readonly Texture2D StarBlack = ContentFinder<Texture2D>.Get("UI/Common/OARO_StarBlack");

    public static readonly Texture2D ellipsisButton = ContentFinder<Texture2D>.Get("UI/Common/OARO_EllipsisButton");
    public static readonly Texture2D ellipsisButton_Down = ContentFinder<Texture2D>.Get("UI/Common/OARO_EllipsisButton_Down");

    public static readonly Texture2D RecommendationIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_RecommendationIcon");

    public static readonly Texture2D BranchSummaryBackground = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BranchSummaryBackground");
    public static readonly Texture2D ShadeTexture = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_Shade");

    public static readonly Texture2D BigStrangeIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigStrangeIcon");
    public static readonly Texture2D SmallStrangeIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallStrangeIcon");

    public static readonly Texture2D BigFriendlyIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigFriendlyIcon");
    public static readonly Texture2D SmallFriendlyIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallFriendlyIcon");

    public static readonly Texture2D BigIdleIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigIdleIcon");
    public static readonly Texture2D SmallIdleIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallIdleIcon");

    public static readonly Texture2D BigAbroadIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigAbroadIcon");
    public static readonly Texture2D SmallAbroadIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallAbroadIcon");

    public static readonly Texture2D BigOnBaseIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigOnBaseIcon");
    public static readonly Texture2D SmallOnBaseIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallOnBaseIcon");

    public static readonly Texture2D SmallGeneralBranchIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_SmallGeneralBranchIcon");
    public static readonly Texture2D BigGeneralBranchIcon = ContentFinder<Texture2D>.Get("UI/BranchCommon/OARO_BigGeneralBranchIcon");

    /// <summary>
    /// 联巡条目背景绑带+渐变
    /// </summary>
    public static readonly Texture2D JointPatrolEntryShadeTex = ContentFinder<Texture2D>.Get("UI/KnightChivalry/JointPatrol/OARO_EntryShade");
    /// <summary>
    ///联巡条目背景绑带+渐变（遮罩）
    /// </summary>
    public static readonly Texture2D JointPatrolEntryShadeMask = ContentFinder<Texture2D>.Get("UI/KnightChivalry/JointPatrol/OARO_EntryShade_M");

    /// <summary>
    /// 荣誉背景渐变
    /// </summary>
    public static readonly Texture2D HonorBackgroundTex = ContentFinder<Texture2D>.Get("UI/BranchHonor/OARO_Background");

    /// <summary>
    /// 荣誉绑带
    /// </summary>
    public static readonly Texture2D HonorRibbonTex = ContentFinder<Texture2D>.Get("UI/BranchHonor/OARO_Ribbon");
    /// <summary>
    /// 荣誉绑带（遮罩）
    /// </summary>
    public static readonly Texture2D HonorRibbonMask = ContentFinder<Texture2D>.Get("UI/BranchHonor/OARO_Ribbon_Mask");
}