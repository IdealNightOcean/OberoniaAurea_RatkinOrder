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
    /// 需求原因
    /// </summary>
    [MustTranslate]
    public List<string> requestReasons;

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
            yield return $"'{nameof(rewardWorkerClass)}' 为 null，已设置为默认值。";
        }
        if (requestThingDef is null)
        {
            yield return $"'{nameof(requestThingDef)}' 为 null。";
        }
        if (requestCountRange.min <= 0)
        {
            yield return $"'{nameof(requestCountRange)}' 的最小值必须大于 0。";
        }
        if (requestCountRange.max <= 0)
        {
            yield return $"'{nameof(requestCountRange)}' 的最大值必须大于 0。";
        }
    }
}