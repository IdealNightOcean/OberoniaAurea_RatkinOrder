using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
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

    public AcceptanceReport CanUseInteraction(BranchInteractionParms parms, bool resultOnly)
    {
        AcceptanceReport acceptance = ParmsValidate(parms, resultOnly);
        if (!acceptance)
            return acceptance;

        acceptance = BranchValidate(parms, resultOnly);
        if (!acceptance)
            return acceptance;

        if (Def.target != BranchInteractionDef.InteractionTarget.None)
        {
            acceptance = TargetValidate(parms, resultOnly);
            if (!acceptance)
                return acceptance;
        }

        return true;
    }

    public void TryApplyInteraction(BranchInteractionParms parms)
    {
        if (!ParmsValidate(parms, resultOnly: true))
        {
            Log.Error($"[OARO] 尝试应用 BranchInteraction 时使用了无效的 {nameof(BranchInteractionParms)}。");
            return;
        }

        ApplyEffect(parms);
    }

    protected virtual AcceptanceReport ParmsValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (!parms.Branch.IsValid())
        {
            return false;
        }
        if (Def.onlyBuildingInteraction && parms.Building is null)
        {
            return false;
        }
        return true;
    }

    protected virtual AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        Branch branch = parms.Branch;
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
        if (Def.friendlyOnly && !branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            return resultOnly ? false : "OARO_NotFriendlyBranch".Translate();
        }
        if (Def.honorOnly)
        {
            if (branch.HonorDef is null)
            {
                return resultOnly ? false : "OARO_NotHonorBranch".Translate();
            }
            if (Def.honorDef is not null && branch.HonorDef != Def.honorDef)
            {
                return resultOnly ? false : "OARO_NotHonorBranchOf".Translate(Def.honorDef.Named(OARO_KeyLibrary_FormatArgName.HONORDEF));
            }
        }
        if (Def.hasCoolDown)
        {
            int cooldownTicksLeft = branch.CooldownManager.GetCooldownTicksLeft(Def.defName);
            if (cooldownTicksLeft > 0)
            {
                return resultOnly ? false : "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
            }
        }
        return true;
    }

    protected virtual AcceptanceReport TargetValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (Def.needRecommendation > 0 && RecommendationUtility.HasEnoughRecommendation(parms.Target, Def.needRecommendation))
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation.Named(KeyLibrary_FormatArgName.Count));
        }
        if (Def.needSilver > 0 && !OAFrame_ThingUtility.HasEnoughThingsOfDef(parms.Target, ThingDefOf.Silver, Def.needSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, Def.needSilver);
        }
        return true;
    }

    protected virtual void ApplyCost(BranchInteractionParms parms)
    {
        Branch branch = parms.Branch;
        if (Def.useDefaultCD && Def.defaultCdDays > 0)
        {
            branch.CooldownManager.RegisterRecord(def.defName, cdTicks: Def.defaultCdDays * 60000, removeWhenExpired: true);
        }
        if (Def.needSupply > 0f)
        {
            branch.Supply -= Def.needSupply;
        }

        if (Def.target != BranchInteractionDef.InteractionTarget.None)
        {
            if (Def.needRecommendation > 0)
            {
                RecommendationUtility.UseRecommendationOfPlayer(parms.Target, Def.needRecommendation);
            }
            if (Def.needSilver > 0)
            {
                OAFrame_ThingUtility.RemoveThingsOfDef(parms.Target, ThingDefOf.Silver, Def.needSilver);
            }
        }
    }

    /// <returns>
    /// <para>- succeeded：是否成功执行交互逻辑</para>
    /// <para>- doPostApply：是否需要执行后续回调 <see cref="PostApplyEffect"/></para>
    /// </returns>
    protected virtual (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms) => (true, true);

    protected virtual void ApplyEffect(BranchInteractionParms parms)
    {
        (bool succeeded, bool doPostApply) = (false, false);
        try
        {
            (succeeded, doPostApply) = InteractionEffect(parms);
        }
        catch (Exception ex)
        {
            (succeeded, doPostApply) = (false, true);
            ModUtility.LogExceptionError(ex,
                errorDesc: $"执行 BranchInteraction[{Def?.defName}] 的交互效果",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(ApplyEffect),
                needStackTrace: true);
        }

        if (succeeded)
        {
            try
            {
                ApplyCost(parms);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"应用 BranchInteraction[{Def?.defName}] 的交互成本",
                    typeName: nameof(BranchInteractionWorker),
                    methodName: nameof(ApplyEffect),
                    needStackTrace: true);
            }
        }

        if (doPostApply)
        {
            PostApplyEffect(parms, succeeded);
        }
    }

    protected void PostApplyEffect(BranchInteractionParms parms, bool succeeded) => parms.Branch?.OnPostApplyBranchInteraction(Def, parms, succeeded);

}