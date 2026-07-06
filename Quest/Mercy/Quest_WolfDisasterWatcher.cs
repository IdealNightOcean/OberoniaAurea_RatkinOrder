using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 狼灾任务监控 QuestNode（内部特化类）
/// </summary>
internal sealed class QuestNode_WolfDisasterWatcher : QuestNode
{
    public SlateRef<string> outSignalDiscovered;
    public SlateRef<string> inSignalGainIntelligence;
    public SlateRef<string> inSignalObservationEstablished;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_WolfDisasterWatcher questPart_WolfDisasterWatcher = new()
        {
            OutSignalDiscovered = QuestGenUtility.HardcodedSignalWithQuestID(outSignalDiscovered.GetValue(slate)),
            InSignalGainIntelligence = QuestGenUtility.HardcodedSignalWithQuestID(inSignalGainIntelligence.GetValue(slate)),
            InSignalObservationEstablished = QuestGenUtility.HardcodedSignalWithQuestID(inSignalObservationEstablished.GetValue(slate))

        };
        QuestGen.quest.AddPart(questPart_WolfDisasterWatcher);
    }

}

/// <summary>
/// 狼灾任务监控 QuestPartActivable（内部特化类）
/// </summary>
internal sealed class QuestPart_WolfDisasterWatcher : QuestPartActivable
{
    private const int ValidIntelligenceForDiscover = 4;

    public string OutSignalDiscovered;
    public string InSignalGainIntelligence;
    public string InSignalObservationEstablished;

    private int validIntelligenceCount;
    private bool observationEstablished;
    private bool hasDiscovered;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalDiscovered, nameof(OutSignalDiscovered));
        Scribe_Values.Look(ref InSignalGainIntelligence, nameof(InSignalGainIntelligence));
        Scribe_Values.Look(ref InSignalObservationEstablished, nameof(InSignalObservationEstablished));

        Scribe_Values.Look(ref validIntelligenceCount, nameof(validIntelligenceCount), 0);
        Scribe_Values.Look(ref observationEstablished, nameof(observationEstablished), defaultValue: false);
        Scribe_Values.Look(ref hasDiscovered, nameof(hasDiscovered), defaultValue: false);
    }
    public override void Cleanup()
    {
        base.Cleanup();
        OutSignalDiscovered = null;
        InSignalGainIntelligence = null;
        InSignalObservationEstablished = null;
    }

    public override string ExpiryInfoPart => "OARO_WolfDisaster_IntelligenceCount".Translate(validIntelligenceCount, ValidIntelligenceForDiscover);

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (!observationEstablished && signal.tag == InSignalObservationEstablished)
        {
            observationEstablished = true;
        }
        if (!hasDiscovered && signal.tag == InSignalGainIntelligence)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out int count))
            {
                GainIntelligence(count);
            }
            else
            {
                GainIntelligence(1);
            }
        }
    }

    public void GainIntelligence(int count)
    {
        validIntelligenceCount = Mathf.Max(validIntelligenceCount + count, 0);
        if (count > 0 && observationEstablished && Rand.Bool)
        {
            validIntelligenceCount++;
        }

        if (!hasDiscovered && validIntelligenceCount >= ValidIntelligenceForDiscover)
        {
            hasDiscovered = true;
            Find.SignalManager.SendSignal(new Signal(OutSignalDiscovered));
        }
    }

    public static bool GetWolfDisasterWatcher(Quest quest, out QuestPart_WolfDisasterWatcher watcher)
    {
        watcher = null;
        if (quest is null)
        {
            return false;
        }

        for (int i = 0; i < quest.PartsListForReading.Count; i++)
        {
            if (quest.PartsListForReading[i] is QuestPart_WolfDisasterWatcher gainWatcher)
            {
                watcher = gainWatcher;
                return true;
            }
        }

        return false;
    }
}
