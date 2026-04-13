using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 征募队的 LordJob | TalkAction （内部特化类）
/// </summary>
internal sealed class QuestPart_LordJob_TaxCollector : QuestPart_LordJob_CommomTalk
{
    public string InSignalTreat;

    public string OutSignalQuestFail;
    public string OutSignalQuestSuccess;

    public Faction SubFaction;

    private bool triggered;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTreat, "InSignalTreat");

        Scribe_Values.Look(ref OutSignalQuestFail, "OutSignalQuestFail");
        Scribe_Values.Look(ref OutSignalQuestSuccess, "OutSignalQuestSuccess");

        Scribe_Values.Look(ref triggered, "triggered", defaultValue: false);

        Scribe_References.Look(ref SubFaction, "SubFaction");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTreat = null;

        OutSignalQuestFail = null;
        OutSignalQuestSuccess = null;

        SubFaction = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!triggered && signal.tag == InSignalTreat)
        {
            triggered = true;
            if (talkWith is not null && TryTriggerQuest())
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalQuestSuccess));
            }
            else
            {
                Find.LetterStack.ReceiveLetter(label: "OARO_TaxCollector_TriggerTreatFailLabel".Translate(),
                                               text: "OARO_TaxCollector_TriggerTreatFail".Translate(),
                                               textLetterDef: LetterDefOf.NegativeEvent,
                                               lookTargets: pawns);

                Find.SignalManager.SendSignal(new Signal(OutSignalQuestFail));
            }
        }
    }

    private bool TryTriggerQuest()
    {
        try
        {
            Slate slate = new();

            slate.Set("map", talkWith.MapHeld);
            slate.Set("faction", faction);
            slate.Set(KeyLibrary_SlateStoreAs.parentFaction, faction);
            slate.Set(KeyLibrary_SlateStoreAs.subFaction, SubFaction);

            List<Pawn> slatePawns = [];
            slatePawns.AddRange(pawns);
            slate.Set("pawns", slatePawns);
            slate.Set("collector", talkWith);


            OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Mercy_TaxCollectorTreat, slate, forced: true);
            return true;
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "generating Tax Collector Treat quest",
                typeName: nameof(QuestPart_LordJob_TaxCollector),
                methodName: nameof(TryTriggerQuest),
                needStackTrace: true);
            return false;
        }
    }

    public override void TalkAction(Pawn talkWith, Pawn talker = null, bool canPostpone = true)
    {
        Map map = talkWith.MapHeld;
        Faction faction = talkWith.Faction;

        DiaNode rootNode = new("OARO_TalkWithTaxCollectorInfo".Translate(talkWith.Named(KeyLibrary_FormatArgName.TALKWITH)));

        DiaOption briberyOpt = new("OARO_TalkWithTaxCollector_Bribery".Translate());
        if (!map.HasEnoughThingsOfDef(ThingDefOf.Silver, 1000))
        {
            briberyOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.LabelCap, 1000));
        }
        else
        {
            briberyOpt.action = delegate
            {
                talkWith.MapHeld.DestoryThingsOfDef(ThingDefOf.Silver, 1000);
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "LeaveByOpt");
                DeregisterTalkAction(clearTalkWith: true);
            };
            briberyOpt.linkLateBind = () => OAFrame_DiaUtility.ConfirmDiaNode(
                text: "OARO_TalkWithTaxCollector_BriberyReply".Translate(talkWith.Named(KeyLibrary_FormatArgName.TALKWITH)),
                acceptText: "Confirm".Translate());
        }
        rootNode.options.Add(briberyOpt);

        DiaOption threatOpt = new("OARO_TalkWithTaxCollector_Threat".Translate());
        if (OrderStationHandler.Instance.OrderHallRoom is null)
        {
            threatOpt.action = delegate
            {
                TalkActionUtility.DisableLordJobTalk(talkWith, dismiss: true);
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "LeaveByOpt");
            };
            threatOpt.linkLateBind = () => OAFrame_DiaUtility.ConfirmDiaNode(
                text: "OARO_TalkWithTaxCollector_NoOrderHallLeave".Translate(talkWith.Named(KeyLibrary_FormatArgName.TALKWITH)),
                acceptText: "Confirm".Translate());
        }
        else
        {
            threatOpt.action = delegate
            {
                RecommendationUtility.UseRecommendationOfMap(talkWith.MapHeld, useCount: 1);
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "LeaveByOpt");
                DeregisterTalkAction(clearTalkWith: true);
            };
            threatOpt.linkLateBind = () => OAFrame_DiaUtility.ConfirmDiaNode(
               text: "OARO_TalkWithTaxCollector_ThreatReply".Translate(talkWith.Named(KeyLibrary_FormatArgName.TALKWITH)),
               acceptText: "Confirm".Translate());

            if (!map.HasEnoughRecommendation(count: 1))
            {
                briberyOpt.Disable("OARO_Insufficient_CurRecommendation".Translate(1.Named(KeyLibrary_FormatArgName.Count)));
            }
        }
        rootNode.options.Add(threatOpt);

        DiaOption treatOpt = new("OARO_TalkWithTaxCollector_Treat".Translate())
        {
            resolveTree = true,
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "TreatByOpt");
                DeregisterTalkAction(clearTalkWith: true);
            }
        };
        rootNode.options.Add(treatOpt);

        DiaOption rejectOpt = new("OARO_TalkWithTaxCollector_Reject".Translate())
        {
            resolveTree = true,
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "RejectByOpt");
                DeregisterTalkAction(clearTalkWith: true);
            }
        };
        rootNode.options.Add(rejectOpt);

        if (canPostpone)
            rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        Dialog_NodeTreeWithFactionInfo nodeTree = new(rootNode, talkWith.Faction);
        Find.WindowStack.Add(nodeTree);
    }
}