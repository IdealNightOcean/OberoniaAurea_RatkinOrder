using OberoniaAurea_Frame;
using RimWorld;
using System;
using Verse;

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
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(EsteemHandler.RelationshipKind.Trustworthy));
        }
        if (ratkinOrder.Esteem < Def.floorEsteem)
        {
            return resultOnly ? false : "OARO_Insufficient_Esteem".Translate(Def.floorEsteem);
        }
        if (ratkinOrder.Funds < Def.MinFundNeeded)
        {
            return resultOnly ? false : "OARO_Insufficient_Fund".Translate(Def.MinFundNeeded.ToStringPercent("0.##"));
        }
        if (def.hasCoolDown)
        {
            int cooldownTicksLeft = ratkinOrder.CooldownManager.GetCooldownTicksLeft(Def.defName);
            if (cooldownTicksLeft > 0)
            {
                return resultOnly ? false : "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
            }
        }
        if (Def.needRecommendation > 0 && RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < Def.needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation, ratkinOrder.Name);
        }
        if (Def.needSilver > 0 && !map.HasEnoughThingsOfDef(ThingDefOf.Silver, Def.needSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, Def.needSilver);
        }
        return true;
    }

    protected abstract void InteractionEffect(RatkinOrder ratkinOrder, Map map);

    public virtual void TryApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        if (ApplyInteraction(ratkinOrder, map))
        {
            try
            {
                ratkinOrder.PostApplyOrderInteraction?.Invoke(Def, ratkinOrder, map);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"call-back: {nameof(RatkinOrder)}.{nameof(RatkinOrder.PostApplyOrderInteraction)}",
                    typeName: nameof(OrderInteractionWorker),
                    methodName: nameof(TryApplyInteraction),
                    needStackTrace: true);
            }
        }
    }

    protected virtual void DoInteractionCost(RatkinOrder ratkinOrder, Map map)
    {
        if (Def.needFund > 0f)
        {
            ratkinOrder.FundHandler.AdjustFundsImmediately(Def.needFund, Def.label);
        }
        else if (Def.fundEventDef is not null)
        {
            ratkinOrder.FundHandler.AddFundEvent(Def.fundEventDef);
        }

        if (Def.useDefaultCD)
        {
            ratkinOrder.CooldownManager.RegisterRecord(Def.defName, cdTicks: Def.defaultCdDays * 60000);
        }

        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, Def.needRecommendation);
        }
        if (Def.needSilver > 0)
        {
            map.DestoryThingsOfDef(ThingDefOf.Silver, Def.needSilver);
        }
    }

    protected bool ApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        try
        {
            DoInteractionCost(ratkinOrder, map);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"{nameof(DoInteractionCost)}for BranchInteraction [{Def?.defName}]",
                typeName: nameof(OrderInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
            return false;
        }

        try
        {
            InteractionEffect(ratkinOrder, map);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"{nameof(InteractionEffect)}for BranchInteraction [{Def?.defName}]",
                typeName: nameof(OrderInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
            return false;
        }
        return true;
    }
}