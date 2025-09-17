using RimWorld;
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

    public static OrderLetter MakeOrderLetter(TaggedString label, TaggedString text, OrderLetterType letterType, RatkinOrder relatedOrder, string sender = null)
    {
        OrderLetter orderLetter = new()
        {
            Label = label,
            Text = text,
            Sender = sender ?? "OARO_Letter_UnkownSender",
            LetterType = letterType,
            RelatedOrder = relatedOrder,
            RelatedFaction = relatedOrder.Faction
        };

        return orderLetter;
    }

    public static void ReadLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        if (!forceSlience && IsTransToRimLetter(letter.LetterType))
        {
            Letter rimLetter = LetterMaker.MakeLetter(letter.Label, letter.Text, letter.RelatedLetterDef ?? LetterDefOf.NeutralEvent, lookTargets: null, letter.RelatedFaction);
            Find.LetterStack.ReceiveLetter(rimLetter);
        }
        letter.PostReaded(letterBox);
    }

    public static bool IsTransToRimLetter(OrderLetterType letterType)
    {
        return letterType switch
        {
            OrderLetterType.Normal => LetterBox.autoTransNormal,
            OrderLetterType.Urgent => LetterBox.autoTransUrgent,
            OrderLetterType.Official => LetterBox.autoTransOfficial,
            _ => false,
        };
    }
}
