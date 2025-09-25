using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 滞销工艺品村庄（特化类）
/// </summary>
public sealed class WorldObject_UnsalableArtcraft : WorldObject_Interactive_Nameable, IThingHolder
{
    private ThingOwner<Thing> sculptures;
    private float remainingMarkerValue;
    private int purchasedCount;
    private int totalCount;
    private int SculpturesCount => sculptures.Count;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref sculptures, "sculptures");
        Scribe_Values.Look(ref remainingMarkerValue, "remainingMarkerValue", 0f);
        Scribe_Values.Look(ref purchasedCount, "purchasedCount", 0);
        Scribe_Values.Look(ref totalCount, "totalCount", 0);
    }

    public override void PostMake()
    {
        base.PostMake();
        remainingMarkerValue = 0f;
        sculptures = new ThingOwner<Thing>(this);

        int sculpturesCount = Rand.RangeInclusive(15, 30);
        for (int i = 0; i < sculpturesCount; i++)
        {
            Thing sculpture = ThingMaker.MakeThing(OARO_RimWorldDefOf.SculptureSmall, ThingDefOf.WoodLog);
            sculpture.TryGetComp<CompQuality>()?.SetQuality(Rand.Bool ? QualityCategory.Good : QualityCategory.Excellent, ArtGenerationContext.Outsider);

            remainingMarkerValue += (sculpture.MarketValue * 0.7f);

            Thing sculptureMini = sculpture.TryMakeMinified();
            sculptures.TryAdd(sculptureMini);
        }

        totalCount = SculpturesCount;
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_UnsalableArtcraft_Remaining".Translate(SculpturesCount, remainingMarkerValue.ToString("F0")));
        sb.AppendInNewLine("OARO_UnsalableArtcraft_Purchased".Translate(purchasedCount));
        return sb.ToString();
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        OpenPurchaseNode(caravan);
    }

    private void OpenPurchaseNode(Caravan caravan)
    {
        DiaNode rootNode = new("OARO_UnsalableArtcraft_PurchaseInfo".Translate());

        AddSilverOpt(100);
        AddSilverOpt(500);
        AddSilverOpt(1000);
        AddSilverOpt(2000);

        DiaOption totalOpt = new("OARO_UnsalableArtcraft_PurchaseOpt_TryBest".Translate())
        {
            action = () => PurchaseOfSculptures(caravan, -1),
            resolveTree = true
        };
        rootNode.options.Add(totalOpt);

        rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(rootNode, Faction));

        void AddSilverOpt(int silverNeed)
        {
            DiaOption subOpt = new("OARO_UnsalableArtcraft_PurchaseOpt".Translate(silverNeed))
            {
                action = () => PurchaseOfSculptures(caravan, silverNeed),
                resolveTree = true
            };
            rootNode.options.Add(subOpt);
        }
    }

    private void PurchaseOfSculptures(Caravan caravan, int silverCountNeed = -1)
    {
        silverCountNeed = silverCountNeed > 0 ? silverCountNeed : Mathf.CeilToInt(remainingMarkerValue);
        OAFrame_CaravanUtility.TakeThingsOfDef(caravan, ThingDefOf.Silver, silverCountNeed, out int caravanSilver);

        List<Thing> inventoryItems = CaravanInventoryUtility.AllInventoryItems(caravan);

        List<Thing> sculptures = [];
        float usedSilver = 0f;
        for (int j = 0; j < SculpturesCount; j++)
        {
            float marketValue = sculptures[j].MarketValue * 0.7f;
            if (usedSilver + marketValue > caravanSilver)
            {
                break;
            }
            usedSilver += marketValue;
            sculptures.Add(sculptures[j]);
        }

        foreach (Thing t in sculptures)
        {
            sculptures.Remove(t);
            CaravanInventoryUtility.GiveThing(caravan, t);
        }
        OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.Silver, (int)usedSilver);

        purchasedCount += sculptures.Count;

        OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_UnsalableArtcraft_PurchaseResult".Translate(sculptures.Count, usedSilver.ToString("F0")));

        if (SculpturesCount == 0)
        {
            remainingMarkerValue = 0f;
            purchasedCount = totalCount;
            if (!Destroyed)
            {
                Destroy();
            }
        }
        else
        {
            remainingMarkerValue = 0f;
            for (int k = 0; k < SculpturesCount; k++)
            {
                remainingMarkerValue += (sculptures[k].MarketValue * 0.7f);
            }
        }
    }

    public override void Destroy()
    {
        float percentage = purchasedCount / totalCount;
        if (percentage >= 0.99f)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "PerfectRequestFulfilled", this.Named("SUBJECT"));
        }
        else if (percentage >= 0.5f)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "RequestFulfilled", this.Named("SUBJECT"));
        }
        else if (percentage > 0f)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "BarelyRequestFulfilled", this.Named("SUBJECT"));
        }
        base.Destroy();
    }


    public ThingOwner GetDirectlyHeldThings()
    {
        return sculptures;
    }
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }
}