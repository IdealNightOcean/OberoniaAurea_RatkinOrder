using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchContractDef : Def
{
    private static readonly Type defaultRewardClass = typeof(BranchContractRewardWorker);
    private static readonly BranchContractRewardWorker defaultRewardWorker = new();

    /// <summary>
    /// 需求奖励方法类
    /// </summary>
    private Type rewardWorkerClass = defaultRewardClass;
    private BranchContractRewardWorker rewardWorker;
    public BranchContractRewardWorker RewardWorker => rewardWorker ??= (rewardWorkerClass == defaultRewardClass ? defaultRewardWorker : (BranchContractRewardWorker)Activator.CreateInstance(rewardWorkerClass));

    /// <summary>
    /// 需求的<see cref="ThingDef"/>
    /// </summary>
    public ThingDef requestThingDef;

    /// <summary>
    /// 需求的数量范围（<see cref="IntRange"/>）
    /// </summary>
    public IntRange requestCountRange;

    /// <summary>
    /// 固定的需求原因
    /// </summary>
    [MustTranslate]
    public string fixedRequestReasons;

    /// <summary>需求原因构建使用的<see cref="RulePackDef"/></summary>
    /// <remarks>- 只在<see cref="fixedRequestReasons"/>为<see langword="null"/>或<see cref="string.Empty"/>时起效</remarks>
    [MustTranslate]
    public RulePackDef requestReasonsRulePack;

    /// <summary>
    /// 需求在[接取前]的持续时间（Day）
    /// </summary>
    /// <remarks>超过该时间仍未接取则会被移除</remarks>
    public float durationDays = 15f;

    /// <summary>
    /// 需求[完成后]的冷却时间（Day）
    /// </summary>
    public float coolingDaysAfterFulfilled;

    public int DurationTicks => (int)(durationDays * 60000);
    public int CoolingTicksAfterFulfilled => (int)(coolingDaysAfterFulfilled * 60000);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (rewardWorkerClass is null)
        {
            rewardWorkerClass = defaultRewardClass;
            yield return $"has a null {nameof(rewardWorkerClass)}. Set to default.";
        }
        if (requestThingDef is null)
        {
            yield return $"has an invalid {nameof(requestThingDef)}";
        }
        if (requestCountRange.min <= 0)
        {
            yield return $"'s {nameof(requestCountRange)} may be negative";
        }
        if (requestCountRange.max <= 0)
        {
            yield return $"'s {nameof(requestCountRange)} must be positive";
        }
    }
}