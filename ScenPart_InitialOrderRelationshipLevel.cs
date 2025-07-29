using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ScenPart_InitialOrderRelationshipLevel : ScenPart
{
    public const string OrderScenTag = "OARO_Order";
    public const string OrderScenPartTag = "OARO_Order_ScenPart";

    private OrderRelationshipKind initRelation = OrderRelationshipKind.Stranger;
    public OrderRelationshipKind InitRelation => initRelation;

    public override void PostGameStart()
    {
        GameComponent_RatkinOrder.Instance.InitOrderRelationship = initRelation;
    }

    public override string Summary(Scenario scen)
    {
        return ScenSummaryList.SummaryWithList(scen, OrderScenPartTag, OrderScenPartTag.Translate()) + "\n";
    }
    public override IEnumerable<string> GetSummaryListEntries(string tag)
    {
        if (tag == OrderScenTag)
        {
            yield return "OARO_InitialOrderRelationshipLevel".Translate(EsteemUtility.GetRelationshipKindLabel(initRelation));
        }
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight);
        string label = EsteemUtility.GetRelationshipKindLabel(initRelation);
        if (!Widgets.ButtonText(scenPartRect, label))
        {
            return;
        }
        List<FloatMenuOption> list = [];
        for (int i = 0; i < EsteemUtility.RelationshipKindArr.Length; i++)
        {
            OrderRelationshipKind selRelationship = EsteemUtility.RelationshipKindArr[i];
            string selLabel = EsteemUtility.GetRelationshipKindLabel(selRelationship);
            list.Add(new FloatMenuOption(selLabel, delegate
            {
                initRelation = selRelationship;
            }));
        }
        Find.WindowStack.Add(new FloatMenu(list));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initRelation, "initRelation", OrderRelationshipKind.Stranger);
    }
}