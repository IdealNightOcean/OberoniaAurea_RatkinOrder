using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingWithComps : BranchBuilding
{
    private List<BranchBuildingComp> comps;
    private Dictionary<Type, BranchBuildingComp> compByType;

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
        compByType = new(compsCount);
        for (int i = 0; i < compsCount; i++)
        {
            BranchBuildingComp buildingComp = null;
            try
            {
                buildingComp = (BranchBuildingComp)Activator.CreateInstance(def.comps[i].compClass);
                comps.Add(buildingComp);
                compByType.Add(buildingComp.GetType(), buildingComp);
                buildingComp.Initialize(this, def.comps[i]);
            }
            catch (Exception ex)
            {
                Log.Error("Could not instantiate or initialize a BranchBuildingComp: " + ex);
                comps.Remove(buildingComp);
            }
        }
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

        compByType.TryGetValue(typeof(T), out BranchBuildingComp targetComp);
        return (T)targetComp;
    }
}