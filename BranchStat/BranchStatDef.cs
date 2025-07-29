using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatDef : Def
{
    public enum StatType
    {
        Float,
        Percent,
        Int,
    }
    public StatType statType = StatType.Float; //属性类型
    public Type workerClass = typeof(BranchStatWorker); //属性计算器类
    private BranchStatWorker worker;
    public BranchStatWorker Worker => worker ??= (BranchStatWorker)Activator.CreateInstance(workerClass, args: this);

    public float baseValue = 0f; //基础值

    public bool cacheable = true;
    public float minValue = int.MinValue + 1; //最小值
    public float maxValue = int.MaxValue - 1; //最大值
    public bool nonNegative = false; //是否为非负数
    public int cacheDuration = 10000; //缓存持续时间 (单位：tick)
    public List<BranchStatPart> statParts; //属性修正器列表

    public override void PostLoad()
    {
        if (statType == StatType.Int)
        {
            minValue = Mathf.Round(minValue);
            maxValue = Mathf.Round(maxValue);
        }

        if (minValue > maxValue)
        {
            (minValue, maxValue) = (maxValue, minValue);
        }

        if (nonNegative)
        {
            minValue = Mathf.Max(minValue, 0f);
            maxValue = Mathf.Max(maxValue, 0f);
        }

        baseValue = Mathf.Clamp(baseValue, minValue, maxValue);

        statParts?.SortByDescending(part => part.priority);

    }
}

public abstract class BranchStatPart
{
    public int priority = 100; //优先级，数值越大越优先
    public abstract float PostTransform(Branch branch, float curValue);
}


[DefOf]
public static class BranchStatDefOf
{
    public static BranchStatDef OARO_AffectRadius;
    public static BranchStatDef OARO_BuildingCeiling;
    public static BranchStatDef OARO_DeployeeDailyXp;

    public static BranchStatDef OARO_BuildingCost;
    public static BranchStatDef OARO_FacilityCost;

    public static BranchStatDef OARO_BombardSupportCount;

    public static BranchStatDef OARO_SquadMemberCeiling;
    public static BranchStatDef OARO_SquadCommanderCeiling;
    public static BranchStatDef OARO_SquadSupplyCeiling;

    public static BranchStatDef OARO_SquadMemberRecoveryRate;
    public static BranchStatDef OARO_SquadSupplyRecoveryRate;

}
