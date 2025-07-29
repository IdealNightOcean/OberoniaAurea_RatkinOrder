using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestPart_LordJob_CommomTalk : QuestPart_MakeLord, ITalkAction
{
    public int durationTicks;
    public bool nearOrderHall;

    public Pawn talkWith;
    public virtual Pawn TalkWith => talkWith;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref durationTicks, "durationTicks", 0);
        Scribe_Values.Look(ref nearOrderHall, "nearOrderHall", defaultValue: false);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && quest?.State == QuestState.Ongoing)
        {
            this.RegisterTalkAction();
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();

        durationTicks = 0;
        nearOrderHall = false;

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

        IntVec3 wanderCell = this.GetTalkPawnWanderCenterCell(nearOrderHall);
        LordJob_VisitColonyTalkable lordJob = new(faction, wanderCell, durationTicks: durationTicks);
        lordJob.SetTalkAction(mapOfPawn, OARO_ModDefOf.OARO_Job_CommonTalkWith);
        return LordMaker.MakeNewLord(faction, lordJob, Map);
    }

    public abstract void TalkAction(Pawn talker, Pawn talkWith);
}