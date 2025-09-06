using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestPart_LordJob_CommomTalk : QuestPart_MakeLord, ITalkAction
{
    public int DurationTicks;
    public bool NearOrderHall;

    protected Pawn talkWith;
    public Pawn TalkWith
    {
        get { return talkWith; }
        set { talkWith = value; }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref DurationTicks, "DurationTicks", 0);
        Scribe_Values.Look(ref NearOrderHall, "NearOrderHall", defaultValue: false);
        Scribe_References.Look(ref talkWith, "talkWith");
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
            signal.args.TryGetArg("SUBJECT", out Pawn p);
            if (p == talkWith)
            {
                this.DeregisterTalkAction();
                talkWith = null;
            }
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

    public abstract void TalkAction(Pawn talker, Pawn talkWith);
}