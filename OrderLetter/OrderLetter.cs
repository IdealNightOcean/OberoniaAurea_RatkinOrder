using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class OrderLetter : IExposable
{
    public Faction RelatedFaction;
    public RatkinOrder RelatedOrder;
    public int ArrivalTick = -1;
    public OrderLetterType LetterType = OrderLetterType.Normal;
    public LetterDef RelatedLetterDef;

    private TaggedString label;
    private TaggedString text;
    private string sender;

    public TaggedString Label
    {
        get
        {
            return label;
        }
        set
        {
            label = value.CapitalizeFirst();
        }
    }
    public TaggedString Text
    {
        get
        {
            return text;
        }
        set
        {
            text = value.CapitalizeFirst();
        }
    }
    public string Sender
    {
        get
        {
            return sender;
        }
        set
        {
            sender = value.CapitalizeFirst();
        }
    }

    public int ExpiredDays = 60;
    public bool Expired => Find.TickManager.TicksGame - ArrivalTick > ExpiredDays * 60000;

    public List<ThingDefCount> RelatedThings;
    public bool HasRelatedThings => !RelatedThings.NullOrEmpty();

    public void PostReaded(Building_OrderLetterBox letterBox = null)
    {
        if (HasRelatedThings)
        {
            TrySpawnRelatedThings(letterBox);
        }
    }

    //获取信件的基本描述（UI右下角文本）
    public string GetLetterDesc()
    {
        StringBuilder sb = new("OARO_Letter_LetterSender".Translate());
        sb.Append(sender.Translate());
        sb.AppendInNewLine("OARO_Letter_RelatedOrder".Translate());
        sb.Append(RelatedOrder is null ? "None".Translate() : RelatedOrder.Name);
        sb.AppendInNewLine("OARO_Letter_RelatedThings".Translate());
        if (HasRelatedThings)
        {
            foreach (ThingDefCount thingDefCount in RelatedThings)
            {
                sb.AppendInNewLine($"   - {thingDefCount.ThingDef.LabelCap} × {thingDefCount.Count}");
            }
        }
        else
        {
            sb.Append("None".Translate());
        }
        sb.AppendInNewLine("OARO_Letter_SendTime".Translate());
        sb.Append(GenDate.DateFullStringAt(ArrivalTick, Vector2.zero));
        return sb.ToString();
    }

    private void TrySpawnRelatedThings(Building_OrderLetterBox letterBox)
    {
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
            map = Find.AnyPlayerHomeMap;
            if (map is not null)
            {
                pos = DropCellFinder.TradeDropSpot(map);
                lost = !pos.IsValid;
            }
        }

        if (lost)
        {

        }
        else
        {
            foreach (ThingDefCount thingDefCount in RelatedThings)
            {
                Thing thing = ThingMaker.MakeThing(thingDefCount.ThingDef);
                thing.stackCount = thingDefCount.Count;
                GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
            }
        }

        RelatedThings = null;
    }

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref RelatedFaction, "RelatedFaction");
        Scribe_References.Look(ref RelatedOrder, "RelatedOrder");
        Scribe_Defs.Look(ref RelatedLetterDef, "RelatedLetterDef");

        Scribe_Values.Look(ref ArrivalTick, "ArrivalTick", -1);
        Scribe_Values.Look(ref LetterType, "LetterType", OrderLetterType.Normal);
        Scribe_Values.Look(ref label, "label");
        Scribe_Values.Look(ref text, "text");
        Scribe_Values.Look(ref sender, "sender");

        Scribe_Collections.Look(ref RelatedThings, "RelatedThings", LookMode.Deep);
    }
}