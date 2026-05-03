using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public enum BranchTaskType : byte
{
    /// <summary>
    /// 一般
    /// </summary>
    General,
    /// <summary>
    /// 打击犯罪丨危害清剿
    /// </summary>
    CrimeFighting,
    /// <summary>
    /// 维稳丨秩序重塑
    /// </summary>
    StabilityMaintenance,
    /// <summary>
    /// 援助丨民意安抚
    /// </summary>
    Assistance,
    /// <summary>
    /// 监察丨行政问责
    /// </summary>
    Supervision
}

public static class BranchTaskTypeExtension
{
    public static readonly BranchTaskType[] EnumArr = Enum.GetValues(typeof(BranchTaskType)) as BranchTaskType[];

    private static Dictionary<BranchTaskType, List<BranchMedalDef>> medalDefsByTaskType;

    public static List<BranchMedalDef> GetMedalDefsByTaskType(BranchTaskType taskType)
    {
        if (medalDefsByTaskType is null)
        {
            InitMedalDefsByTaskType();
        }

        if (medalDefsByTaskType.TryGetValue(taskType, out List<BranchMedalDef> medalDefs))
        {
            return medalDefs;
        }

        return [];
    }

    private static void InitMedalDefsByTaskType()
    {
        medalDefsByTaskType = DefDatabase<BranchMedalDef>.AllDefsListForReading.GroupBy(m => m.focusedTaskType)
                                                                               .ToDictionary(g => g.Key, g => g.ToList());
    }
}