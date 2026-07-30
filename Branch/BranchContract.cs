using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部合约
/// </summary>
public class BranchContract : IExposable
{
    public enum ContractState : byte
    {
        /// <summary>
        /// 无效
        /// </summary>
        Invalid,
        /// <summary>
        /// 冷却中
        /// </summary>
        Cooling,
        /// <summary>
        /// 进行中
        /// </summary>
        Ongoing,
        /// <summary>
        /// 已完成
        /// </summary>
        Finished
    }

    private BranchContractDef def;
    private int requestCount;
    private string requestReason = string.Empty;
    private ContractState curState;

    public ThingDef RequestThingDef => def?.requestThingDef;
    public int RequestCount => requestCount;
    public string RequestReason => requestReason;
    public ContractState CurState => curState;
    public bool ValidOngoing => curState == ContractState.Ongoing && RequestThingDef is not null;

    private int expirationTick = -1;
    public int TicksToExpire => Mathf.Max(0, expirationTick - Find.TickManager.TicksGame);
    public bool ShouldRemove
    {
        get
        {
            return curState switch
            {
                ContractState.Invalid or ContractState.Finished => true,
                ContractState.Ongoing => RequestThingDef is null || TicksToExpire <= 0,
                ContractState.Cooling => TicksToExpire <= 0,
                _ => true,
            };
        }
    }

    public static BranchContract MakeBranchContract(BranchContractDef def) => new() { def = def };

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_Values.Look(ref requestCount, nameof(requestCount), 0);
        Scribe_Values.Look(ref requestReason, nameof(requestReason), string.Empty);
        Scribe_Values.Look(ref curState, nameof(curState), ContractState.Invalid);
        Scribe_Values.Look(ref expirationTick, nameof(expirationTick), -1);
    }

    public void PostInit(Branch branch)
    {
        requestCount = def.requestCountRange.RandomInRange;

        expirationTick = Find.TickManager.TicksGame + def.DurationTicks;
        try
        {
            requestReason = GetContractReason(branch);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, "获取合同订单原因", nameof(BranchContract), nameof(PostInit));
            requestReason = KeyLibrary_Misc.ErrorTipWithColor;
        }
        curState = ContractState.Ongoing;
    }

    public AcceptanceReport CanFulfill(Caravan caravan, bool resultOnly)
    {
        if (!ValidOngoing)
        {
            return resultOnly ? false : "OARO_InvalidContract".Translate();
        }
        if (caravan is null)
        {
            return resultOnly ? false : "OARO_NeedACaravan".Translate();
        }
        if (CaravanInventoryUtility.HasThings(caravan, RequestThingDef, requestCount))
        {
            return true;
        }
        return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(RequestThingDef.label, requestCount);
    }

    public void Fulfill(Caravan caravan, Branch branch)
    {
        if (caravan is null || !ValidOngoing)
        {
            return;
        }
        caravan?.RemoveThingsOfDef(RequestThingDef, requestCount);
        def.RewardWorker.Reward(this, caravan, branch);

        if (def.CoolingTicksAfterFulfilled > 0)
        {
            expirationTick = Find.TickManager.TicksGame + def.CoolingTicksAfterFulfilled;
            curState = ContractState.Cooling;
        }
        else
        {
            curState = ContractState.Finished;
        }

        branch.PopulationHandler.Notify_ContractCompleted();
        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchContractCompleted, 1f, addIfMiss: true);

        return;
    }

    private string GetContractReason(Branch branch)
    {
        if (def.requestReasons.NullOrEmpty())
        {
            return "OARO_BranchContract_DefaultReason".Translate(branch.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                                                                 branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                                                                 RequestThingDef.Named("REQUESTDEF"),
                                                                 requestCount.Named("RequestCount"));
        }

        return def.requestReasons.RandomElement().Formatted(
            branch.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
            branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
            RequestThingDef.Named("REQUESTDEF"),
            requestCount.Named("RequestCount"));

    }
}