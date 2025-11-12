using System;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

public static class EnumArraryLibrary
{
    public static readonly EsteemHandler.RelationshipKind[] OrderRelationshipKindsArr = (EsteemHandler.RelationshipKind[])Enum.GetValues(typeof(EsteemHandler.RelationshipKind));

    public const int AvailableBranchMedalTypesCount = 5; // BranchMedalType.None不计数

    public static readonly KnightPersonality[] KnightPersonalitiesArr = (KnightPersonality[])Enum.GetValues(typeof(KnightPersonality));

    public static readonly PatrolEndType[] PatrolEndTypeArr = (PatrolEndType[])Enum.GetValues(typeof(PatrolEndType));
    public const int AvailablePersonalitiesCount = 5; // KnightPersonality.None不计数
}