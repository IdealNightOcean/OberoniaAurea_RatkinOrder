using System.Collections.Generic;
using System.Text;
using Verse;
using static OberoniaAurea.RatkinOrder.JointBranchRecord;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentDef : Def
{
    public enum IncidentType
    {
        Neutral,
        Positive,
        Negative,
        Building,
        Honor,
        Disaster
    }

    public IncidentType incidentType;

    public BranchTaskType? restrictTaskType;

    public PatrolLevel? patrolLevelLimits;

    public Branch.BranchType? restrictBranchType;

    public BranchBuildingDef relatedBuilding;

    [MustTranslate]
    public List<string> customDescriptions;

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


    public JointIncidentRecord ApplyIncident(JointBranchRecord record)
    {
        if (record?.Branch is null)
        {
            return null;
        }

        StringBuilder explainSB = new();
        if (!customDescriptions.NullOrEmpty())
        {
            explainSB.AppendLine(customDescriptions.RandomElement().Formatted(record.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)));
            explainSB.AppendLine();
        }

        if (!parts.NullOrEmpty())
        {
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].ApplyPart(this, record, explainSB);
            }
        }

        return new JointIncidentRecord()
        {
            Def = this,
            RelatedBranch = record.Branch,
            Description = explainSB.ToString(),
            TriggerTick = Find.TickManager.TicksGame
        };
    }

    public static IncidentType GetPotentialIncidentType(JointBranchRecord record)
    {
        List<(IncidentType, float)> typeSelector = new(5)
                {
                    (IncidentType.Building,10f),
                    (IncidentType.Disaster,2f)
                };
        if (record.HasInteraction(PatrolInteractionType.Information))
        {
            typeSelector.AddRange(
                [
                    (IncidentType.Neutral,32f),
                    (IncidentType.Positive,33f)
                ]);
        }
        else
        {
            typeSelector.AddRange(
                [
                    (IncidentType.Neutral,25f),
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

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (incidentType == IncidentType.Building && relatedBuilding is null)
        {
            yield return "Incident type is 'Building', but RelatedBuilding is null.";
        }
        if (relatedBuilding is not null && incidentType != IncidentType.Building)
        {
            incidentType = IncidentType.Building;
            yield return "RelatedBuilding is specified, but incident type is not 'Building'. Type has been updated to 'Building'.";
        }
    }
}