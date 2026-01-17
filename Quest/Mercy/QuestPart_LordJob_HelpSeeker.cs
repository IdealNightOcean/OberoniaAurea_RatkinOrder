using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 善行求助者的 LordJob | TalkAction
/// </summary>
public class QuestPart_LordJob_HelpSeeker : QuestPart_LordJob_CommomTalk
{
    public string OutSignalAccept;
    public string OutSignalTransfer;
    public string OutSignalTransferWithHelp;
    public string OutSignalReject;

    public string OutSignalTalkTextReset;

    public MercyQuestDef MercyQuestDef;
    public Faction SubFaction;
    public Faction ParentFaction;

    public string RawTalkText;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalAccept, nameof(OutSignalAccept));
        Scribe_Values.Look(ref OutSignalTransfer, nameof(OutSignalTransfer));
        Scribe_Values.Look(ref OutSignalTransferWithHelp, nameof(OutSignalTransferWithHelp));
        Scribe_Values.Look(ref OutSignalReject, nameof(OutSignalReject));

        Scribe_Values.Look(ref OutSignalTalkTextReset, nameof(OutSignalTalkTextReset));

        Scribe_Values.Look(ref RawTalkText, nameof(RawTalkText));

        Scribe_Defs.Look(ref MercyQuestDef, nameof(MercyQuestDef));

        Scribe_References.Look(ref SubFaction, nameof(SubFaction));
        Scribe_References.Look(ref ParentFaction, nameof(ParentFaction));
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OutSignalAccept = null;
        OutSignalTransfer = null;
        OutSignalTransferWithHelp = null;
        OutSignalReject = null;

        OutSignalTalkTextReset = null;

        RawTalkText = null;

        MercyQuestDef = null;
        SubFaction = null;
        ParentFaction = null;
    }

    protected override void ForceTriggerTalk()
    {
        if (RatkinOrderSettings.MercyPreQuestForceDecision)
        {
            base.ForceTriggerTalk();
        }
    }

    public override void TalkAction(Pawn talkWith, Pawn talker = null, bool canPostpone = true)
    {
        DiaNode rootNode = new(GetTalkText());
        DiaOption acceptOpt = new("OARO_TalkWithHelpSeeker_Accept".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalAccept, talkWith.Named(KeyLibrary_FormatArgName.SUBJECT), MercyQuestDef.Named(KeyLibrary_FormatArgName.MERCYQUEST)));
                DeregisterTalkAction(clearTalkWith: true);
            },
            resolveTree = true
        };
        DiaOption transferOpt = new("OARO_TalkWithHelpSeeker_Transfer".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalTransfer, talkWith.Named(KeyLibrary_FormatArgName.SUBJECT), MercyQuestDef.Named(KeyLibrary_FormatArgName.MERCYQUEST)));
                DeregisterTalkAction(clearTalkWith: true);
            },
            resolveTree = true
        };
        if (BranchUtility.GetAllAvailableBranches(b => b.IsBranchOfType(Branch.BranchType.Friendly)).NullOrEmpty())
        {
            transferOpt.Disable("OARO_NoAnyFriendlyBranch".Translate());
        }

        DiaOption transferWithHelpOpt = new("OARO_TalkWithHelpSeeker_TransferWithHelp".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalTransferWithHelp, talkWith.Named(KeyLibrary_FormatArgName.SUBJECT), MercyQuestDef.Named(KeyLibrary_FormatArgName.MERCYQUEST)));
                talkWith.MapHeld?.DestoryThingsOfDef(ThingDefOf.Silver, 200);
                DeregisterTalkAction(clearTalkWith: true);
            },
            resolveTree = true
        };
        if (!talkWith.MapHeld.HasEnoughThingsOfDef(ThingDefOf.Silver, 200))
        {
            transferWithHelpOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.LabelCap, 200));
        }

        DiaOption rejectOpt = new("OARO_TalkWithHelpSeeker_Reject".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalReject, talkWith.Named(KeyLibrary_FormatArgName.SUBJECT), MercyQuestDef.Named(KeyLibrary_FormatArgName.MERCYQUEST)));
                DeregisterTalkAction(clearTalkWith: true);
            },
            resolveTree = true
        };

        rootNode.options.Add(acceptOpt);
        rootNode.options.Add(rejectOpt);
        rootNode.options.Add(transferOpt);
        rootNode.options.Add(transferWithHelpOpt);
        if (canPostpone)
            rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        Dialog_NodeTreeWithFactionInfo nodeTree = new(rootNode, talkWith.Faction);
        Find.WindowStack.Add(nodeTree);
    }

    private TaggedString GetTalkText()
    {
        if (!string.IsNullOrEmpty(RawTalkText))
        {
            try
            {
                return RawTalkText.Formatted(TextNamedArguments());
            }
            catch (Exception ex1)
            {
                Log.Warning($"[OARO] 在 {nameof(QuestPart_LordJob_HelpSeeker)}.{nameof(GetTalkText)}() 中格式化 {nameof(RawTalkText)} 失败。异常：\n {ex1}");
            }
        }

        if (MercyQuestDef is null)
        {
            return "OARK_RatkinMercyQuest_HelpSeekDefault".Translate(TextNamedArguments());
        }
        else
        {
            if (!string.IsNullOrEmpty(MercyQuestDef.fixedHelpDesc))
            {
                return MercyQuestDef.fixedHelpDesc.Formatted(TextNamedArguments());
            }
            if (MercyQuestDef.helpDescRulePack is not null)
            {
                try
                {
                    GrammarRequest grammarRequest = new();
                    grammarRequest.Includes.Add(MercyQuestDef.helpDescRulePack);
                    grammarRequest.Rules.AddRange(GrammarUtility.RulesForPawn("HELPSEEKER", TalkWith));
                    grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("SUBFACTION", SubFaction));
                    if (ParentFaction is not null)
                    {
                        grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("PARENTFACTION", ParentFaction));
                    }
                    return GrammarResolver.Resolve("r_text", grammarRequest);
                }
                catch (Exception ex2)
                {
                    ModUtility.LogExceptionError(ex2,
                        errorDesc: "解析对话文本",
                        typeName: nameof(QuestPart_LordJob_HelpSeeker),
                        methodName: nameof(GetTalkText),
                        needStackTrace: true);
                }
            }

            return "OARK_RatkinMercyQuest_HelpSeekDefault".Translate(TextNamedArguments());
        }

    }

    public void SetRawTalkText(string talkText)
    {
        RawTalkText = talkText;
        if (quest.State == QuestState.Ongoing && !string.IsNullOrEmpty(OutSignalTalkTextReset))
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalTalkTextReset));
        }
    }

    protected NamedArgument[] TextNamedArguments()
    {
        List<NamedArgument> arguments =
        [
            new NamedArgument(TalkWith, "HELPSEEKER"),
            new NamedArgument(SubFaction, "SUBFACTION")
        ];

        if (ParentFaction is not null)
        {
            arguments.Add(new NamedArgument(ParentFaction, "PARENTFACTION"));
        }
        return arguments.ToArray();
    }
}
