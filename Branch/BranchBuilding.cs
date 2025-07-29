using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding : IExposable
{
    public BranchBuildingDef def;

    public virtual void PostAddBuilding(Branch branch) { } // 添加建筑时触发的事件

    public virtual void PostRemoveBuilding(Branch branch) { } // 移除建筑时触发的事件

    public virtual void PostLoadInit(Branch branch) { } // 加载建筑时触发的事件

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
    }

}
