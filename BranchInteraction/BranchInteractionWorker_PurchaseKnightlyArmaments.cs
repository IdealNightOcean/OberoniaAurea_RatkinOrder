using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_PurchaseKnightlyArmaments(BranchInteractionDef def) : BranchInteractionWorker(def)
{
    protected override void ApplyInteraction(BranchInteractionParms parms)
    {
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(StuffNode(parms), parms.RatkinOrder);
        Find.WindowStack.Add(nodeTree);
    }

    private DiaNode StuffNode(BranchInteractionParms parms)
    {
        DiaNode rootNode = new("OARO_PurchaseKnightlyArmamentsRoot_Stuff".Translate());

        DiaOption steelOpt = new(ThingDefOf.Steel.label)
        {
            linkLateBind = () => QualityNode(parms, ThingDefOf.Steel),
            resolveTree = false
        };
        rootNode.options.Add(steelOpt);

        DiaOption wolf = new(ThingDefOf.WoodLog.label)
        {
            linkLateBind = () => QualityNode(parms, ThingDefOf.WoodLog),
            resolveTree = false
        };
        rootNode.options.Add(wolf);

        rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);

        return rootNode;
    }

    private DiaNode QualityNode(BranchInteractionParms parms, ThingDef stuff)
    {
        int caravanSilver = parms.Caravan.GetCountOfThingDef(ThingDefOf.Silver);
        DiaNode rootNode = new("OARO_PurchaseKnightlyArmamentsRoot_Quality".Translate());

        DiaOption goodOption = new(QualityCategory.Good.GetLabel() + (4000))
        {
            action = () => GiveKnightlyArmaments(parms, QualityCategory.Good, stuff, 4000),
            resolveTree = true
        };
        rootNode.options.Add(goodOption);
        if (caravanSilver < 4000)
        {
            goodOption.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, 4000.ToString()));
        }

        DiaOption excellentOption = new(QualityCategory.Excellent.GetLabel() + (6000))
        {
            action = () => GiveKnightlyArmaments(parms, QualityCategory.Excellent, stuff, 6000),
            resolveTree = true
        };
        rootNode.options.Add(excellentOption);
        if (caravanSilver < 6000)
        {
            goodOption.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, 6000.ToString()));
        }

        DiaOption masterworkOption = new(QualityCategory.Masterwork.GetLabel() + (8000))
        {
            action = () => GiveKnightlyArmaments(parms, QualityCategory.Excellent, stuff, 8000),
            resolveTree = true
        };
        rootNode.options.Add(masterworkOption);
        if (caravanSilver < 8000)
        {
            goodOption.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, 8000.ToString()));
        }

        DiaOption backOpt = new("GoBack".Translate())
        {
            linkLateBind = () => StuffNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(backOpt);

        return rootNode;
    }

    private void GiveKnightlyArmaments(BranchInteractionParms parms, QualityCategory quality, ThingDef stuffDef, int price)
    {
        parms.Caravan.RemoveThingsOfDef(ThingDefOf.Silver, price);
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_PurchaseKnightlyArmamentsRoot_Purchased".Translate(quality.GetLabel().Named(KeyLibrary_FormatArgName.Quality), stuffDef.label.Named(KeyLibrary_FormatArgName.STUFF), price.ToString().Named("Price")),
            ratkinOrder: parms.Branch.RatkinOrder);
        Find.WindowStack.Add(nodeTree);

        base.ApplyInteraction(parms);
    }

    protected override void DoInteractionCost(BranchInteractionParms parms)
    {
        base.DoInteractionCost(parms);
        if (!parms.Branch.EffectTags.HasTag("PurchaseKnightlyArmamentsNoCD"))
        {
            parms.Branch.CooldownManager.RegisterRecord(Def.defName, cdTicks: Def.defaultCdDays * 60000, removeWhenExpired: true);
        }
    }
}