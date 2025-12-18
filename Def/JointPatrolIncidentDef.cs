using RimWorld;
using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.JointBranchRecord;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentDef : JointPatrolInteractionDef
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
    /// 分部建筑限制
    /// </summary>
    public BranchBuildingDef relatedBuilding;

    /// <summary>
    /// 是否为常驻骑士添加心情
    /// </summary>
    public bool addThought = true;

    /// <summary> 为常驻骑士添加的心情Def </summary>
    protected ThoughtDef thoughtToAdd;

    /// <summary> 为常驻骑士添加的心情Def </summary>
    /// <remarks> 
    /// <para>- 若 <see cref="addThought"/> 为 <see langword="false"/>，永远返回 <see langword="null"/></para>
    /// <para>- 若 <see cref="addThought"/> 为 <see langword="true"/>，且同时 <see cref="thoughtToAdd"/> 不为 <see langword="null"/>，返回 <see cref="thoughtToAdd"/></para>
    /// <para>- 若 <see cref="addThought"/> 为 <see langword="true"/>，且同时 <see cref="thoughtToAdd"/> 为 <see langword="null"/>，根据 <see cref="incidentType"/> 返回默认值</para>
    /// </remarks>
    public ThoughtDef ThoughtToAdd
    {
        get
        {
            if (!addThought) return null;
            if (thoughtToAdd is not null) return thoughtToAdd;

            return incidentType switch
            {
                IncidentType.Positive => OARO_ThoughtDefOf.OARO_Thought_JointPatrolPositive,
                IncidentType.Negative => OARO_ThoughtDefOf.OARO_Thought_JointPatrolNegative,
                IncidentType.Disaster => OARO_ThoughtDefOf.OARO_Thought_JointPatrolDisaster,
                _ => null
            };
        }
    }

    /// <summary>
    /// 能否触发小事件
    /// </summary>
    public override bool CanApplyOn(Branch branch, PatrolLevel patrolLevel)
    {
        if (!base.CanApplyOn(branch, patrolLevel))
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