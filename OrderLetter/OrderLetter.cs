using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class OrderLetter : IExposable
{
    public static readonly Texture2D Texture_Letter_Type_A = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_LetterA", true);
    public static readonly Texture2D Texture_Letter_Type_B = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_LetterB", true);
    public static readonly Texture2D Texture_Letter_Type_C = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_LetterC", true);
    public enum LetterType
    {
        Normal,
        Urgent,
        Official,
    }
    public Texture2D Icon => letterType switch
    {
        LetterType.Normal => Texture_Letter_Type_A,
        LetterType.Urgent => Texture_Letter_Type_B,
        LetterType.Official => Texture_Letter_Type_C,
        _ => Texture_Letter_Type_A,
    };

    public Faction relatedFaction;
    public RatkinOrder relatedOrder;
    public int arrivalTick = -1;
    public LetterType letterType = LetterType.Normal;
    public LetterDef relatedLetterDef;

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

    public int expiredDays = 60;
    public bool Expired => Find.TickManager.TicksGame - arrivalTick > expiredDays * 60000;

    public List<ThingDefCount> relatedThings;
    public bool HasRelatedThings => !relatedThings.NullOrEmpty();

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
        sb.Append(relatedOrder is null ? "None".Translate() : relatedOrder.Name);
        sb.AppendInNewLine("OARO_Letter_RelatedThings".Translate());
        if (HasRelatedThings)
        {
            foreach (ThingDefCount thingDefCount in relatedThings)
            {
                sb.AppendInNewLine($"   - {thingDefCount.ThingDef.LabelCap} × {thingDefCount.Count}");
            }
        }
        else
        {
            sb.Append("None".Translate());
        }
        sb.AppendInNewLine("OARO_Letter_SendTime".Translate());
        sb.Append(GenDate.DateFullStringAt(arrivalTick, Vector2.zero));
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
            foreach (ThingDefCount thingDefCount in relatedThings)
            {
                Thing thing = ThingMaker.MakeThing(thingDefCount.ThingDef);
                thing.stackCount = thingDefCount.Count;
                GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
            }
        }

        relatedThings = null;
    }

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref relatedFaction, "relatedFaction");
        // Scribe_References.Look(ref relatedOrder, "relatedOrder");
        Scribe_Defs.Look(ref relatedLetterDef, "relatedLetterDef");

        Scribe_Values.Look(ref arrivalTick, "arrivalTick", -1);
        Scribe_Values.Look(ref letterType, "letterType", LetterType.Normal);
        Scribe_Values.Look(ref label, "label");
        Scribe_Values.Look(ref text, "text");
        Scribe_Values.Look(ref sender, "sender");

        Scribe_Collections.Look(ref relatedThings, "relatedThings", LookMode.Deep);
    }
}