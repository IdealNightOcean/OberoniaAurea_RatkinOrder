using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding : IExposable
{
    public BranchBuildingDef Def;

    /// <summary>
    /// 仅在添加建筑时触发
    /// </summary>
    public virtual void InitActive(Branch branch) { }

    /// <summary>
    ///  添加建筑和加载存档时触发
    /// </summary>
    public virtual void PostActive(Branch branch) { }

    /// <summary>
    /// 移除建筑时触发
    /// </summary>
    public virtual void PostRemoveBuilding(Branch branch) { }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref Def, "Def");
    }

}
