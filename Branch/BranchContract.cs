using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class BranchContract : IExposable
{
    public enum ContractState : byte
    {
        NotAccepted,
        Cooling,
        Ongoing,
        Finished,
        Invalid
    }

    private BranchContractDef def;
    private int requestCount;
    private string requestReason = string.Empty;
    private ContractState curState;
    private Quest relatedQuest;

    public ThingDef RequestThingDef => def.requestThingDef;
    public int RequestCount => requestCount;
    public string RequestReason => requestReason;
    public ContractState CurState => curState;
    public Quest RelatedQuest => relatedQuest;

    private int expirationTick = -1;
    public int TicksToExpire => expirationTick - Find.TickManager.TicksGame;
    public bool ShouldRemove
    {
        get
        {
            return curState switch
            {
                ContractState.Ongoing => relatedQuest?.State != QuestState.Ongoing,
                ContractState.NotAccepted or ContractState.Cooling => TicksToExpire <= 0,
                ContractState.Finished or ContractState.Invalid => true,
                _ => true,
            };
        }
    }

    public static BranchContract MakeBranchContract(BranchContractDef def)
    {
        return new BranchContract
        {
            def = def,
            curState = ContractState.NotAccepted
        };
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref requestCount, "requestCount", 0);
        Scribe_Values.Look(ref requestReason, "requestReason", string.Empty);
        Scribe_Values.Look(ref curState, "curState", ContractState.NotAccepted);
        Scribe_Values.Look(ref expirationTick, "expirationTick", -1);
        Scribe_References.Look(ref relatedQuest, "relatedQuest");
    }

    public void PostInit(Branch branch)
    {
        requestCount = def.requestCountRange.RandomInRange;
        relatedQuest = null;
        expirationTick = Find.TickManager.TicksGame + def.DurationTicks;
        requestReason = GetContractReason(branch);
        curState = ContractState.NotAccepted;
    }

    public void OnAccepted(Branch branch)
    {
        Slate slate = GenerateQuestSlate(branch);
        if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out Quest quest, OARO_QuestScriptDefOf.OARO_Quest_BranchContract, slate, forced: true))
        {
            relatedQuest = quest;
            curState = ContractState.Ongoing;
        }
        else
        {
            curState = ContractState.Invalid;
        }
    }

    public void OnContractFinished(bool succeed)
    {
        int coolingDuration = -1;
        if (def is not null)
        {
            coolingDuration = succeed ? def.CoolingTicksAfterSucceed : def.CoolingTicksAfterFailed;
        }

        def = null;
        requestCount = 0;
        relatedQuest = null;
        expirationTick = -1;
        curState = ContractState.Cooling;

        if (coolingDuration > 0)
        {
            expirationTick = Find.TickManager.TicksGame + coolingDuration;
            curState = ContractState.Cooling;
        }
        else
        {
            curState = succeed ? ContractState.Finished : ContractState.Invalid;
        }
    }

    private Slate GenerateQuestSlate(Branch branch)
    {
        Slate slate = new();
        slate.SetBasicBranchSlateVar(branch);

        slate.Set(KeyLibrary_SlateStoreAs.ContractDef, def);
        slate.Set(KeyLibrary_SlateStoreAs.ContractThingDef, RequestThingDef);
        slate.Set(KeyLibrary_SlateStoreAs.ContractThingCount, requestCount);
        slate.Set(KeyLibrary_SlateStoreAs.ContractReason, requestReason);

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        slate.Set("map", map);
        float points = StorytellerUtility.DefaultThreatPointsNow(map);
        slate.Set("points", points);

        return slate;
    }

    private string GetContractReason(Branch branch)
    {
        if (!string.IsNullOrEmpty(def.fixedRequestReasons))
        {
            return def.fixedRequestReasons.Formatted(branch.Name.Named("BRANCHNAME"), RequestThingDef.Named("REQUESTDEF"), requestCount.Named("REQUESTCOUNT"));
        }
        if (def.requestReasonsRulePack is not null)
        {
            GrammarRequest grammarRequest = new();
            grammarRequest.Includes.Add(def.requestReasonsRulePack);
            grammarRequest.Constants.Add("requestDef", RequestThingDef.defName);
            grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder("ORDER", branch.RatkinOrder));
            grammarRequest.Rules.AddRange(ModUtility.RulesForBranch("BRNACH", branch, alsoAddOrderRule: false));
            grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("ORDERFACTION", branch.RatkinOrder.Faction));
            grammarRequest.Rules.AddRange(GrammarUtility.RulesForDef("REQUESTTHING", RequestThingDef));
            grammarRequest.Rules.Add(new Rule_String("requestCount", requestCount.ToString()));

            return GrammarResolver.Resolve("r_text", grammarRequest);
        }
        return "OARO_BranchContract_DefaultReason".Translate(branch.Name.Named("BRANCHNAME"), RequestThingDef.Named("REQUESTDEF"), requestCount.Named("REQUESTCOUNT"));
    }
}