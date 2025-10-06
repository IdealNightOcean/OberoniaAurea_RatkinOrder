using RimWorld;
using System;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团团级交互的功能类，必须实现一个只接受OrderInteractionDef参数的构造函数
/// </summary>
public abstract class OrderInteractionWorker(OrderInteractionDef def)
{
    public readonly OrderInteractionDef Def = def ?? throw new ArgumentNullException(nameof(def));

    public virtual AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (ratkinOrder.Relationship < Def.floorRelationship)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Trustworthy));
        }
        if (ratkinOrder.Esteem < Def.floorEsteem)
        {
            return resultOnly ? false : "OARO_Insufficient_Esteem".Translate(Def.floorEsteem);
        }
        if (ratkinOrder.Funds < Def.needFund)
        {
            return resultOnly ? false : "OARO_Insufficient_Fund".Translate((Def.needFund * 0.01f).ToStringPercent("F2"));
        }
        if (!Def.cdRecordKey.NullOrEmpty())
        {
            int cooldownTicksLeft = ratkinOrder.CooldownManager.GetCooldownTicksLeft(Def.cdRecordKey);
            if (cooldownTicksLeft > 0)
            {
                return resultOnly ? false : "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
            }
        }
        if (Def.needRecommendation > 0 && RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < Def.needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation, ratkinOrder.Name);
        }
        return true;
    }

    public abstract void InteractionEffect(RatkinOrder ratkinOrder, Map map);

    public void ApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        if (Def.needFund > 0f)
        {
            if (Def.fundEventDef is null)
            {
                ratkinOrder.FundHandler.AdjustFundsImmediately(Def.needFund);
            }
            else
            {
                ratkinOrder.FundHandler.AddFundEvent(Def.fundEventDef);
            }
        }

        if (Def.cdDays > 0 && !Def.cdRecordKey.NullOrEmpty())
        {
            ratkinOrder.CooldownManager.RegisterRecord(Def.cdRecordKey, cdTicks: Def.cdDays * 60000);
        }

        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, Def.needRecommendation);
        }

        InteractionEffect(ratkinOrder, map);
    }
}