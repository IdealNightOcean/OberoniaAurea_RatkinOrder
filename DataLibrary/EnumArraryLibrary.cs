using System;
using System.Linq;

namespace OberoniaAurea.RatkinOrder;

public static class EnumArraryLibrary
{

    public static BranchTaskType[] JointPatrolTaskTypeArr { get; } = [BranchTaskType.CrimeFighting, BranchTaskType.StabilityMaintenance, BranchTaskType.Assistance, BranchTaskType.Supervision];

    public static BranchTaskHandler.RadicalismDegree[] RadicalismDegreeArr { get; } = (BranchTaskHandler.RadicalismDegree[])Enum.GetValues(typeof(BranchTaskHandler.RadicalismDegree));
    public static JointBranchRecord.PatrolInteractionType[] AvailablePatrolInteractionTypeArr { get; } = Enum.GetValues(typeof(JointBranchRecord.PatrolInteractionType)).Cast<JointBranchRecord.PatrolInteractionType>().Where(t => t != JointBranchRecord.PatrolInteractionType.None).ToArray();

}