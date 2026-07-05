using RimWorld.Planet;
using System;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_JointPatrolCaravanHelpSite : WorldObject_Interactive_Nameable, IJointPatrolCaravanHelpSite
{
    protected Branch branch;
    public Branch Branch => branch;
    private JointPatrolCaravanHelpDef caravanIncidentDef;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
        Scribe_Defs.Look(ref caravanIncidentDef, "caravanIncidentDef");
    }

    public void InitJointPatrolCaravanHelp(Branch branch, JointPatrolCaravanHelpDef def)
    {
        SetOrderBranch(branch);
        caravanIncidentDef = def;
        name = $"{def.label} ({branch?.Name})";
    }

    public void SetOrderBranch(Branch branch) => this.branch = branch;

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        string caravanIncidentStr = caravanIncidentDef?.Worker?.RequestHelpReason(branch);
        if (!String.IsNullOrEmpty(caravanIncidentStr))
        {
            sb.AppendInNewLine(caravanIncidentStr);
        }
        return sb.ToString();
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (!ValidateChecker())
        {
            return;
        }

        caravanIncidentDef?.Worker?.Notify_CaravanArrived(caravan, branch, this);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
            this.SafeDestroy();
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
            this.SafeDestroy();
        }
    }

    private bool ValidateChecker(bool silence = false)
    {
        if (caravanIncidentDef is null)
        {
            if (!silence)
            {
                Log.Error($"[OARO] {nameof(caravanIncidentDef)} 在 {nameof(Notify_CaravanArrived)} 中为null");
            }
            return false;
        }
        if (!branch.IsValid())
        {
            if (!silence)
            {
                Log.Error($"[OARO] 在 {nameof(Notify_CaravanArrived)} 中使用了无效的{nameof(branch)}.");
            }
            return false;
        }
        if (branch.RatkinOrder.JointPatrolManager.CurState != JointPatrolManager.PatrolState.Ongoing)
        {
            if (!silence)
            {
                Log.Error($"[OARO] {nameof(JointPatrolManager)} 状态不是{nameof(JointPatrolManager.PatrolState.Ongoing)}（当前状态：{branch.RatkinOrder.JointPatrolManager.CurState}）");
            }
            return false;
        }
        return true;
    }
}