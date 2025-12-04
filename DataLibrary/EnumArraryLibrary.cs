using System;
using System.Linq;

namespace OberoniaAurea.RatkinOrder;

public static class EnumArraryLibrary
{
    public static readonly EsteemHandler.RelationshipKind[] OrderRelationshipKindsArr = (EsteemHandler.RelationshipKind[])Enum.GetValues(typeof(EsteemHandler.RelationshipKind));

    public const int AvailableBranchMedalTypesCount = 5; // BranchMedalType.None不计数

    public static readonly KnightPersonality[] KnightPersonalitiesArr = (KnightPersonality[])Enum.GetValues(typeof(KnightPersonality));
    public const int AvailablePersonalitiesCount = 5; // KnightPersonality.None不计数


    public static readonly BranchTaskType[] BranchTaskTypeArr = (BranchTaskType[])Enum.GetValues(typeof(BranchTaskType));
    public static readonly BranchTaskType[] AvailableBranchTaskTypeArr = Enum.GetValues(typeof(BranchTaskType)).Cast<BranchTaskType>().Where(t => t != BranchTaskType.General).ToArray();
    public static readonly BranchTaskType[] JointPatrolTaskTypeArr = [BranchTaskType.CrimeFighting, BranchTaskType.StabilityMaintenance, BranchTaskType.Assistance, BranchTaskType.Supervision];

    public static readonly BranchTaskHandler.RadicalismDegree[] RadicalismDegreeArr = (BranchTaskHandler.RadicalismDegree[])Enum.GetValues(typeof(BranchTaskHandler.RadicalismDegree));
    public static readonly JointBranchRecord.PatrolInteractionType[] AvailablePatrolInteractionTypeArr = Enum.GetValues(typeof(JointBranchRecord.PatrolInteractionType)).Cast<JointBranchRecord.PatrolInteractionType>().Where(t => t != JointBranchRecord.PatrolInteractionType.None).ToArray();
}