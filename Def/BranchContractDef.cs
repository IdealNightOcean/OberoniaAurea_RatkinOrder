using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchContractDef : Def
{
    private static readonly Type defaultRewardClass = typeof(BranchContractRewardWorker);
    private static readonly BranchContractRewardWorker defaultRewardWorker = new();

    private Type rewardWorkerClass = defaultRewardClass;
    private BranchContractRewardWorker rewardWorker;
    public BranchContractRewardWorker RewardWorker => rewardWorker ??= (rewardWorkerClass == defaultRewardClass ? defaultRewardWorker : (BranchContractRewardWorker)Activator.CreateInstance(rewardWorkerClass));

    public ThingDef requestThingDef;
    public IntRange requestCountRange;

    [MustTranslate]
    public string fixedRequestReasons;
    [MustTranslate]
    public RulePackDef requestReasonsRulePack;

    public float durationDays = 15f;
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
            yield return "has a null rewardWorkerClass. Set to default.";
        }
        if (requestThingDef is null)
        {
            yield return "has an invalid requestThing";
        }
        if (requestCountRange.min <= 0)
        {
            yield return "'s request thing count may be negative";
        }
        if (requestCountRange.max <= 0)
        {
            yield return "'s request thing count must be positive";
        }
    }
}