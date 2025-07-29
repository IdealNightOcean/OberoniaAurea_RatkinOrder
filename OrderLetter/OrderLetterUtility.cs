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

    public static OrderLetter MakeOrderLetter(TaggedString label, TaggedString text, OrderLetter.LetterType letterType, RatkinOrder relatedOrder, string sender = null)
    {
        OrderLetter orderLetter = new()
        {
            Label = label,
            Text = text,
            Sender = sender ?? "OARO_Letter_UnkownSender",
            letterType = letterType,
            relatedOrder = relatedOrder,
            // relatedFaction = relatedOrder.Faction,
            relatedFaction = relatedOrder.Faction
        };

        return orderLetter;
    }

    public static void ReadLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        if (!forceSlience && IsTransToRimLetter(letter.letterType))
        {
            Letter rimLetter = LetterMaker.MakeLetter(letter.Label, letter.Text, letter.relatedLetterDef ?? LetterDefOf.NeutralEvent, lookTargets: null, letter.relatedFaction);
            Find.LetterStack.ReceiveLetter(rimLetter);
        }
        letter.PostReaded(letterBox);
    }

    private static RimOrderLetter MakerRimLetter(OrderLetter letter)
    {
        RimOrderLetter rimLetter = (RimOrderLetter)LetterMaker.MakeLetter(letter.Label, letter.Text, letter.relatedLetterDef ?? LetterDefOf.NeutralEvent, lookTargets: null, letter.relatedFaction);
        // rimLetter.relatedOrder = letter.relatedOrder;
        return rimLetter;
    }

    public static bool IsTransToRimLetter(OrderLetter.LetterType letterType)
    {
        return letterType switch
        {
            OrderLetter.LetterType.Normal => LetterBox.autoTransNormal,
            OrderLetter.LetterType.Urgent => LetterBox.autoTransUrgent,
            OrderLetter.LetterType.Official => LetterBox.autoTransOfficial,
            _ => false,
        };
    }
}
