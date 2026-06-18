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
        if (Def.needRecommendation > 0 && RecommendationUtility.CurRecommendationCount(map) < Def.needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation.Named(KeyLibrary_FormatArgName.Count));
        }
        if (Def.needSilver > 0 && !map.HasEnoughThingsOfDef(ThingDefOf.Silver, Def.needSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, Def.needSilver);
        }
        return true;
    }

    public void TryApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        if (!ratkinOrder.IsValid())
        {
            Log.Error("[OARO] RatkinOrder无效，无法应用交互。");
            return;
        }
        if (map is null)
        {
            Log.Error("[OARO] Map为null，无法应用交互。");
            return;
        }

        ApplyEffect(ratkinOrder, map);
    }

    protected virtual void ApplyCost(RatkinOrder ratkinOrder, Map map)
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
            RecommendationUtility.UseRecommendationOfPlayer(map, Def.needRecommendation);
        }
        if (Def.needSilver > 0)
        {
            map.DestroyThingsOfDef(ThingDefOf.Silver, Def.needSilver);
        }
    }

    protected virtual void ApplyEffect(RatkinOrder ratkinOrder, Map map)
    {
        (bool succeeded, bool doPostApply) = (false, false);
        try
        {
            (succeeded, doPostApply) = InteractionEffect(ratkinOrder, map);
        }
        catch (Exception ex)
        {
            (succeeded, doPostApply) = (false, true);
            ModUtility.LogExceptionError(ex,
                errorDesc: $"执行 BranchInteraction [{Def?.defName}] 的交互效果",
                typeName: nameof(OrderInteractionWorker),
                methodName: nameof(ApplyEffect),
                needStackTrace: true);
        }

        if (succeeded)
        {
            try
            {
                ApplyCost(ratkinOrder, map);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"应用 BranchInteraction [{Def?.defName}] 的交互代价",
                    typeName: nameof(OrderInteractionWorker),
                    methodName: nameof(ApplyEffect),
                    needStackTrace: true);
            }
        }

        if (doPostApply)
        {
            PostApplyInteraction(ratkinOrder, map, succeeded);
        }
    }

    /// <returns>
    /// <para>- succeeded：是否成功执行交互逻辑</para>
    /// <para>- doPostApply：是否需要执行后续回调 <see cref="PostApplyInteraction"/></para>
    /// </returns>
    protected virtual (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map) => (true, true);

    protected void PostApplyInteraction(RatkinOrder ratkinOrder, Map map, bool succeeded) => ratkinOrder?.OnPostApplyOrderInteraction(Def, map, succeeded);
}