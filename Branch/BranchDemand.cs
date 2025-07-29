using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemand : IExposable
{
    public enum DemandState
    {
        NotAccepted,
        Ongoing,
        Finished,
        Invalid,
    }

    private BranchDemandDef def;
    private DemandState curState;
    private Quest relatedQuest;

    public BranchDemandDef Def => def;

    public DemandState CurState => curState;
    public BranchDemandType DemandType => def.demandType;
    public Quest RelatedQuest => relatedQuest;

    private int appearTick = -1;

    public int TicksToExpire => (appearTick + def.DurationTicks) - Find.TickManager.TicksGame;

    public bool ShouldRemove
    {
        get
        {
            if (curState == DemandState.Finished || curState == DemandState.Invalid)
            {
                return true;
            }
            if (curState == DemandState.NotAccepted && TicksToExpire <= 0)
            {
                return true;
            }
            return false;
        }
    }

    public BranchDemand() { }
    public BranchDemand(BranchDemandDef def)
    {
        this.def = def;
        curState = DemandState.NotAccepted;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref curState, "curState", DemandState.NotAccepted);

        Scribe_References.Look(ref relatedQuest, "relatedQuest");

        Scribe_Values.Look(ref appearTick, "appearTick", -1);
    }

    public virtual void PostAddToBranch(Branch branch)
    {
        appearTick = Find.TickManager.TicksGame;
    }

    public virtual void Notify_Accepted(Branch branch)
    {
        curState = TryGenerateQuestAndMakeAvailable(branch) ? DemandState.Ongoing : DemandState.Invalid;
    }

    protected virtual Slate TryGenerateQuestSlate(Branch branch)
    {
        Slate slate = new();
        slate.Set(KeyLibrary_SlateStoreAs.RatkinOrderStoreAs, branch.RatkinOrder);
        slate.Set(KeyLibrary_SlateStoreAs.OrderNameStoreAs, branch.RatkinOrder.Name);
        slate.Set(KeyLibrary_SlateStoreAs.OrderFactionStoreAs, branch.RatkinOrder.Faction);

        slate.Set(KeyLibrary_SlateStoreAs.BranchStoreAs, branch);
        slate.Set(KeyLibrary_SlateStoreAs.BranchNameStoreAs, branch.Name);
        slate.Set(KeyLibrary_SlateStoreAs.BranchSiteStoreAs, branch.WorldObject);

        slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionStoreAs, branch.RatkinOrder.Faction);
        slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionDefStoreAs, branch.RatkinOrder.Faction.def);

        slate.Set(KeyLibrary_SlateStoreAs.DemandDefStoreAs, Def);
        slate.Set(KeyLibrary_SlateStoreAs.DemandTypeStoreAs, Def.demandType);

        return slate;
    }

    protected virtual bool TryGenerateQuestAndMakeAvailable(Branch branch)
    {
        Slate slate = TryGenerateQuestSlate(branch);
        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out relatedQuest, def.relatedQuestDef, slate, forced: true);
    }
}
