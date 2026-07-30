using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetter_SimpleAttachments : OrderLetter, IAttachments
{
    protected List<Thing> attachments;
    public List<Thing> Attachments => attachments;

    public override void PostReaded(Building_OrderLetterBox letterBox = null)
    {
        base.PostReaded(letterBox);
        SpawnAttachments(letterBox);
    }

    public void AddAttachment(Thing attachment)
    {
        attachments ??= [];
        attachments.Add(attachment);
    }

    public void AddAttachments(IEnumerable<Thing> newAttachments)
    {
        if (newAttachments is null)
        {
            return;
        }
        attachments ??= [];
        attachments.AddRange(newAttachments);
    }

    protected override string AttachmentInfo()
    {
        if (hasReaded)
        {
            return "OARO_Attachments_Received".Translate();
        }
        else if (!attachments.NullOrEmpty())
        {
            return GenLabel.ThingsLabel(attachments);
        }
        else
        {
            return "None".Translate();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref attachments, nameof(attachments), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            attachments?.RemoveAll(t => t is null);
        }
    }

    private void SpawnAttachments(Building_OrderLetterBox letterBox)
    {
        if (attachments.NullOrEmpty())
        {
            attachments = null;
            return;
        }

        Map map;
        IntVec3 pos = IntVec3.Invalid;
        bool lost = true;

        if (letterBox is not null && letterBox.Spawned)
        {
            map = letterBox.MapHeld;
            pos = letterBox.PositionHeld;
            lost = false;
        }
        else
        {
            map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
            if (map is not null)
            {
                pos = DropCellFinder.TradeDropSpot(map);
                lost = !pos.IsValid;
            }
        }

        if (lost)
        {
            Messages.Message("OARO_OrderLetter_AttachmentsLost".Translate(), MessageTypeDefOf.NegativeEvent);
        }
        else
        {
            foreach (Thing thing in attachments)
            {
                GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
            }
        }

        attachments = null;
    }
}