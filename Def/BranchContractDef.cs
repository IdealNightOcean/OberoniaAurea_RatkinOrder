using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchContractDef : Def
{
    public ThingDef requestThingDef;
    public IntRange requestCountRange;

    [MustTranslate]
    public string fixedRequestReasons;
    [MustTranslate]
    public RulePackDef requestReasonsRulePack;

    public float durationDays = 15f;
    public float coolingDaysAfterSucceed;
    public float coolingDaysAfterFailed;

    public int DurationTicks => (int)(durationDays * 60000);
    public int CoolingTicksAfterSucceed => (int)(coolingDaysAfterSucceed * 60000);
    public int CoolingTicksAfterFailed => (int)(coolingDaysAfterFailed * 60000);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
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