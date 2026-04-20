using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部驻派记录 - 包含驻派Def、相关Pawn、驻派总天数、剩余天数等内容
/// </summary>
public abstract class BranchResident : IExposable
{
    protected BranchResidentDef def;
    public BranchResidentDef Def => def;

    protected Pawn pawn;
    public Pawn Pawn => pawn;

    protected int totalDeployDays;
    public int TotalDeployDays => totalDeployDays;

    public int DeployDaysLeft;

    public float Progress => totalDeployDays > 0f ? Mathf.Clamp01((float)DeployDaysLeft / totalDeployDays) : 0f;

    public static BranchResident GenerateBranchResident(BranchResidentDef def, Pawn residentPawn, int deployDaysOverride = -1)
    {
        BranchResident resident = (BranchResident)Activator.CreateInstance(def.residentClass);
        resident.def = def;
        resident.pawn = residentPawn;
        resident.totalDeployDays = deployDaysOverride > 0 ? deployDaysOverride : def.defaultDeployDays;
        return resident;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_References.Look(ref pawn, nameof(pawn));
        Scribe_Values.Look(ref totalDeployDays, nameof(totalDeployDays), 0);
        Scribe_Values.Look(ref DeployDaysLeft, nameof(DeployDaysLeft), 0);
    }

    public virtual void StartResidency(Branch branch) => DeployDaysLeft = totalDeployDays;

    public abstract void EndResidency(Branch branch);
}