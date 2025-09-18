using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_AvaerageMood : QuestPartActivable
{
    public string InSignal;
    public string InSignalRemovePawn;

    public string OutSignalSuccess;
    public string OutSignalBelowLowThreshold;
    public string OutSignalBelowHighThreshold;

    public float MoodLowThreshold = -1f;
    public float MoodHighThreshold = -1f;

    public int MinTicksAboveThreshold = -1;
    public int MaxTicksBelowThreshold = -1;

    private int aboveThresholdDuration;
    private int belowThresholdDuration;

    private int ticksToNextCheck = 2500;
    public int CheckInterval = 2500;
    public int CheckPeriod = 2 * 60000;
    public int SampleSize => Mathf.FloorToInt(CheckPeriod / CheckInterval);

    public List<Pawn> Pawns = [];

    private List<float> movingAverage = [];
    private float cachedMovingAverage;

    public override string ExpiryInfoPart => "QuestAveragePawnMood".Translate(CheckPeriod.ToStringTicksToPeriodVerbose(), cachedMovingAverage.ToStringPercent());
    public override string ExpiryInfoPartTip => "QuestAveragePawnMoodTargets".Translate(Pawns.Select(p => p.LabelShort).ToCommaList(useAnd: true), CheckPeriod.ToStringTicksToPeriodVerbose());

    private float AveragePawnMoodPercent
    {
        get
        {
            float averagePercent = 0f;
            int availablePawnCount = 0;
            for (int i = 0; i < Pawns.Count; i++)
            {
                if (Pawns[i].needs is not null && Pawns[i].needs.mood is not null)
                {
                    averagePercent += Pawns[i].needs.mood.CurLevelPercentage;
                    availablePawnCount++;
                }
            }
            if (availablePawnCount == 0)
            {
                return 0f;
            }
            return averagePercent / availablePawnCount;
        }
    }
    private float MovingAveragePawnMoodPercent
    {
        get
        {
            if (movingAverage.Count == 0)
            {
                return AveragePawnMoodPercent;
            }
            float movingAveragePercent = 0f;
            for (int i = 0; i < movingAverage.Count; i++)
            {
                movingAveragePercent += movingAverage[i];
            }
            return movingAveragePercent / movingAverage.Count;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignal, "InSignal");
        Scribe_Values.Look(ref InSignalRemovePawn, "InSignalRemovePawn");

        Scribe_Values.Look(ref OutSignalSuccess, "OutSignalSuccess");
        Scribe_Values.Look(ref OutSignalBelowLowThreshold, "OutSignalBelowLowThreshold");
        Scribe_Values.Look(ref OutSignalBelowHighThreshold, "OutSignalBelowHighThreshold");

        Scribe_Values.Look(ref MoodLowThreshold, "MoodLowThreshold", -1f);
        Scribe_Values.Look(ref MoodHighThreshold, "MoodHighThreshold", -1f);

        Scribe_Values.Look(ref MinTicksAboveThreshold, "MinTicksAboveThreshold", -1);
        Scribe_Values.Look(ref MaxTicksBelowThreshold, "MaxTicksBelowThreshold", -1);

        Scribe_Values.Look(ref aboveThresholdDuration, "aboveThresholdDuration", 0);
        Scribe_Values.Look(ref belowThresholdDuration, "belowThresholdDuration", 0);

        Scribe_Values.Look(ref ticksToNextCheck, "ticksToNextCheck", 2500);
        Scribe_Values.Look(ref CheckInterval, "CheckInterval", 2500);
        Scribe_Values.Look(ref CheckPeriod, "CheckPeriod", 2 * 60000);

        Scribe_Collections.Look(ref Pawns, "Pawns", LookMode.Reference);
        Scribe_Collections.Look(ref movingAverage, "movingAverage", LookMode.Value);

        Scribe_Values.Look(ref cachedMovingAverage, "cachedMovingAverage", 0f);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignal = null;
        InSignalRemovePawn = null;

        outSignalsCompleted = null;
        OutSignalBelowLowThreshold = null;
        OutSignalBelowHighThreshold = null;

        MoodHighThreshold = -1f;
        MoodHighThreshold = -1f;

        MinTicksAboveThreshold = -1;
        MaxTicksBelowThreshold = -1;

        aboveThresholdDuration = 0;
        belowThresholdDuration = 0;

        ticksToNextCheck = 2500;
        CheckInterval = 2500;
        CheckPeriod = 2 * 60000;

        Pawns?.Clear();
        movingAverage?.Clear();

        cachedMovingAverage = 0f;
    }

    public override void QuestPartTick()
    {
        base.QuestPartTick();

        if (--ticksToNextCheck >= 0)
        {
            ticksToNextCheck = CheckInterval;
            while (movingAverage.Count >= SampleSize)
            {
                movingAverage.RemoveLast();
            }
            movingAverage.Insert(0, AveragePawnMoodPercent);
            cachedMovingAverage = MovingAveragePawnMoodPercent;

            if (MinTicksAboveThreshold > 0 && MoodHighThreshold > 0f && cachedMovingAverage >= MoodHighThreshold)
            {
                aboveThresholdDuration += CheckInterval;
                if (aboveThresholdDuration >= MinTicksAboveThreshold)
                {
                    Find.SignalManager.SendSignal(new Signal(OutSignalSuccess));
                    Complete();
                }
            }
            else if (MaxTicksBelowThreshold > 0 && MoodLowThreshold > 0f && cachedMovingAverage <= MoodLowThreshold)
            {
                belowThresholdDuration += CheckInterval;
                if (belowThresholdDuration >= MaxTicksBelowThreshold)
                {
                    Find.SignalManager.SendSignal(new Signal(OutSignalBelowLowThreshold));
                    Complete();
                }
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == InSignalRemovePawn)
        {
            if (signal.args.TryGetArg("SUBJECT", out Pawn p))
            {
                Pawns?.Remove(p);
            }
        }
    }

    protected override void ProcessQuestSignal(Signal signal)
    {
        base.ProcessQuestSignal(signal);
        if (signal.tag == InSignal)
        {
            if (MoodLowThreshold > 0f && cachedMovingAverage <= MoodLowThreshold)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalBelowLowThreshold));
            }

            else if (MoodHighThreshold > 0f && cachedMovingAverage <= MoodHighThreshold)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalBelowHighThreshold));
            }
            else
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalSuccess));
            }

            Complete();
        }
    }
}