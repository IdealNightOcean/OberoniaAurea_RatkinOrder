using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部联巡交互Def
/// </summary>
/// <remarks>- <see cref="JointPatrolCaravanHelpDef"/> 和 <see cref="JointPatrolIncidentDef"/> 的基类</remarks>
public abstract class JointPatrolInteractionDef : Def
{
    /// <summary>
    /// 专注任务骑士精神限制
    /// </summary>
    public KnightChivalryDef restrictChivalry;

    /// <summary>
    /// 分部类型限制
    /// </summary>
    public Branch.BranchType? restrictBranchType;

    /// <summary>
    /// 联巡等级限制
    /// </summary>
    public PatrolLevel? patrolLevelLimits;

    /// <summary>
    /// 事件描述列表
    /// </summary>
    [MustTranslate]
    public List<string> customDescriptions;

    /// <summary>
    /// 事件功能列表
    /// </summary>
    public List<JointPatrolInteractionPart> parts;

    public virtual bool CanApplyOn(Branch branch, PatrolLevel patrolLevel)
    {
        if (restrictChivalry is not null && restrictChivalry != branch.TaskHandler.FocusedTaskChivalry)
        {
            return false;
        }
        if (restrictBranchType.HasValue && !branch.IsBranchOfType(restrictBranchType.Value))
        {
            return false;
        }
        if (patrolLevelLimits.HasValue && patrolLevel != patrolLevelLimits)
        {
            return false;
        }
        return true;
    }
}