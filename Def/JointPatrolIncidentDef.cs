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

    /// <summary>
    /// 事件类型
    /// </summary>
    public IncidentType incidentType;

    /// <summary>
    /// 专注任务限制
    /// </summary>
    public BranchTaskType? restrictTaskType;

    /// <summary>
    /// 联巡等级限制
    /// </summary>
    public PatrolLevel? patrolLevelLimits;

    /// <summary>
    /// 分部类型限制
    /// </summary>
    public Branch.BranchType? restrictBranchType;

    /// <summary>
    /// 分部建筑限制
    /// </summary>
    public BranchBuildingDef relatedBuilding;

    /// <summary>
    /// 事件描述列表
    /// </summary>
    [MustTranslate]
    public List<string> customDescriptions;

    /// <summary>
    /// 事件功能列表
    /// </summary>
    public List<JointPatrolIncidentPart> parts;

    /// <summary>
    /// 能否触发小事件
    /// </summary>
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

    /// <summary>
    /// 触发小事件
    /// </summary>
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

    /// <summary>
    /// 根据联巡参与者获取随机的联巡小事件类型（<see cref="IncidentType"/>）
    /// </summary>
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
            yield return $"Incident type is '{nameof(IncidentType.Building)}', but '{nameof(relatedBuilding)}' is null.";
        }
        if (relatedBuilding is not null && incidentType != IncidentType.Building)
        {
            incidentType = IncidentType.Building;
            yield return $"'{nameof(relatedBuilding)}' is specified, but incident type is not '{nameof(IncidentType.Building)}'. Type has been set to '{nameof(IncidentType.Building)}'.";
        }
    }
}