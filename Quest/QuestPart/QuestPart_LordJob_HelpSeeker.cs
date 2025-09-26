using RimWorld;
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
    public string OutSignalReject;

    public QuestScriptDef MmercyQuestDef;
    public Faction SubFaction;
    public Faction ParentFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalAccept, "OutSignalAccept");
        Scribe_Values.Look(ref OutSignalReject, "OutSignalReject");

        Scribe_Defs.Look(ref MmercyQuestDef, "MmercyQuestDef");

        Scribe_References.Look(ref SubFaction, "SubFaction");
        Scribe_References.Look(ref ParentFaction, "ParentFaction");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OutSignalAccept = null;
        OutSignalReject = null;
        MmercyQuestDef = null;
        SubFaction = null;
        ParentFaction = null;
    }

    public override void TalkAction(Pawn talker, Pawn talkWith)
    {
        Find.WindowStack.Add(HelpQuizNodeTree(talkWith));
    }

    private Dialog_NodeTree HelpQuizNodeTree(Pawn talkWith)
    {
        DiaNode rootNode = new(GetTalkText(MmercyQuestDef));
        DiaOption acceptOpt = new("OARO_TalkWithHelpSeeker_Accept".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalAccept, talkWith.Named("SUBJECT"), MmercyQuestDef.Named("QUEST")));
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            resolveTree = true
        };

        DiaOption rejectOpt = new("OARO_TalkWithHelpSeeker_Reject".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalReject, talkWith.Named("SUBJECT"), MmercyQuestDef.Named("QUEST")));
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            resolveTree = true
        };
        DiaOption ignoreOpt = new("OARO_TalkWithHelpSeeker_Ignore".Translate())
        {
            resolveTree = true
        };

        rootNode.options.Add(acceptOpt);
        rootNode.options.Add(rejectOpt);
        rootNode.options.Add(ignoreOpt);

        return new Dialog_NodeTreeWithFactionInfo(rootNode, talkWith.Faction);
    }

    private TaggedString GetTalkText(QuestScriptDef mercyQuest)
    {
        TaggedString talkText;
        MercyQuestExtension mercyQuestExtension = mercyQuest.GetModExtension<MercyQuestExtension>();
        if (mercyQuestExtension is null)
        {
            talkText = "OARK_RatkinMercyQuest_HelpSeekDefault".Translate(TextNamedArguments());
        }
        else
        {
            if (mercyQuestExtension.fixedQuestDesc is not null)
            {
                talkText = mercyQuestExtension.fixedQuestDesc.Formatted(TextNamedArguments());
            }
            else if (mercyQuestExtension.questDescMaker is null)
            {
                talkText = "OARK_RatkinMercyQuest_HelpSeekDefault".Translate(TextNamedArguments());
            }
            else
            {
                GrammarRequest grammarRequest = new();
                grammarRequest.Includes.Add(mercyQuestExtension.questDescMaker);
                grammarRequest.Rules.AddRange(GrammarUtility.RulesForPawn("HELPSEEKER", TalkWith));
                grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("SUBFACTION", SubFaction));
                if (ParentFaction is not null)
                {
                    grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("PARENTFACTION", ParentFaction));
                }
                talkText = GrammarResolver.Resolve("r_text", grammarRequest);
            }
        }
        return talkText;
    }

    private NamedArgument[] TextNamedArguments()
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
