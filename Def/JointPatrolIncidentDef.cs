using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentDef : Def
{
    public enum IncidentType
    {
        Normal,
        Positive,
        Negative,
        Building,
        Honor,
        Disaster
    }

    public IncidentType incidentType;

    public BranchTaskType? restrictTaskType;

    public JointPatrolManager.PatrolLevel? patrolLevelLimits;

    public Branch.BranchType? restrictBranchType;

    public BranchBuildingDef relatedBuilding;

    public List<JointPatrolIncidentPart> parts;

    public bool CanApply(Branch branch)
    {
        if (restrictTaskType.HasValue && branch.TaskHandler.FocusedTaskType != restrictTaskType.Value)
        {
            return false;
        }
        if (restrictBranchType.HasValue && !branch.IsBranchOfType(restrictBranchType.Value))
        {
            return false;
        }
        if (relatedBuilding is not null && !branch.BuildingHandler.HasBuilding(relatedBuilding))
        {
            return true;
        }
        return true;
    }


    public static IncidentType GetPotentialIncidentType(JointPatrolManager.JointBranchRecord record)
    {
        List<(IncidentType, float)> typeSelector = new(5)
                {
                    (IncidentType.Building,10f),
                    (IncidentType.Disaster,2f)
                };
        if (record.HasInteraction(JointPatrolManager.PatrolInteractionType.Information))
        {
            typeSelector.AddRange(
                [
                    (IncidentType.Normal,32f),
                    (IncidentType.Positive,33f)
                ]);
        }
        else
        {
            typeSelector.AddRange(
                [
                    (IncidentType.Normal,25f),
                    (IncidentType.Positive,25f),
                    (IncidentType.Positive,15f)
                ]);
        }
        if (record.Branch.IsBranchOfType(Branch.BranchType.Honor))
        {
            typeSelector.Add((IncidentType.Honor, 3f));
        }

        return typeSelector.RandomElementByWeight(t => t.Item2).Item1;
    }

    public void ApplyIncident(Branch branch, out string effectExplain)
    {
        effectExplain = string.Empty;
        if (parts is null)
        {
            return;
        }

        StringBuilder explainSB = new();
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].ApplyPart(this, branch, explainSB);
        }
        effectExplain = explainSB.ToString();
    }
}

public abstract class JointPatrolIncidentPart
{
    public abstract void ApplyPart(JointPatrolIncidentDef def, Branch branch, StringBuilder effectExplain);
}

public class JointPatrolIncidentPart_Fund : JointPatrolIncidentPart
{
    [MustTranslate]
    public string changeReason;
    public float change;

    public override void ApplyPart(JointPatrolIncidentDef def, Branch branch, StringBuilder effectExplain)
    {
        branch.RatkinOrder.FundHandler.AdjustFundsImmediately(change, changeReason);
        effectExplain.AppendLine("OARO_ChangeOffset_Fund".Translate(change.ToStringPercentSigned("0.##")));
    }
}

public class JointPatrolIncidentPart_JointPatrolPotency : JointPatrolIncidentPart
{
    private float potencyOffset;
    public override void ApplyPart(JointPatrolIncidentDef def, Branch branch, StringBuilder effectExplain)
    {

    }
}