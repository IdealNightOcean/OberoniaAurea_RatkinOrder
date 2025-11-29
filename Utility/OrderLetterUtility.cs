using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using RelatedLetterType = OrderLetter.RelatedLetterType;

[StaticConstructorOnStartup]
public static class OrderLetterUtility
{
    public static OrderLetterBox LetterBox => OrderLetterBox.Instance;

    public static void OpenLetterBox()
    {
        Find.WindowStack.Add(new Window_OrderLetterBox());
    }

    public static OrderLetter MakeOrderLetter(
        TaggedString label,
        TaggedString text,
        OrderLetterDef def,
        RatkinOrder relatedOrder,
        Branch relatedBranch = null,
        string sender = null,
        RelatedLetterType relatedLetterType = RelatedLetterType.Neutral)
    {
        OrderLetter orderLetter = (OrderLetter)Activator.CreateInstance(def.letterClass);

        orderLetter.Def = def;
        orderLetter.Label = label;
        orderLetter.Text = text;
        orderLetter.Sender = sender ?? "OARO_Letter_UnkownSender".Translate();
        orderLetter.RelatedOrder = relatedOrder;
        orderLetter.RelatedBranch = relatedBranch;
        orderLetter.RelatedFaction = relatedOrder.Faction;
        orderLetter.RelatedLetterTypeValue = relatedLetterType;

        return orderLetter;
    }

    public static void ReceiveLetter(
        TaggedString label,
        TaggedString text,
        OrderLetterDef def,
        RatkinOrder relatedOrder,
        Branch relatedBranch = null,
        string sender = null,
        int delayDays = -1,
        RelatedLetterType relatedLetterType = RelatedLetterType.Neutral)
    {
        OrderLetter orderLetter = MakeOrderLetter(
            label: label,
            text: text,
            def: def,
            relatedOrder: relatedOrder,
            relatedBranch: relatedBranch,
            sender: sender,
            relatedLetterType: relatedLetterType);

        OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays);
    }

    public static void ReadLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        if (!forceSlience && IsTransToRimLetter(letter.Def))
        {
            ChoiceLetter rimLetter = LetterMaker.MakeLetter(letter.Label, letter.Text, letter.RelatedLetterDef, lookTargets: null, letter.RelatedFaction);
            if (rimLetter is ChoiceLetter_RatkinOrder rimOrderLetter)
            {
                rimOrderLetter.relatedOrder = letter.RelatedOrder;
            }
            Find.LetterStack.ReceiveLetter(rimLetter);
        }
        letter.PostReaded(letterBox);
    }

    public static bool IsTransToRimLetter(OrderLetterDef def)
    {
        if (!def.canShowAsRimLetter)
        {
            return false;
        }
        if (def.forceShowAsRimLetter)
        {
            return true;
        }
        else
        {
            return def.letterType switch
            {
                OrderLetterType.Normal => LetterBox.autoTransNormal,
                OrderLetterType.Urgent => LetterBox.autoTransUrgent,
                OrderLetterType.Official => LetterBox.autoTransOfficial,
                _ => false,
            };
        }
    }
}