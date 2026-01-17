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
    private const float MarketValueFactor = 0.7f;

    private ThingOwner<Thing> sculptures;
    private float remainingMarkerValue;
    private int purchasedCount;
    private int totalCount;

    private bool hasSettled;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref sculptures, nameof(sculptures));
        Scribe_Values.Look(ref remainingMarkerValue, nameof(remainingMarkerValue), 0f);
        Scribe_Values.Look(ref purchasedCount, nameof(purchasedCount), 0);
        Scribe_Values.Look(ref totalCount, nameof(totalCount), 0);
    }

    public override void PostMake()
    {
        base.PostMake();
        remainingMarkerValue = 0f;
        sculptures = new ThingOwner<Thing>(this);

        int sculpturesCount = Rand.RangeInclusive(15, 30);
        ThingDef sculptureDef = DefDatabase<ThingDef>.GetNamed("SculptureSmall");
        for (int i = 0; i < sculpturesCount; i++)
        {
            Thing sculpture = ThingMaker.MakeThing(sculptureDef, ThingDefOf.WoodLog);
            sculpture.TryGetComp<CompQuality>()?.SetQuality(Rand.Bool ? QualityCategory.Good : QualityCategory.Excellent, ArtGenerationContext.Outsider);

            remainingMarkerValue += (sculpture.MarketValue * MarketValueFactor);

            Thing sculptureMini = sculpture.TryMakeMinified();
            sculptures.TryAdd(sculptureMini);
        }

        totalCount = sculptures.Count;
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_UnsalableArtcraft_Remaining".Translate(sculptures.Count, remainingMarkerValue.ToString("F0")));
        sb.AppendInNewLine("OARO_UnsalableArtcraft_Purchased".Translate(purchasedCount));
        return sb.ToString();
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        OpenPurchaseNode(caravan);
    }

    private void OpenPurchaseNode(Caravan caravan)
    {
        int caravanSilver = caravan.GetCountOfThingDef(ThingDefOf.Silver);
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
        if (remainingMarkerValue > caravanSilver)
        {
            totalOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.LabelCap, remainingMarkerValue));
        }
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
            if (silverNeed > caravanSilver)
            {
                subOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.LabelCap, silverNeed));
            }
            rootNode.options.Add(subOpt);
        }
    }

    private void PurchaseOfSculptures(Caravan caravan, int silverCountNeed = -1)
    {
        silverCountNeed = silverCountNeed > 0 ? silverCountNeed : Mathf.FloorToInt(remainingMarkerValue);

        List<Thing> inventoryItems = CaravanInventoryUtility.AllInventoryItems(caravan);

        List<Thing> takeSculptures = [];
        float usedSilver = 0f;
        for (int j = 0; j < sculptures.Count; j++)
        {
            float marketValue = sculptures[j].MarketValue * MarketValueFactor;
            if (usedSilver + marketValue > silverCountNeed)
            {
                continue;
            }
            usedSilver += marketValue;
            takeSculptures.Add(sculptures[j]);
        }

        purchasedCount += takeSculptures.Count;
        foreach (Thing t in takeSculptures)
        {
            sculptures.Remove(t);
            CaravanInventoryUtility.GiveThing(caravan, t);
        }
        OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.Silver, (int)usedSilver);
        OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_UnsalableArtcraft_PurchaseResult".Translate(
            takeSculptures.Count.Named(KeyLibrary_FormatArgName.Count),
            usedSilver.ToString("F0").Named("Price")));

        if (sculptures.Count == 0)
        {
            remainingMarkerValue = 0f;
            purchasedCount = totalCount;
            hasSettled = true;
            QuestUtility.SendQuestTargetSignals(questTags, "PerfectRequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT));
        }
        else
        {
            remainingMarkerValue = 0f;
            for (int k = 0; k < sculptures.Count; k++)
            {
                remainingMarkerValue += (sculptures[k].MarketValue * MarketValueFactor);
            }
        }
    }

    public override void Destroy()
    {
        if (!hasSettled)
        {
            hasSettled = true;
            float percentage = purchasedCount / (float)totalCount;
            if (percentage >= 0.99f)
            {
                QuestUtility.SendQuestTargetSignals(questTags, "PerfectRequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT));
            }
            else if (percentage >= 0.5f)
            {
                QuestUtility.SendQuestTargetSignals(questTags, "RequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT));
            }
            else if (percentage > 0f)
            {
                QuestUtility.SendQuestTargetSignals(questTags, "BarelyRequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT));
            }
        }

        base.Destroy();
    }


    public ThingOwner GetDirectlyHeldThings() => sculptures;
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }
}