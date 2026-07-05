using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_JointPatrolCaravanHelpSite_FixedCaravan : WorldObject_InteractWithFixedCaravan_Nameable, IJointPatrolCaravanHelpSite
{
    protected Branch branch;
    public Branch Branch => branch;

    private JointPatrolCaravanHelpDef caravanIncidentDef;

    private JointPatrolCaravanHelpWorker_FixedCaravan incidentWorker;
    public JointPatrolCaravanHelpWorker_FixedCaravan IncidentWorker => incidentWorker ??= caravanIncidentDef?.Worker as JointPatrolCaravanHelpWorker_FixedCaravan;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Defs.Look(ref caravanIncidentDef, nameof(caravanIncidentDef));
    }

    public void InitJointPatrolCaravanHelp(Branch branch, JointPatrolCaravanHelpDef def)
    {
        SetOrderBranch(branch);
        caravanIncidentDef = def;
        name = $"{def.label} ({branch?.Name})";
    }

    public void SetOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        string caravanIncidentStr = IncidentWorker?.RequestHelpReason(branch);
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

        if (isWorking)
        {
            Messages.Message("OAFrame_Message_AlreadyHasFixedCaravan".Translate(), MessageTypeDefOf.RejectInput, historical: false);
        }
        else if (caravan.IsExactTypeCaravan())
        {
            if (caravanIncidentDef.Worker is not JointPatrolCaravanHelpWorker_FixedCaravan fixedCaravanWorker)
            {
                Log.Error($"[OARO] CaravanIncidentDef.Worker 不是 {nameof(JointPatrolCaravanHelpWorker_FixedCaravan)} 类型。当前类型：{caravanIncidentDef.Worker?.GetType().Name ?? "null"}");
                return;
            }
            fixedCaravanWorker.Notify_CaravanArrived(caravan, branch, this);
        }
    }

    public override bool StartWork(Caravan caravan)
    {
        if (!ValidateChecker())
        {
            return false;
        }

        if (base.StartWork(caravan))
        {
            return IncidentWorker?.PostStartWork(associatedFixedCaravan, branch, this) ?? false;
        }
        return false;
    }

    public void SetTicksRemaining(int ticksRemaining)
    {
        if (isWorking)
        {
            this.ticksRemaining = ticksRemaining;
        }
    }

    protected override void InterruptWork()
    {
        if (!ValidateChecker())
        {
            return;
        }

        IncidentWorker?.InterruptWork(associatedFixedCaravan, branch, this);
    }

    protected override void FinishWork()
    {
        if (!ValidateChecker())
        {
            return;
        }

        IncidentWorker?.FinishWork(associatedFixedCaravan, branch, this);
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
                Log.Error($"[OARO] {nameof(Notify_CaravanArrived)}使用了无效的{nameof(branch)}。");
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