using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingComp : IExposable
{
    protected BranchBuilding parent;
    public BranchBuilding Parent => parent;
    protected BranchBuildingCompProperties props;

    /// <summary>
    /// 在实例化时使用；
    /// 注意加载存档时会调用，此时BranchBuilding(parent)和对应Branch尚未绑定
    /// </summary>
    public virtual void Initialize(BranchBuilding parent, BranchBuildingCompProperties props)
    {
        this.parent = parent;
        this.props = props;
    }

    public virtual void ExposeData() { }

    public virtual void PostInitActive() { }
    public virtual void PostPostActive() { }
    public virtual void PostPostDeactive() { }
    public virtual void PostInitUpgraded() { }
    public virtual void PostPostUpgraded() { }
}