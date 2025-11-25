using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderLetterUtility
{
    public static OrderLetterBox LetterBox => OrderLetterBox.Instance;

    public static void OpenLetterBox()
    {
        Find.WindowStack.Add(new Window_OrderLetterBox());
    }

    public static OrderLetter MakeOrderLetter(TaggedString label, TaggedString text, OrderLetterDef def, RatkinOrder relatedOrder, string sender = null)
    {
        OrderLetter orderLetter = (OrderLetter)Activator.CreateInstance(def.letterClass);

        orderLetter.Def = def;
        orderLetter.Label = label;
        orderLetter.Text = text;
        orderLetter.Sender = sender ?? "OARO_Letter_UnkownSender".Translate();
        orderLetter.RelatedOrder = relatedOrder;
        orderLetter.RelatedFaction = relatedOrder.Faction;

        return orderLetter;
    }

    public static void ReceiveLetter(TaggedString label, TaggedString text, OrderLetterDef def, RatkinOrder relatedOrder, string sender = null, int delayDays = -1)
    {
        OrderLetter orderLetter = MakeOrderLetter(label, text, def, relatedOrder, sender);
        OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays);
    }

    public static void ReadLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        if (!forceSlience && IsTransToRimLetter(letter.Def))
        {
            ChoiceLetter rimLetter = LetterMaker.MakeLetter(letter.Label, letter.Text, letter.Def.relatedLetterDef ?? LetterDefOf.NeutralEvent, lookTargets: null, letter.RelatedFaction);
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