using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemand : IExposable
{
    public enum DemandType : byte
    {
        Normal,
        Urgency,
        Supplementary,
        Critical
    }

    private enum DemandState : byte
    {
        Invalid,
        NotAccepted,
        Ongoing,
        Finished
    }

    private BranchDemandDef def;
    private DemandState curState;
    private Quest relatedQuest;
    private int expirationTick = -1;

    public BranchDemandDef Def => def;
    public bool HasAccepted => curState != DemandState.NotAccepted;
    public bool IsOngoing => curState == DemandState.Ongoing && relatedQuest?.State == QuestState.Ongoing;
    public DemandType DemandTypeValue => def.demandType;
    public Quest RelatedQuest => relatedQuest;
    public int TicksToExpire => expirationTick - Find.TickManager.TicksGame;

    public bool ShouldRemove
    {
        get
        {
            return curState switch
            {
                DemandState.NotAccepted => TicksToExpire <= 0,
                DemandState.Ongoing => relatedQuest?.State != QuestState.Ongoing,
                DemandState.Finished or DemandState.Invalid => true,
                _ => true
            };
        }
    }

    public static BranchDemand MakeBranchDemand(BranchDemandDef def)
    {
        BranchDemand demand = (BranchDemand)Activator.CreateInstance(type: def.demandClass);
        demand.def = def;
        demand.curState = DemandState.NotAccepted;
        return demand;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_Values.Look(ref curState, nameof(curState), DemandState.Invalid);
        Scribe_References.Look(ref relatedQuest, nameof(relatedQuest));
        Scribe_Values.Look(ref expirationTick, nameof(expirationTick), -1);
    }

    public virtual void PostInit(Branch branch)
    {
        expirationTick = Find.TickManager.TicksGame + def.DurationTicks;
        curState = DemandState.NotAccepted;
    }

    public virtual void OnAccepted(Branch branch)
    {
        Slate slate = GenerateQuestSlate(branch);
        if (OberoniaAurea_Frame.Utility.OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out relatedQuest, def.relatedQuestDef, slate, forced: true))
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
        slate.SetBasicBranchSlateVar(branch);

        slate.Set(OARO_KeyLibrary_SlateStoreAs.demandDef, def);
        slate.Set(OARO_KeyLibrary_SlateStoreAs.demandType, def.demandType);

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        slate.Set("map", map);
        float points = StorytellerUtility.DefaultThreatPointsNow(map);
        slate.Set("points", points);

        return slate;
    }

    public string GetFullDesc()
    {
        if (def.demandType == DemandType.Critical)
        {
            StringBuilder sb = new(def.description);
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("OARO_DemandTarget".Translate());
            sb.AppendLine(":");
            sb.AppendLine(def.targetDesc);
            sb.AppendLine();
            sb.Append("OARO_DemandReward".Translate());
            sb.AppendLine(":");
            sb.AppendLine(def.rewardDesc);
            return sb.ToString();
        }
        else
        {
            return def.description;
        }
    }

    public override string ToString() => $"{def.defName} - {curState}";
}