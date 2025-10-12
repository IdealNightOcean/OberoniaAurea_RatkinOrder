using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionDef : Def
{
    public Type workerClass;
    private OrderInteractionWorker worker;
    public OrderInteractionWorker Worker => worker ??= (OrderInteractionWorker)Activator.CreateInstance(workerClass, this);
    public string cdRecordKey;

    public int cdDays;

    public int needRecommendation;
    public float needFund;

    /// <summary>
    /// 只在 needFund > 0f 时生效
    /// needFund > 0f 时
    /// 如有fundEventDef，则执行FundHandler.AddFundEvent(如有fundEventDef);
    /// 如无fundEventDef，则执行FundHandler.AdjustFundsImmediately(needFund);
    /// </summary>
    public OrderFundEventDef fundEventDef;

    public EsteemHandler.RelationshipKind floorRelationship = EsteemHandler.RelationshipKind.Stranger;
    public int floorEsteem;

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
    }
}