using RimWorld;
using System;
using System.Collections.Generic;
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
        orderLetter.RelatedFaction = relatedOrder?.Faction;
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

    public static OrderLetter MakeSpecialLetter(SpecialLetterDefBase letterDef)
    {
        OrderLetter specialLetter = MakeOrderLetter(
            label: letterDef.labelOverride ?? letterDef.label,
            text: letterDef.text,
            def: letterDef.relatedOrderLetterDef,
            sender: letterDef.sender,
            relatedOrder: null,
            relatedLetterType: letterDef.relatedLetterType);

        if (!letterDef.attachments.NullOrEmpty())
        {
            if (specialLetter is IAttachments attachmentsLetter)
            {
                foreach (ThingDefCountClass tdcc in letterDef.attachments)
                {
                    Thing thing = ThingMaker.MakeThing(tdcc.thingDef);
                    thing.stackCount = tdcc.count;
                    attachmentsLetter.AddAttachment(thing);
                }
            }
            else
            {
                Log.Error($"[OARO] {letterDef.defName} 有附件，但其信件类 {specialLetter.GetType().Name} 未实现 {nameof(IAttachments)} 接口。");
            }
        }

        return specialLetter;
    }

    public static OrderLetter MakeDailyOrderLetter(DailyOrderLetterDef letterDef, RatkinOrder ratkinOrder, Branch branch = null)
    {
        List<NamedArgument> args =
        [
            ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName),
        ];
        if (branch is not null)
        {
            args.Add(branch.NameColored.Named(KeyLibrary_FormatArgName.BranchName));
        }
        NamedArgument[] argsArr = args.ToArray();

        OrderLetter dailyOrderLetter = MakeOrderLetter(
            label: (letterDef.labelOverride ?? letterDef.label).Formatted(argsArr),
            text: letterDef.text.Formatted(argsArr),
            def: letterDef.relatedOrderLetterDef,
            relatedOrder: ratkinOrder,
            relatedBranch: branch,
            sender: letterDef.sender ?? (branch is null ? ratkinOrder.Name : branch.Name),
            relatedLetterType: letterDef.relatedLetterType);

        if (!letterDef.attachments.NullOrEmpty())
        {
            if (dailyOrderLetter is IAttachments attachmentsLetter)
            {
                foreach (ThingDefCountClass tdcc in letterDef.attachments)
                {
                    Thing thing = ThingMaker.MakeThing(tdcc.thingDef);
                    thing.stackCount = tdcc.count;
                    attachmentsLetter.AddAttachment(thing);
                }
            }
            else
            {
                Log.Error($"[OARO] {letterDef.defName} 有附件，但其信件类 {dailyOrderLetter.GetType().Name} 未实现 {nameof(IAttachments)} 接口。");
            }
        }

        return dailyOrderLetter;
    }

    public static void ReadLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        if (!forceSlience && IsTransToRimLetter(letter.Def))
        {
            ChoiceLetter rimLetter = LetterMaker.MakeLetter(
                label: letter.Label,
                text: letter.Text,
                def: letter.RelatedLetterDef,
                lookTargets: null,
                relatedFaction: letter.RelatedFaction);

            if (rimLetter is ChoiceLetter_RatkinOrder rimOrderLetter)
            {
                rimOrderLetter.RelatedOrder = letter.RelatedOrder;
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