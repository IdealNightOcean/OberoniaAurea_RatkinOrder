using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Reflection;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemand : IExposable
{
    public enum DemandType : byte
    {
        Normal,
        Urgency,
        Supplementary,
        Important,
        Core
    }

    private enum DemandState : byte
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
    public bool HasAccepted => curState != DemandState.NotAccepted;
    public bool IsOngoing => curState == DemandState.Ongoing;
    public DemandType DemandTypeValue => def.demandType;
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

    protected BranchDemand() { }

    /// <summary>
    /// 常用于反射构造，注意子类同参数构造函数需要非公开
    /// </summary>
    protected BranchDemand(BranchDemandDef def)
    {
        this.def = def;
        curState = DemandState.NotAccepted;
    }

    public static BranchDemand MakeBranchDemand(BranchDemandDef def)
    {
        return (BranchDemand)Activator.CreateInstance(
            type: def.demandClass,
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
            binder: null,
            args: [def],
            culture: null);
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

    public virtual void OnAccepted(Branch branch)
    {
        Slate slate = GenerateQuestSlate(branch);
        if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out relatedQuest, def.relatedQuestDef, slate, forced: true))
        {
            curState = DemandState.Ongoing;
        }
        else
        {
            curState = DemandState.Invalid;
        }
    }

    protected virtual Slate GenerateQuestSlate(Branch branch)
    {
        Slate slate = new();
        slate.SetBasicOrderSlateVar(branch);

        slate.Set(KeyLibrary_SlateStoreAs.DemandDef, Def);
        slate.Set(KeyLibrary_SlateStoreAs.DemandType, Def.demandType);

        Map map = QuestGen_Get.GetMap();
        slate.Set("map", map);
        float points = StorytellerUtility.DefaultThreatPointsNow(map);
        slate.Set("points", points);

        return slate;
    }

    public override string ToString()
    {
        return def.defName + "-" + curState.ToString();
    }
}