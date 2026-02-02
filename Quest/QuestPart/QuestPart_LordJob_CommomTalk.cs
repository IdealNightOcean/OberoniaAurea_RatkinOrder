using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestPart_LordJob_CommomTalk : QuestPart_MakeLord, ITalkAction
{
    public int DurationTicks;
    public bool NearOrderHall;

    public string InSignalForceTriggerTalk;

    protected bool talkable;

    protected Pawn talkWith;
    public Pawn TalkWith => talkWith;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref DurationTicks, nameof(DurationTicks), 0);
        Scribe_Values.Look(ref NearOrderHall, nameof(NearOrderHall), defaultValue: false);

        Scribe_Values.Look(ref InSignalForceTriggerTalk, nameof(InSignalForceTriggerTalk));

        Scribe_Values.Look(ref talkable, nameof(talkable), defaultValue: false);
        Scribe_References.Look(ref talkWith, nameof(talkWith));
        if (Scribe.mode == LoadSaveMode.PostLoadInit && quest?.State == QuestState.Ongoing)
        {
            this.RegisterTalkAction();
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();

        DurationTicks = 0;
        NearOrderHall = false;
        InSignalForceTriggerTalk = null;

        this.DeregisterTalkAction();
        talkWith = null;
    }

    public override void PostQuestAdded()
    {
        base.PostQuestAdded();
        if (quest?.State == QuestState.Ongoing)
        {
            this.RegisterTalkAction();
        }
    }

    public override void PreQuestAccept()
    {
        base.PreQuestAccept();
        this.RegisterTalkAction();
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignalRemovePawn)
        {
            signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p);
            if (p == talkWith)
            {
                DeregisterTalkAction(clearTalkWith: true);
            }
        }
        if (signal.tag == InSignalForceTriggerTalk)
        {
            ForceTriggerTalk();
        }
    }

    protected virtual void ForceTriggerTalk()
    {
        if (talkable && talkWith is not null)
        {
            TalkAction(talkWith: talkWith, canPostpone: false);
        }
    }

    protected override Lord MakeLord()
    {
        pawns.AddDistinct(mapOfPawn);

        IntVec3 wanderCell = this.GetTalkPawnWanderCenterCell(NearOrderHall);
        LordJob_VisitColonyTalkable lordJob = new(faction, wanderCell, durationTicks: DurationTicks);
        lordJob.SetTalkAction(mapOfPawn, OARO_JobDefOf.OARO_Job_CommonTalkWith);
        return LordMaker.MakeNewLord(faction, lordJob, Map);
    }

    public abstract void TalkAction(Pawn talkWith, Pawn talker = null, bool canPostpone = true);

    public void SetTalkWith(Pawn talkWith, bool resetTalkable = true)
    {
        if (this.talkWith is not null)
            DeregisterTalkAction(clearTalkWith: true);

        if (talkWith is null)
            return;

        if (resetTalkable)
            talkable = true;

        this.talkWith = talkWith;
        if (talkWith.GetLord()?.LordJob is LordJob_VisitColonyTalkable talkableLordJob)
        {
            talkableLordJob.EnableTalk(talkWith);
            this.talkWith = talkWith;
            this.RegisterTalkAction();
        }
    }

    protected void DeregisterTalkAction(bool dismiss = true, bool clearTalkWith = true)
    {
        talkable = false;
        TalkActionUtility.DeregisterTalkAction(this, dismiss);
        if (clearTalkWith)
        {
            talkWith = null;
        }
    }
}