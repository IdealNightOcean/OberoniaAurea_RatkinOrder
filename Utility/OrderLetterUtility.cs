using RimWorld;
using Verse;
using static OberoniaAurea.RatkinOrder.OrderLetter;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class OrderLetterUtility
{
    public static OrderLetterBox LetterBox => OrderLetterBox.Instance;

    public static void OpenLetterBox()
    {
        Find.WindowStack.Add(new Window_OrderLetterBox());
    }

    public static OrderLetter MakeOrderLetter(TaggedString label, TaggedString text, LetterType letterType, RatkinOrder relatedOrder, string sender = null)
    {
        OrderLetter orderLetter = new()
        {
            Label = label,
            Text = text,
            Sender = sender ?? "OARO_Letter_UnkownSender",
            LetterTypeValue = letterType,
            RelatedOrder = relatedOrder,
            RelatedFaction = relatedOrder.Faction
        };

        return orderLetter;
    }

    public static void ReceiveLetter(TaggedString label, TaggedString text, LetterType letterType, RatkinOrder relatedOrder, string sender = null)
    {
        OrderLetter orderLetter = MakeOrderLetter(label, text, letterType, relatedOrder, sender);
        OrderLetterBox.Instance.ReceiveLetter(orderLetter);
    }

    public static void ReadLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        if (!forceSlience && IsTransToRimLetter(letter.LetterTypeValue))
        {
            Letter rimLetter = LetterMaker.MakeLetter(letter.Label, letter.Text, letter.RelatedLetterDef ?? LetterDefOf.NeutralEvent, lookTargets: null, letter.RelatedFaction);
            Find.LetterStack.ReceiveLetter(rimLetter);
        }
        letter.PostReaded(letterBox);
    }

    public static bool IsTransToRimLetter(LetterType letterType)
    {
        return letterType switch
        {
            LetterType.Normal => LetterBox.autoTransNormal,
            LetterType.Urgent => LetterBox.autoTransUrgent,
            LetterType.Official => LetterBox.autoTransOfficial,
            _ => false,
        };
    }
}
