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

    protected abstract AcceptanceReport TargetValidate(BranchInteractionParms parms, bool resultOnly);

    public AcceptanceReport CanUseInteraction(BranchInteractionParms parms, bool resultOnly)
    {
        AcceptanceReport acceptance = ParmsValidate(parms, resultOnly);
        if (!acceptance)
        {
            return acceptance;
        }

        acceptance = BranchValidate(parms, resultOnly);
        if (!acceptance)
        {
            return acceptance;
        }

        acceptance = TargetValidate(parms, resultOnly);
        if (!acceptance)
        {
            return acceptance;
        }

        return true;
    }

    public void TryApplyInteraction(BranchInteractionParms parms)
    {
        if (!ParmsValidate(parms, resultOnly: true))
        {
            Log.Error($"[OARO] Attempt to apply BranchInteraction with a invalid {nameof(BranchInteractionParms)}.");
            return;
        }

        ApplyInteraction(parms);
    }

    protected void DoInteractionCost(BranchInteractionParms parms)
    {
        DoBranchCost(parms);
        DoTargetCost(parms);
    }

    protected virtual void DoBranchCost(BranchInteractionParms parms)
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
    }

    protected abstract void DoTargetCost(BranchInteractionParms parms);

    /// <returns>
    /// <para>- succeeded：是否成功执行交互逻辑</para>
    /// <para>- doPostApply：是否需要执行后续回调 <see cref="PostApplyInteraction"/></para>
    /// </returns>
    protected virtual (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms) => (true, true);

    protected virtual void ApplyInteraction(BranchInteractionParms parms)
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
                errorDesc: $"{nameof(InteractionEffect)} for BranchInteraction[{Def?.defName}]",
                typeName: nameof(BranchInteractionWorker),
                methodName: nameof(ApplyInteraction),
                needStackTrace: true);
        }

        if (succeeded)
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
        }

        if (doPostApply)
        {
            PostApplyInteraction(parms, succeeded);
        }
    }

    protected void PostApplyInteraction(BranchInteractionParms parms, bool succeeded)
    {
        try
        {
            parms.Branch?.PostApplyBranchInteraction?.Invoke(Def, parms, succeeded);
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