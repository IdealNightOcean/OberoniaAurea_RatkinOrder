using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部交互的功能类，必须实现一个只接受BranchInteractionDef参数的构造函数
/// 分部交互以远行队Caravan为交互载体
/// </summary>
public abstract class BranchInteractionWorker(BranchInteractionDef def)
{
    public readonly BranchInteractionDef Def = def ?? throw new ArgumentNullException(nameof(def));

    public virtual AcceptanceReport CanUseInteraction(Branch branch, Caravan caravan, BranchBuilding building = null, bool resultOnly = false)
    {
        if (branch is null || caravan is null)
        {
            return false;
        }
        if (Def.isBuildingInteraction && building is null)
        {
            return resultOnly ? false : "OARO_Insufficient_TargetBranchBuilding".Translate();
        }
        RatkinOrder ratkinOrder = branch.RatkinOrder;
        if (ratkinOrder.Relationship < Def.floorRelationship)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(EsteemHandler.RelationshipKind.Trustworthy));
        }
        if (ratkinOrder.Esteem < Def.floorEsteem)
        {
            return resultOnly ? false : "OARO_Insufficient_Esteem".Translate(Def.floorEsteem);
        }
        if (branch.Supply < Def.needSupply)
        {
            return resultOnly ? false : "OARO_Insufficient_BranchSupply".Translate(Def.needSupply.ToStringPercent("0.##"));
        }
        if (branch.PopulationHandler.Population < Def.floorPopulation)
        {
            return resultOnly ? false : "OARO_Insufficient_BranchPopulation".Translate(Def.floorPopulation);
        }
        if (Def.cdDays > 0)
        {
            int cooldownTicksLeft = ratkinOrder.CooldownManager.GetCooldownTicksLeft(Def.defName);
            if (cooldownTicksLeft > 0)
            {
                return resultOnly ? false : "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
            }
        }
        if (Def.needRecommendation > 0 && CaravanInventoryUtility.HasThings(caravan, OARO_ThingDefOf.OARO_OrderRecommendation, Def.needRecommendation, (t) => ((OrderRecommendation)t).RatkinOrder == ratkinOrder))
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation, ratkinOrder.Name);
        }
        if (Def.needSilver > 0 && !CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, Def.needSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, Def.needSilver);
        }
        return true;
    }

    public virtual void TryApplyInteraction(Branch branch, Caravan caravan, BranchBuilding building = null, Action<BranchInteractionDef, Branch, Caravan, BranchBuilding> postApplyAction = null)
    {
        if (ApplyInteraction(branch, caravan, building))
        {
            try
            {
                postApplyAction?.Invoke(Def, branch, caravan, building);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"call-back: {nameof(postApplyAction)}",
                    typeName: nameof(BranchInteractionWorker),
                    methodName: nameof(TryApplyInteraction),
                    needStackTrace: true);
            }
        }
    }

    protected virtual void DoInteractionCost(Branch branch, Caravan caravan, BranchBuilding building = null)
    {
        if (Def.cdDays > 0)
        {
            branch.CooldownManager.RegisterRecord(def.defName, cdTicks: Def.cdDays * 60000);
        }
        if (Def.needSupply > 0f)
        {
            branch.Supply -= Def.needSupply;
        }
        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfCaravan(branch.RatkinOrder, caravan, Def.needRecommendation);
        }
        if (Def.needSilver > 0)
        {
            caravan.RemoveThingsOfDef(ThingDefOf.Silver, Def.needSilver);
        }
    }

    protected abstract void InteractionEffect(Branch branch, Caravan caravan, BranchBuilding building = null);

    protected bool ApplyInteraction(Branch branch, Caravan caravan, BranchBuilding building = null)
    {
        if (branch is null || caravan is null)
        {
            return false;
        }
        if (Def.isBuildingInteraction && building is null)
        {
            Log.Error("[OARO] Attempt to apply BranchInteraction with a null  branch building.");
            return false;
        }

        try
        {
            DoInteractionCost(branch, caravan, building);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"{nameof(DoInteractionCost)} for BranchInteraction[{Def?.defName}]",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
            return false;
        }

        try
        {
            InteractionEffect(branch, caravan, building);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"{nameof(InteractionEffect)} for BranchInteraction[{Def?.defName}]",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
            return false;
        }
        return true;
    }
}