using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_LordJob_TaxCollector : QuestPart_LordJob_CommomTalk
{
    public string inSignalTreat;

    public string outSignalQuestFail;
    public string outSignalQuestSuccess;

    public Faction subFaction;

    private bool triggered;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalTreat, "inSignalTreat");

        Scribe_Values.Look(ref outSignalQuestFail, "outSignalQuestFail");
        Scribe_Values.Look(ref outSignalQuestSuccess, "outSignalQuestSuccess");

        Scribe_Values.Look(ref triggered, "triggered", defaultValue: false);

        Scribe_References.Look(ref subFaction, "subFaction");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalTreat = null;

        outSignalQuestFail = null;
        outSignalQuestSuccess = null;

        subFaction = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!triggered && signal.tag == inSignalTreat)
        {
            triggered = true;
            if (talkWith is not null && TryTriggerQuest())
            {
                Find.SignalManager.SendSignal(new Signal(outSignalQuestSuccess));
            }
            else
            {
                Find.LetterStack.ReceiveLetter(label: "OARO_TaxCollector_TriggerTreatFailLabel".Translate(),
                                               text: "OARO_TaxCollector_TriggerTreatFail".Translate(),
                                               textLetterDef: LetterDefOf.NegativeEvent,
                                               lookTargets: pawns);

                Find.SignalManager.SendSignal(new Signal(outSignalQuestFail));
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
            slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFactionStoreAs, subFaction);

            List<Pawn> slatePawns = [];
            slatePawns.AddRange(pawns);
            slate.Set("pawns", slatePawns);
            slate.Set("collector", talkWith);


            OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Mercy_TaxCollectorTreat, slate, forced: true);
            return true;
        }
        catch (System.Exception ex)
        {
            Log.Error($"Failed to generate Tax Collector Treat quest: {ex.Message}");
            return false;
        }
    }

    public override void TalkAction(Pawn talker, Pawn talkWith)
    {
        Find.WindowStack.Add(TalkDialog(talker, talkWith));
    }

    private static Dialog_NodeTree TalkDialog(Pawn talker, Pawn talkWith)
    {
        Map map = talkWith.MapHeld;
        List<Thing> silvers = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
        int totalSilverCount = 0;
        for (int i = 0; i < silvers.Count; i++)
        {
            totalSilverCount += silvers[i].stackCount;
        }

        DiaNode rootNode = new("OARO_TalkWithTaxCollectorInfo".Translate(talkWith));

        DiaOption briberyOpt = new("OARO_TalkWithTaxCollector_Bribery".Translate());
        if (totalSilverCount < 1000)
        {
            briberyOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.LabelCap, 1000));
        }
        else
        {
            briberyOpt.action = delegate
            {
                talkWith.MapHeld.DestoryThingsOfDef(ThingDefOf.Silver, 1000);
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "LeaveByOpt");
                TalkActionUtility.DisableLordJobTalk(talkWith);
            };
            briberyOpt.linkLateBind = () => OAFrame_DiaUtility.ConfirmDiaNode("OARO_TalkWithTaxCollector_BriberyReply".Translate(talkWith), acceptText: "Confirm".Translate());
        }
        rootNode.options.Add(briberyOpt);

        DiaOption threatOpt = new("OARO_TalkWithTaxCollector_Threat".Translate())
        {
            action = delegate
            {
                //临时占位，后续应改成其他方式
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "LeaveByOpt");
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            linkLateBind = () => OAFrame_DiaUtility.ConfirmDiaNode("OARO_TalkWithTaxCollector_ThreatReply".Translate(talkWith), acceptText: "Confirm".Translate())
        };

        DiaOption treatOpt = new("OARO_TalkWithTaxCollector_Treat".Translate())
        {
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "TreatByOpt");
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            resolveTree = true
        };
        rootNode.options.Add(treatOpt);

        DiaOption ignoreOpt = new("OARO_TalkWithTaxCollector_Ignore".Translate())
        {
            resolveTree = true
        };
        rootNode.options.Add(ignoreOpt);

        return new Dialog_NodeTree(rootNode);
    }
}