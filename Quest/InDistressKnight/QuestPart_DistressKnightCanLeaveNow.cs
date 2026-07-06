using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_DistressKnightCanLeaveNow : QuestPartActivable
{
    private const int CheckInterval = 30000;
    public string OutSignal;
    public string InSignalRemovePawn;

    public List<Pawn> Pawns;

    private int ticksToNextCheck = 2500;

    public bool CanRecruitNow
    {
        get
        {
            if (Pawns.NullOrEmpty())
            {
                return false;
            }
            for (int i = 0; i < Pawns.Count; i++)
            {
                if (!IsHealthyPawn(Pawns[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OutSignal = null;
        InSignalRemovePawn = null;

        Pawns = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref ticksToNextCheck, nameof(ticksToNextCheck), 2500);
        Scribe_Values.Look(ref OutSignal, nameof(OutSignal));
        Scribe_Values.Look(ref InSignalRemovePawn, nameof(InSignalRemovePawn));
        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void QuestPartTick()
    {
        if ((--ticksToNextCheck) <= 0)
        {
            ticksToNextCheck = CheckInterval;
            if (CanRecruitNow)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignal));
                Complete();
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!Pawns.NullOrEmpty() && signal.tag == InSignalRemovePawn)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p))
            {
                Pawns.Remove(p);
            }
        }
    }

    private static bool IsHealthyPawn(Pawn pawn) //判断一个Pawn是否健康
    {
        if (pawn.Destroyed || pawn.InMentalState)
        {
            return false;
        }
        HediffSet pawnHediffSet = pawn.health.hediffSet;
        if (pawnHediffSet is null) //没有健康状态属性那肯定是健康的（确信）
        {
            return true;
        }
        if (pawnHediffSet.BleedRateTotal > 0.001f)
        {
            return false;
        }
        if (pawnHediffSet.HasNaturallyHealingInjury())
        {
            return false;
        }
        return true;
    }
}