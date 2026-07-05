using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal sealed class QuestNode_Root_ResidentKnightBackPlayer : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        Map map = slate.Get<Map>("map") ?? QuestGen_Get.GetMap();
        MapParent mapParent = map?.Parent;
        RatkinOrder ratkinOrder = slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.ratkinOrder);

        string forceEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("ForceEnd_Quest");

        quest.Delay(delayTicks: 15 * 60000, inner: null, outSignalComplete: forceEndSignal);

        List<Pawn> pawns = slate.Get<List<Pawn>>("residentKnights");
        if (pawns.NullOrEmpty())
        {
            QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            return;
        }

        string inSignalPawnSpawned = QuestGenUtility.HardcodedSignalWithQuestID("residentKnights.Spawned");
        string outSignalCanBack = QuestGenUtility.HardcodedSignalWithQuestID("ResidentKnight_CanBack");
        string succeedEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("SucceedEnd_Quest");

        QuestPart_ResidentKnightBackPlayer questPart_ResidentKnightBackPlayer = new()
        {
            inSignalEnable = slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            ratkinOrder = ratkinOrder,
            pawns = [],
            inSignalPawnSpawned = inSignalPawnSpawned,
            outSignalCanBack = outSignalCanBack,
            outSignalEnd = succeedEndSignal,
            mapParent = mapParent
        };
        questPart_ResidentKnightBackPlayer.pawns.AddRange(pawns);
        quest.AddPart(questPart_ResidentKnightBackPlayer);

        quest.PawnsArrive(pawns: pawns,
            mapParent: mapParent,
            inSignal: outSignalCanBack,
            arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
            joinPlayer: true,
            customLetterLabel: "OARO_LetterLabel_ResidentKnightReturnFromJointPatrol".Translate(),
            customLetterText: "OARO_LetterText_ResidentKnightReturnFromJointPatrol".Translate(
                GenLabel.ThingsLabel(pawns.Cast<Thing>()).Named("PawnsInfo"),
                ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName))
            );

        quest.End(QuestEndOutcome.Unknown, inSignal: forceEndSignal);
        quest.End(QuestEndOutcome.Success, inSignal: succeedEndSignal);
    }
}

internal sealed class QuestPart_ResidentKnightBackPlayer : QuestPartActivable
{
    public RatkinOrder ratkinOrder;
    public MapParent mapParent;

    public string inSignalPawnSpawned;
    public string outSignalCanBack;
    public string outSignalEnd;

    public List<Pawn> pawns;

    private StringBuilder resultSummarySB;
    private string resultSummary;

    private int ticksToNextMapCheck = 1000;

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            resultSummary = resultSummarySB?.ToString();
        }

        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_References.Look(ref mapParent, "mapParent");

        Scribe_Values.Look(ref inSignalPawnSpawned, "inSignalPawnSpawned");
        Scribe_Values.Look(ref outSignalCanBack, "outSignalCanBack");
        Scribe_Values.Look(ref outSignalEnd, "outSignalEnd");

        Scribe_Values.Look(ref resultSummary, "resultSummary");
        Scribe_Values.Look(ref ticksToNextMapCheck, "ticksToNextMapCheck", 1000);

        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            resultSummarySB = new StringBuilder(resultSummary ?? string.Empty);
            resultSummary = string.Empty;
        }
    }

    public override void Cleanup()
    {
        ratkinOrder = null;
        mapParent = null;

        inSignalPawnSpawned = null;
        outSignalCanBack = null;
        outSignalEnd = null;

        resultSummarySB = null;
        resultSummary = null;
        ticksToNextMapCheck = 1000;

        pawns = null;
        base.Cleanup();
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (resultSummarySB is null)
        {
            return;
        }

        OrderLetterUtility.ReceiveLetter(
            label: "OARO_LetterLabel_ResidentKnightReturnFromJointPatrol".Translate(),
            text: resultSummarySB.ToString(),
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: ratkinOrder,
            sender: ratkinOrder?.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);
    }

    public override void QuestPartTick()
    {
        if ((--ticksToNextMapCheck) <= 0)
        {
            ticksToNextMapCheck = 10000;
            mapParent = quest.GetAvailableMapParent(mapParent);
            if (mapParent is not null)
            {
                Find.SignalManager.SendSignal(new Signal(outSignalCanBack));
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!pawns.NullOrEmpty() && signal.tag == inSignalPawnSpawned)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p) && pawns.Remove(p))
            {
                string partResult = ResidentKnightAcademic(p);
                if (!String.IsNullOrEmpty(partResult))
                {
                    resultSummarySB ??= new(64);
                    resultSummarySB.AppendLine(partResult);
                }
                if (pawns.Count == 0)
                {
                    Find.SignalManager.SendSignal(new Signal(outSignalEnd));
                }
            }
        }
    }

    public override bool QuestPartReserves(Pawn p)
    {
        return pawns?.Contains(p) ?? false;
    }

    private string ResidentKnightAcademic(Pawn pawn)
    {
        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight record))
        {
            return null;
        }
        KnightChivalryDef chivalry = record.Chivalry;
        AcademicHandler academicHandler = record.AcademicHandler;
        KnightAcademicDef academicDef = academicHandler.Academics.Where(kv => (kv.Key.academicType == KnightAcademicDef.AcademicType.Geneal)
                                                                           && (kv.Key.chivalry == chivalry)
                                                                           && (kv.Value < kv.Key.MaxStageLevel))
                                                                 .RandomElementWithFallback().Key;

        academicDef ??= DefDatabase<KnightAcademicDef>.AllDefsListForReading.Where(d => (d.academicType == KnightAcademicDef.AcademicType.Geneal)
                                                                                     && (d.chivalry == chivalry))
                                                                            .RandomElementWithFallback();

        if (academicDef is null || !academicHandler.CanUpgradeAcademic(academicDef, directly: true, resultOnly: true))
        {
            float gainPoints = 1000f * record.Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
            record.MeditationPoints += gainPoints;
            return "OARO_JointPatrol_OnlyMeditationPoints".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN), gainPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count));
        }
        else
        {
            float gainPoints = 500f * record.Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
            record.MeditationPoints += gainPoints;
            academicHandler.UpgradeAcademic(academicDef, directly: true);
            return "OARO_JointPatrol_MeditationPointsAndAcademic".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN), gainPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count), academicDef.Named("ACADEMIC"));
        }
    }
}