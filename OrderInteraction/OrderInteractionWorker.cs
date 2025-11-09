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
        if (def.cdDays > 0)
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

    public virtual void TryApplyInteraction(RatkinOrder ratkinOrder, Map map, Action<OrderInteractionDef, RatkinOrder, Map> postApplyAction = null)
    {
        if (ApplyInteraction(ratkinOrder, map))
        {
            try
            {
                postApplyAction?.Invoke(Def, ratkinOrder, map);
            }
            catch (Exception ex)
            {
                Log.Error($"An Exception occurred in {nameof(postApplyAction)}.\nException:\n{ex.Message}");
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

        if (Def.cdDays > 0)
        {
            ratkinOrder.CooldownManager.RegisterRecord(Def.defName, cdTicks: Def.cdDays * 60000);
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
            Log.Error($"Error processing costs for BranchInteraction [{Def.defName}].\nException:\n{ex}");
            return false;
        }

        try
        {
            InteractionEffect(ratkinOrder, map);
        }
        catch (Exception ex)
        {
            Log.Error($"Error triggering effect for BranchInteraction [{Def.defName}].\nException:\n{ex}");
            return false;
        }
        return true;
    }
}