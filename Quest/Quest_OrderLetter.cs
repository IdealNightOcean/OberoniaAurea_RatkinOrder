using OberoniaAurea.RatkinOrder;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea_Frame;

public class QuestNode_OrderLetter : QuestNode
{
    protected static Type defaultPartClass = typeof(QuestPart_OrderLetter);

    public SlateRef<Type> partClass = defaultPartClass;
    protected virtual Type PartClass
    {
        get
        {
            return partClass.GetValue(QuestGen.slate) ?? defaultPartClass;
        }
    }

    [NoTranslate]
    public SlateRef<string> inSignal;
    public SlateRef<OrderLetterDef> orderLetterDef;
    public SlateRef<OrderLetter.RelatedLetterType?> relatedLetterType;

    public SlateRef<RatkinOrder> relatedOrder;
    public SlateRef<Branch> relatedBranch;

    public SlateRef<string> label;
    public SlateRef<string> text;
    public SlateRef<string> sender;

    public SlateRef<RulePack> labelRules;
    public SlateRef<RulePack> textRules;

    public SlateRef<int> delayDays = -1;

    public SlateRef<IEnumerable<Thing>> attachments;
    public SlateRef<QuestPart.SignalListenMode?> signalListenMode;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_OrderLetter questPart_OrderLetter = (QuestPart_OrderLetter)Activator.CreateInstance(PartClass);

        questPart_OrderLetter.InSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal);
        questPart_OrderLetter.OrderLetterDef = orderLetterDef.GetValue(slate) ?? OrderLetterDefOf.OARO_UrgentLetter;
        questPart_OrderLetter.RelatedLetterType = relatedLetterType.GetValue(slate) ?? OrderLetter.RelatedLetterType.Neutral;

        questPart_OrderLetter.RawLabel = "error";
        questPart_OrderLetter.RawText = "error";
        questPart_OrderLetter.RawSender = "Unkown";

        questPart_OrderLetter.DelayDays = delayDays.GetValue(slate);

        questPart_OrderLetter.RelatedOrder = relatedOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.ratkinOrder);
        questPart_OrderLetter.RelatedBranch = relatedBranch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.branch);

        questPart_OrderLetter.signalListenMode = signalListenMode.GetValue(slate).GetValueOrDefault();
        questPart_OrderLetter.InitLetterTextRequest(label.GetValue(slate), text.GetValue(slate), sender.GetValue(slate), labelRules.GetValue(slate), textRules.GetValue(slate));
        questPart_OrderLetter.InitAttachments(attachments.GetValue(slate));

        PostGeneratePart(questPart_OrderLetter);
        QuestGen.quest.AddPart(questPart_OrderLetter);
    }

    protected virtual void PostGeneratePart(QuestPart_OrderLetter questPart_OrderLetter) { }
}

public class QuestPart_OrderLetter : QuestPart
{
    protected const string RootSymbol = "root";

    public string InSignal;

    public string RawLabel;
    public string RawText;
    public string RawSender;

    public OrderLetterDef OrderLetterDef;
    public OrderLetter.RelatedLetterType RelatedLetterType;

    public int DelayDays = -1;

    public RatkinOrder RelatedOrder;
    public Branch RelatedBranch;

    protected List<Thing> attachments;

    public void InitLetterTextRequest(string label, string text, string sender, RulePack labelRules = null, RulePack textRules = null)
    {
        Slate slate = QuestGen.slate;
        QuestGen.AddTextRequest(RootSymbol, delegate (string x)
        {
            RawLabel = x;
        }, QuestGenUtility.MergeRules(labelRules, label, RootSymbol));
        QuestGen.AddTextRequest(RootSymbol, delegate (string x)
        {
            RawText = x;
        }, QuestGenUtility.MergeRules(textRules, text, RootSymbol));
        QuestGen.AddTextRequest(RootSymbol, delegate (string x)
        {
            RawSender = x;
        }, QuestGenUtility.MergeRules(null, sender, RootSymbol));
    }

    public void InitAttachments(IEnumerable<Thing> attachments)
    {
        if (attachments is null)
        {
            return;
        }

        this.attachments ??= [];
        this.attachments.AddRange(attachments);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignal, nameof(InSignal));

        Scribe_Values.Look(ref RawLabel, nameof(RawLabel));
        Scribe_Values.Look(ref RawText, nameof(RawText));
        Scribe_Values.Look(ref RawSender, nameof(RawSender));
        Scribe_Defs.Look(ref OrderLetterDef, nameof(OrderLetterDef));
        Scribe_Values.Look(ref RelatedLetterType, nameof(RelatedLetterType), defaultValue: OrderLetter.RelatedLetterType.Neutral);

        Scribe_Values.Look(ref DelayDays, nameof(DelayDays), -1);

        Scribe_References.Look(ref RelatedOrder, nameof(RelatedOrder));
        Scribe_References.Look(ref RelatedBranch, nameof(RelatedBranch));

        Scribe_Collections.Look(ref attachments, nameof(attachments), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            attachments?.RemoveAll(t => t is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignal = null;

        RawLabel = null;
        RawText = null;
        RawSender = null;
        OrderLetterDef = null;

        RelatedOrder = null;
        RelatedBranch = null;

        attachments = null;
    }

    public override IEnumerable<Faction> InvolvedFactions
    {
        get
        {
            foreach (Faction involvedFaction in base.InvolvedFactions)
            {
                yield return involvedFaction;
            }
            if (RelatedOrder is not null)
            {
                yield return RelatedOrder.Faction;
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != InSignal)
        {
            return;
        }

        OrderLetter orderLetter = OrderLetterUtility.MakeOrderLetter(
            label: signal.args.GetFormattedText(RawLabel),
            text: signal.args.GetFormattedText(RawText),
            def: OrderLetterDef,
            relatedOrder: RelatedOrder,
            relatedBranch: RelatedBranch,
            sender: signal.args.GetFormattedText(RawSender),
            relatedLetterType: RelatedLetterType);

        if (attachments is not null && orderLetter is IAttachments attachmentsLetter)
        {
            attachmentsLetter.AddAttachments(attachments);
            attachments = null;
        }

        PostGenerateLetter(orderLetter);

        OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays: DelayDays);
    }

    protected virtual void PostGenerateLetter(OrderLetter orderLetter) { }
}