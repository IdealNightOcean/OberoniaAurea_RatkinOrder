using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame;
using RimWorld.QuestGen;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_RimOrderLetter : QuestNode_ChoiceLetter
{
    public SlateRef<RatkinOrder> ratkinOrder;

    protected override Type PartClass => partClass.GetValue(QuestGen.slate) ?? typeof(QuestPart_RimOrderLetter);

    protected override void PostGeneratePart(QuestPart_ChoiceLetter questPart_ChoiceLetter)
    {
        if (questPart_ChoiceLetter is QuestPart_RimOrderLetter rimOrderLetterPart)
        {
            rimOrderLetterPart.RelatedOrder = ratkinOrder.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<RatkinOrder>(OARO_KeyLibrary_SlateStoreAs.ratkinOrder);
        }
    }
}

public class QuestPart_RimOrderLetter : QuestPart_ChoiceLetter
{
    public RatkinOrder RelatedOrder;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref RelatedOrder, nameof(RelatedOrder));
    }

    protected override void PostGenerateLetter(ChoiceLetter choiceLetter, out bool letterValid)
    {
        letterValid = true;
        if (choiceLetter is ChoiceLetter_RatkinOrder rimOrderLetter)
        {
            rimOrderLetter.RelatedOrder = RelatedOrder;
        }
    }
}