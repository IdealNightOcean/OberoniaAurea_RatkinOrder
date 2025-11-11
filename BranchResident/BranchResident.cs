using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchResident : IExposable
{
    protected BranchResidentDef def;
    public BranchResidentDef Def => def;

    protected Pawn resident;
    public Pawn Resident => resident;

    protected int totalDeployDays;
    public int TotalDeployDays => totalDeployDays;

    public int DeployDaysLeft;

    public float Progress => totalDeployDays > 0f ? Mathf.Clamp01(DeployDaysLeft / totalDeployDays) : 0f;

    public static BranchResident GenerateBranchResident(BranchResidentDef def, Pawn residentPawn, int deployDaysOverride = -1)
    {
        BranchResident resident = (BranchResident)Activator.CreateInstance(def.residentClass);
        resident.def = def;
        resident.resident = residentPawn;
        resident.totalDeployDays = deployDaysOverride > 0 ? deployDaysOverride : def.defaultDeployDays;
        return resident;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_References.Look(ref resident, "resident");
        Scribe_Values.Look(ref totalDeployDays, "totalDeployDays", 0);
        Scribe_Values.Look(ref DeployDaysLeft, "DeployDaysLeft", 0);
    }

    public virtual void StartResidency(Branch branch)
    {
        DeployDaysLeft = totalDeployDays;
    }

    public abstract void EndResidency(Branch branch);

    public bool Validate() => resident is not null;
}