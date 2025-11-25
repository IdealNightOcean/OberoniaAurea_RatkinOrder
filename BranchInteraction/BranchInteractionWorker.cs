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

    protected readonly struct InteractionParms(Branch branch, Caravan caravan, BranchBuilding building)
    {
        public readonly Branch Branch = branch;
        public readonly Caravan Caravan = caravan;
        public readonly BranchBuilding Building = building;

        public readonly RatkinOrder RatkinOrder => Branch?.RatkinOrder;
        public readonly Faction Faction => RatkinOrder?.Faction;
    }

    public virtual AcceptanceReport CanUseInteraction(Branch branch, Caravan caravan, BranchBuilding building = null, bool resultOnly = false)
    {
        if (branch is null || caravan is null)
        {
            return false;
        }
        if (Def.onlyBuildingInteraction && building is null)
        {
            return resultOnly ? false : "OARO_Require_TargetBranchBuilding".Translate();
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
        if (Def.hasCoolDown)
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

    public void TryApplyInteraction(Branch branch, Caravan caravan, BranchBuilding building = null)
    {
        if (branch is null || caravan is null)
        {
            return;
        }
        if (Def.onlyBuildingInteraction && building is null)
        {
            Log.Error("[OARO] Attempt to apply BranchInteraction with a null branch building.");
            return;
        }
        InteractionParms parms = new(branch, caravan, building);
        ApplyInteraction(parms);
    }

    protected virtual void DoInteractionCost(InteractionParms parms)
    {
        Branch branch = parms.Branch;

        if (Def.useDefaultCD)
        {
            branch.CooldownManager.RegisterRecord(def.defName, cdTicks: Def.defaultCdDays * 60000);
        }
        if (Def.needSupply > 0f)
        {
            branch.Supply -= Def.needSupply;
        }
        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfCaravan(branch.RatkinOrder, parms.Caravan, Def.needRecommendation);
        }
        if (Def.needSilver > 0)
        {
            parms.Caravan.RemoveThingsOfDef(ThingDefOf.Silver, Def.needSilver);
        }
    }

    protected virtual bool InteractionEffect(InteractionParms parms) => true;

    protected virtual void ApplyInteraction(InteractionParms parms)
    {
        try
        {
            DoInteractionCost(parms);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"{nameof(DoInteractionCost)} for BranchInteraction[{Def?.defName}]",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
        }

        bool applied = false;
        try
        {
            applied = InteractionEffect(parms);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"{nameof(InteractionEffect)} for BranchInteraction[{Def?.defName}]",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
            applied = false;
        }

        if (applied)
        {
            PostApplyInteraction(parms);
        }
    }

    protected void PostApplyInteraction(InteractionParms parms) => PostApplyInteraction(parms.Branch, parms.Caravan, parms.Building);
    protected void PostApplyInteraction(Branch branch, Caravan caravan, BranchBuilding building = null)
    {
        try
        {
            branch.PostApplyBranchInteraction?.Invoke(Def, branch, caravan, building);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"call-back: {nameof(Branch)}.{nameof(Branch.PostApplyBranchInteraction)}",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(PostApplyInteraction),
                needStackTrace: true);
        }
    }
}