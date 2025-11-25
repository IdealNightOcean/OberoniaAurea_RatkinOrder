using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class OrderLetter : IExposable
{
    public OrderLetterDef Def;

    public Faction RelatedFaction;
    public RatkinOrder RelatedOrder;
    public int ArrivalTick = -1;

    protected bool hasReaded;
    protected TaggedString label;
    protected TaggedString text;
    protected string sender;

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

    public void OnReaded() => hasReaded = true;
    public virtual void PostReaded(Building_OrderLetterBox letterBox = null) { }

    //获取信件的基本描述（UI右下角文本）
    public string GetLetterDesc()
    {
        StringBuilder sb = new("OARO_Letter_LetterSender".Translate());
        sb.AppendLine(sender.Translate());
        sb.Append("OARO_Letter_RelatedOrder".Translate());
        sb.AppendLine(RelatedOrder is null ? "None".Translate() : RelatedOrder.Name);
        sb.Append("OARO_Letter_RelatedThings".Translate());
        sb.AppendLine(AttachmentInfo());
        sb.Append("OARO_Letter_SendTime".Translate());
        sb.AppendLine(GenDate.DateFullStringAt(ArrivalTick, Vector2.zero));
        return sb.ToString();
    }

    protected virtual string AttachmentInfo()
    {
        return "None".Translate();
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref Def, "Def");

        Scribe_References.Look(ref RelatedFaction, "RelatedFaction");
        Scribe_References.Look(ref RelatedOrder, "RelatedOrder");

        Scribe_Values.Look(ref hasReaded, "hasReaded", defaultValue: false);
        Scribe_Values.Look(ref ArrivalTick, "ArrivalTick", -1);
        Scribe_Values.Look(ref label, "label");
        Scribe_Values.Look(ref text, "text");
        Scribe_Values.Look(ref sender, "sender");
    }
}