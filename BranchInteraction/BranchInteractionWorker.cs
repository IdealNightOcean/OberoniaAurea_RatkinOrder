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

    public AcceptanceReport CanUseInteraction(Branch branch, Caravan caravan, bool resultOnly) => CanUseInteraction(branch, null, caravan, resultOnly);
    public virtual AcceptanceReport CanUseInteraction(Branch branch, BranchBuilding building, Caravan caravan, bool resultOnly)
    {
        if (!Def.isCommonInteraction)
        {
            if (building is null || def.relatedBranchBuilding != building.Def)
            {
                return resultOnly ? false : "OARO_Insufficient_BranchBuilding".Translate(def.relatedBranchBuilding.label);
            }
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
        if (!string.IsNullOrEmpty(Def.cdRecordKey))
        {
            int cooldownTicksLeft = ratkinOrder.CooldownManager.GetCooldownTicksLeft(Def.cdRecordKey);
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

    protected abstract void InteractionEffect(Branch branch, BranchBuilding building, Caravan caravan);

    public void ApplyInteraction(Branch branch, Caravan caravan) => ApplyInteraction(branch, null, caravan);
    public void ApplyInteraction(Branch branch, BranchBuilding building, Caravan caravan)
    {
        try
        {
            if (!Def.isCommonInteraction)
            {
                if (building is null || def.relatedBranchBuilding != building.Def)
                {
                    Log.Error("Attempt to apply BranchInteraction with a null or non-related branch building.");
                    return;
                }
            }

            if (Def.cdDays > 0 && !string.IsNullOrEmpty(Def.cdRecordKey))
            {
                branch.CooldownManager.RegisterRecord(Def.cdRecordKey, cdTicks: Def.cdDays * 60000);
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
        catch (Exception ex)
        {
            Log.Error($"Error processing costs for BranchInteraction [{Def.defName}].\nException:\n{ex}");
        }

        try
        {
            InteractionEffect(branch, building, caravan);
        }
        catch (Exception ex)
        {
            Log.Error($"Error triggering effect for BranchInteraction [{Def.defName}].\nException:\n{ex}");
        }
    }
}