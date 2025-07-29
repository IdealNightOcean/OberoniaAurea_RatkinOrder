using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团团级协助的功能类，必须实现一个只接受OrderAssistanceDef参数的构造函数
/// </summary>
public abstract class OrderAssistanceWorker
{
    public readonly OrderAssistanceDef Def;

    public OrderAssistanceWorker(OrderAssistanceDef def)
    {
        Def = def;
    }

    public virtual AcceptanceReport CanUseAssistance(RatkinOrder order, Map map, bool resultOnly)
    {
        if (order.Relationship < Def.floorRelationship)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(EsteemUtility.GetRelationshipKindLabel(OrderRelationshipKind.Trustworthy));
        }
        if (order.Esteem < Def.floorEsteem)
        {
            return resultOnly ? false : "OARO_Insufficient_Esteem".Translate(Def.floorEsteem);
        }
        if (order.Funds < Def.needFund)
        {
            return resultOnly ? false : "OARO_Insufficient_Fund".Translate((Def.needFund * 0.01f).ToStringPercent("F2"));
        }
        if (!Def.cdRecordKey.NullOrEmpty())
        {
            int cooldownTicksLeft = order.CooldownManager.GetCooldownTicksLeft(Def.cdRecordKey);
            if (cooldownTicksLeft > 0)
            {
                return resultOnly ? false : "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
            }
        }
        if (Def.needRecommendation > 0 && RecommendationUtility.CurRecommendationOfMap(order, map) < Def.needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation);
        }
        return true;
    }

    public abstract void AssistanceEffect(RatkinOrder order, Map map);

    public virtual void ApplyAssistance(RatkinOrder order, Map map)
    {
        if (Def.needFund > 0f)
        {
            if (Def.fundEventDef is null)
            {
                order.FundHandler.AdjustFundsImmediately(Def.needFund);
            }
            else
            {
                order.FundHandler.AddFundEvent(Def.fundEventDef);
            }
        }

        if (Def.cdDays > 0 && !Def.cdRecordKey.NullOrEmpty())
        {
            order.CooldownManager.RegisterRecord(Def.cdRecordKey, cdTicks: Def.cdDays * 60000);
        }

        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfMap(order, map, Def.needRecommendation);
        }

        AssistanceEffect(order, map);
    }
}