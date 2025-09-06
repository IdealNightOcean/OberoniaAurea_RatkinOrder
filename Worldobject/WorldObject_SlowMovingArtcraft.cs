using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 滞销工艺品村庄（特化类）
/// </summary>
public sealed class WorldObject_SlowMovingArtcraft : WorldObject_Interactive_Nameable, IThingHolder
{
    private ThingOwner<Thing> sculptures;
    private float totalMarkerValue;
    private int soldCount;
    private int totalCount;
    private int SculpturesCount => sculptures.Count;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref sculptures, "sculptures");
        Scribe_Values.Look(ref totalMarkerValue, "totalMarkerValue", 0f);
        Scribe_Values.Look(ref soldCount, "soldCount", 0);
        Scribe_Values.Look(ref totalCount, "totalCount", 0);
    }

    public override void PostMake()
    {
        base.PostMake();
        totalMarkerValue = 0f;
        sculptures = new ThingOwner<Thing>(this);

        int sculpturesCount = Rand.RangeInclusive(15, 30);
        for (int i = 0; i < sculpturesCount; i++)
        {
            Thing sculpture = ThingMaker.MakeThing(OARO_RimWorldDefOf.SculptureSmall, ThingDefOf.WoodLog);
            sculpture.TryGetComp<CompQuality>()?.SetQuality(Rand.Bool ? QualityCategory.Good : QualityCategory.Excellent, ArtGenerationContext.Outsider);

            totalMarkerValue += (sculpture.MarketValue * 0.7f);

            Thing sculptureMini = sculpture.TryMakeMinified();
            sculptures.TryAdd(sculptureMini);
        }

        totalCount = SculpturesCount;
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {

    }

    private void PurchaseOfSculptures(Caravan caravan)
    {
        float caravanSilver = 0;
        List<Thing> inventoryItems = CaravanInventoryUtility.AllInventoryItems(caravan);
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].def == ThingDefOf.Silver)
            {
                caravanSilver += inventoryItems[i].stackCount;
            }
        }

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

        OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.Silver, (int)usedSilver);
        foreach (Thing t in sculptures)
        {
            sculptures.Remove(t);
            CaravanInventoryUtility.GiveThing(caravan, t);
        }

        soldCount += sculptures.Count;

        OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PurchaseOfSculptures".Translate(sculptures.Count, usedSilver.ToString("F0")));

        if (SculpturesCount == 0)
        {
            totalMarkerValue = 0f;
            soldCount = totalCount;
        }
        else
        {
            totalMarkerValue = 0f;
            for (int k = 0; k < SculpturesCount; k++)
            {
                totalMarkerValue += (sculptures[k].MarketValue * 0.7f);
            }
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        float percentage = soldCount / totalCount;
        if (percentage >= 0.99f)
        {

        }
        else if (percentage >= 0.5f)
        {

        }
        else if (percentage > 0f)
        {

        }
        else
        {

        }
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