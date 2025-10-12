using System;
using System.Reflection;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding : IExposable
{
    protected BranchBuildingDef def;
    protected Branch branch;

    public BranchBuildingDef Def => def;
    public Branch Branch => branch;
    public RatkinOrder RatkinOrder => branch.RatkinOrder;

    protected BranchBuilding() { }

    protected virtual void Initialize(BranchBuildingDef def, Branch branch)
    {
        this.def = def;
        this.branch = branch;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_References.Look(ref branch, "branch");
    }

    public static BranchBuilding MakeBranchBuilding(BranchBuildingDef def, Branch branch)
    {
        BranchBuilding building = (BranchBuilding)Activator.CreateInstance(
            type: def.buildingClass,
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
            binder: null,
            args: null,
            culture: null);

        building.Initialize(def, branch);
        return building;
    }

    /// <summary>
    /// 仅在添加建筑时触发
    /// </summary>
    public virtual void InitActive() { }

    /// <summary>
    ///  添加建筑和加载存档时触发
    /// </summary>
    public virtual void PostActive() { }

    /// <summary>
    /// 移除建筑时触发
    /// </summary>
    public virtual void PostDeactive() { }

}