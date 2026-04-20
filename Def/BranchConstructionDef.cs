using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部建设项目Def
/// </summary>
/// <remarks>- <see cref="BranchBuildingDef"/> 和 <see cref="BranchFacilityDef"/> 的基类</remarks>
public abstract class BranchConstructionDef : Def
{
    /// <summary>
    /// 图标（小+大）
    /// </summary>
    public PathedTexture2DWithExpanded iconTexture;

}