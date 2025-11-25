using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingWithComps : BranchBuilding
{
    private List<BranchBuildingComp> comps;
    private Dictionary<Type, BranchBuildingComp[]> compsByType;

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            InitializeComps();
        }
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].ExposeData();
            }
        }
    }

    protected override void Initialize(BranchBuildingDef def, Branch branch)
    {
        base.Initialize(def, branch);
        InitializeComps();
    }

    private void InitializeComps()
    {
        if (def.comps is null || def.comps.Count == 0)
        {
            return;
        }
        int compsCount = def.comps.Count;
        comps = new(compsCount);
        compsByType = new(compsCount);
        for (int i = 0; i < compsCount; i++)
        {
            BranchBuildingComp buildingComp = null;
            try
            {
                buildingComp = (BranchBuildingComp)Activator.CreateInstance(def.comps[i].compClass);
                comps.Add(buildingComp);
                buildingComp.Initialize(this, def.comps[i]);
            }
            catch (Exception ex)
            {
                comps.Remove(buildingComp);

                ModUtility.LogExceptionError(ex,
                    errorDesc: "instantiate or initialize a BranchBuildingComp",
                    typeName: nameof(BranchBuildingWithComps),
                    methodName: nameof(InitializeComps),
                    needStackTrace: true);
            }
        }

        compsByType = comps.GroupBy(c => c.GetType()).ToDictionary(g => g.Key, g => g.ToArray());

    }

    public override void InitActive()
    {
        base.InitActive();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostInitActive();
            }
        }
    }

    public override void PostActive()
    {
        base.PostActive();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostPostActive();
            }
        }
    }

    public override void PostDeactive()
    {
        base.PostDeactive();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostPostDeactive();
            }
        }
    }

    public override void InitUpgraded()
    {
        base.InitUpgraded();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostInitUpgraded();
            }
        }
    }

    public override void PostUpgraded()
    {
        base.PostUpgraded();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostPostUpgraded();
            }
        }
    }

    public T GetComp<T>() where T : BranchBuildingComp
    {
        if (comps is null || comps.Count == 0)
        {
            return null;
        }

        int compCount = comps.Count;
        if (compCount < 3)
        {
            if (comps[0] is T targetCompI)
            {
                return targetCompI;
            }
            if (compCount == 2 && comps[1] is T targetCompII)
            {
                return targetCompII;
            }
            return null;
        }

        if (compsByType is not null)
        {
            if (compsByType.TryGetValue(typeof(T), out BranchBuildingComp[] potentialComps))
            {
                return (T)potentialComps[0];
            }
            if (typeof(T).IsSealedWithCache())
            {
                return null;
            }
        }

        for (int i = 0; i < compCount; i++)
        {
            if (comps[i] is T targetCompIII)
            {
                return targetCompIII;
            }
        }
        return null;
    }
}