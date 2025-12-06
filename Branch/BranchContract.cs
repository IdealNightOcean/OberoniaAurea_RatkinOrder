using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class BranchContract : IExposable
{
    public enum ContractState : byte
    {
        Invalid,
        Cooling,
        Ongoing,
        Finished
    }

    private BranchContractDef def;
    private int requestCount;
    private string requestReason = string.Empty;
    private ContractState curState;

    public ThingDef RequestThingDef => def.requestThingDef;
    public int RequestCount => requestCount;
    public string RequestReason => requestReason;
    public ContractState CurState => curState;
    public bool ValidOngoing => curState == ContractState.Ongoing && def is not null && RequestThingDef is not null;

    private int expirationTick = -1;
    public int TicksToExpire => expirationTick - Find.TickManager.TicksGame;
    public bool ShouldRemove
    {
        get
        {
            return curState switch
            {
                ContractState.Invalid or ContractState.Finished => true,
                ContractState.Ongoing => def is null,
                ContractState.Cooling => TicksToExpire <= 0,
                _ => true,
            };
        }
    }

    public static BranchContract MakeBranchContract(BranchContractDef def)
    {
        return new BranchContract { def = def };
    }

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
            ModUtility.LogExceptionError(ex, "getting contract reason", nameof(BranchContract), nameof(PostInit));
            requestReason = "ERROR".Colorize(ColorLibrary.RedReadable);
        }
        curState = ContractState.Ongoing;
    }

    public AcceptanceReport CanFulfill(Caravan caravan)
    {
        if (caravan is null || !ValidOngoing)
        {
            return false;
        }
        if (CaravanInventoryUtility.HasThings(caravan, RequestThingDef, requestCount))
        {
            return true;
        }
        return "OAFrame_NeedCountOfThing".Translate(RequestThingDef.label, requestCount);
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
        if (!string.IsNullOrEmpty(def.fixedRequestReasons))
        {
            return def.fixedRequestReasons.Formatted(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), RequestThingDef.Named("REQUESTDEF"), requestCount.Named("RequestCount"));
        }
        if (def.requestReasonsRulePack is not null)
        {
            string reason;
            try
            {
                GrammarRequest grammarRequest = new();
                grammarRequest.Includes.Add(def.requestReasonsRulePack);
                grammarRequest.Constants.Add("requestDef", RequestThingDef.defName);
                grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder("ORDER", branch.RatkinOrder));
                grammarRequest.Rules.AddRange(ModUtility.RulesForBranch("BRNACH", branch, alsoAddOrderRule: false));
                grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("ORDERFACTION", branch.RatkinOrder.Faction));
                grammarRequest.Rules.AddRange(GrammarUtility.RulesForDef("REQUESTTHING", RequestThingDef));
                grammarRequest.Rules.Add(new Rule_String("requestCount", requestCount.ToString()));
                reason = GrammarResolver.Resolve("r_text", grammarRequest);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex, nameof(GrammarResolver.Resolve), nameof(BranchContract), nameof(GetContractReason));
                reason = null;
            }

            if (string.IsNullOrEmpty(reason))
            {
                return "OARO_BranchContract_DefaultReason".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), RequestThingDef.Named("REQUESTDEF"), requestCount.Named("RequestCount"));
            }
            else
            {
                return reason;
            }
        }
        return "OARO_BranchContract_DefaultReason".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), RequestThingDef.Named("REQUESTDEF"), requestCount.Named("RequestCount"));

    }
}