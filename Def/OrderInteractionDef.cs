using System;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionDef : InteractionDefBase
{
    public Type workerClass;
    private OrderInteractionWorker worker;
    public OrderInteractionWorker Worker => worker ??= (OrderInteractionWorker)Activator.CreateInstance(workerClass, args: this);

    public float needFund = -1f;
    /// <summary>
    /// 只在 needFund > 0f 时生效
    /// needFund > 0f 时
    /// 如有fundEventDef，则执行FundHandler.AddFundEvent(如有fundEventDef);
    /// 如无fundEventDef，则执行FundHandler.AdjustFundsImmediately(needFund);
    /// </summary>
    public OrderFundEventDef fundEventDef;

    public float MinFundNeeded => needFund > 0f ? needFund : (fundEventDef is null ? 0f : fundEventDef.changeRange.min);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (workerClass is null)
        {
            yield return "has a null workerClass.";
        }

        if (needFund > 0f && fundEventDef is not null)
        {
            yield return "can't set both needFund and fundEventDef.";
        }
    }
}