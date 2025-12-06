using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetter_SimpleAttachments : OrderLetter
{
    public List<Thing> Attachments;

    public override void PostReaded(Building_OrderLetterBox letterBox = null)
    {
        base.PostReaded(letterBox);
        SpawnAttachments(letterBox);
    }

    protected override string AttachmentInfo()
    {
        if (hasReaded)
        {
            return "OARO_Attachments_Received".Translate();
        }
        else if (!Attachments.NullOrEmpty())
        {
            return GenLabel.ThingsLabel(Attachments);
        }
        else
        {
            return "None".Translate();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Attachments, nameof(Attachments), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Attachments?.RemoveAll(t => t is null);
        }
    }

    private void SpawnAttachments(Building_OrderLetterBox letterBox)
    {
        if (Attachments.NullOrEmpty())
        {
            Attachments = null;
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
            foreach (Thing thing in Attachments)
            {
                GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
            }
        }

        Attachments = null;
    }
}