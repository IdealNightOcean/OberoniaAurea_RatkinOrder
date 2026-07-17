using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_PurchaseKnightlyArmaments(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    private ThingDef MetallicStuff { get; set; }
    private ThingDef LeatheryFabricStuff { get; set; }
    private QualityCategory Quality { get; set; }

    protected override void ApplyEffect(BranchInteractionParms parms)
    {
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(MetallicStuffNode(parms), parms.RatkinOrder);
        Find.WindowStack.Add(nodeTree);
    }

    private DiaNode MetallicStuffNode(BranchInteractionParms parms)
    {
        DiaNode rootNode = new("OARO_PurchaseKnightlyArmamentsRoot_MetallicStuff".Translate());

        DiaOption woodLogOpt = new(ThingDefOf.WoodLog.label)
        {
            action = () => MetallicStuff = ThingDefOf.WoodLog,
            linkLateBind = () => LeatheryFabricStuffNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(woodLogOpt);

        DiaOption clothOpt = new(ThingDefOf.Cloth.label)
        {
            action = () => MetallicStuff = ThingDefOf.Cloth,
            linkLateBind = () => LeatheryFabricStuffNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(clothOpt);

        IEnumerable<ThingDef> metallicStuffs = GenStuff.AllowedStuffs([StuffCategoryDefOf.Metallic]);
        foreach (ThingDef stuff in metallicStuffs)
        {
            DiaOption stuffOpt = new(stuff.label)
            {
                action = () => MetallicStuff = stuff,
                linkLateBind = () => LeatheryFabricStuffNode(parms),
                resolveTree = false
            };
            rootNode.options.Add(stuffOpt);
        }

        rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);

        return rootNode;
    }

    private DiaNode LeatheryFabricStuffNode(BranchInteractionParms parms)
    {
        DiaNode rootNode = new("OARO_PurchaseKnightlyArmamentsRoot_LeatheryFabricStuff".Translate());

        DiaOption woodLogOpt = new(ThingDefOf.WoodLog.label)
        {
            action = () => LeatheryFabricStuff = ThingDefOf.WoodLog,
            linkLateBind = () => QualityNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(woodLogOpt);

        DiaOption clothOpt = new(ThingDefOf.Steel.label)
        {
            action = () => LeatheryFabricStuff = ThingDefOf.Steel,
            linkLateBind = () => QualityNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(clothOpt);

        IEnumerable<ThingDef> metallicStuffs = GenStuff.AllowedStuffs([StuffCategoryDefOf.Leathery, StuffCategoryDefOf.Fabric]);
        foreach (ThingDef stuff in metallicStuffs)
        {
            DiaOption stuffOpt = new(stuff.label)
            {
                action = () => LeatheryFabricStuff = stuff,
                linkLateBind = () => QualityNode(parms),
                resolveTree = false
            };
            rootNode.options.Add(stuffOpt);
        }
        DiaOption goBackOpt = new("GoBack".Translate())
        {
            linkLateBind = () => MetallicStuffNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(goBackOpt);
        rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);

        return rootNode;
    }

    private DiaNode QualityNode(BranchInteractionParms parms)
    {
        DiaNode rootNode = new("OARO_PurchaseKnightlyArmamentsRoot_Quality".Translate());

        DiaOption goodOption = new(QualityCategory.Good.GetLabel())
        {
            action = () => Quality = QualityCategory.Good,
            linkLateBind = () => ConfirmNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(goodOption);

        DiaOption excellentOption = new(QualityCategory.Excellent.GetLabel())
        {
            action = () => Quality = QualityCategory.Excellent,
            linkLateBind = () => ConfirmNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(excellentOption);

        DiaOption masterworkOption = new(QualityCategory.Masterwork.GetLabel())
        {
            action = () => Quality = QualityCategory.Masterwork,
            linkLateBind = () => ConfirmNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(masterworkOption);

        DiaOption backOpt = new("GoBack".Translate())
        {
            linkLateBind = () => LeatheryFabricStuffNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(backOpt);

        return rootNode;
    }

    private DiaNode ConfirmNode(BranchInteractionParms parms)
    {
        int caravanSilver = parms.TargetCaravan.GetCountOfThingDef(ThingDefOf.Silver);
        int price = GetArmamentsPrice();
        DiaNode rootNode = new("OARO_PurchaseKnightlyArmamentsRoot_Confirm".Translate(
            MetallicStuff.Named("MetallicStuff"),
            LeatheryFabricStuff.Named("LeatheryFabricStuff"),
            Quality.GetLabel().Named(KeyLibrary_FormatArgName.Quality),
            price.Named(KeyLibrary_FormatArgName.Count)));

        if (price > 0)
        {
            DiaOption confirmOpt = new("Confirm".Translate())
            {
                action = () => GiveKnightlyArmaments(parms, price),
                resolveTree = true
            };
            rootNode.options.Add(confirmOpt);
            if (caravanSilver < price)
            {
                confirmOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, price.ToString()));
            }
        }

        rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);

        DiaOption backOpt = new("GoBack".Translate())
        {
            linkLateBind = () => QualityNode(parms),
            resolveTree = false
        };
        rootNode.options.Add(backOpt);

        return rootNode;
    }

    private int GetArmamentsPrice()
    {
        List<ThingDef> armaments = Def.GetModExtension<ThingList_Extension>()?.thingList ?? [];
        if (armaments.NullOrEmpty())
        {
            Log.Error($"[OARO] 在 {nameof(BranchInteractionWorker_PurchaseKnightlyArmaments)}.{nameof(GetArmamentsPrice)} 中武器列表为null或为空集合");
            return -1;
        }

        float totalPrice = 0f;

        foreach (ThingDef def in armaments)
        {
            if (!def.MadeFromStuff)
            {
                totalPrice += StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, stuffDef: null, quality: Quality));
            }
            else
            {
                if (def.stuffCategories.Contains(StuffCategoryDefOf.Metallic))
                {
                    totalPrice += StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, stuffDef: MetallicStuff, quality: Quality));
                }
                else
                {
                    totalPrice += StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(def, stuffDef: LeatheryFabricStuff, quality: Quality));
                }
            }
        }

        return (int)(totalPrice * 0.7f);
    }

    private void GiveKnightlyArmaments(BranchInteractionParms parms, int price)
    {
        List<ThingDef> armaments = Def.GetModExtension<ThingList_Extension>()?.thingList ?? [];
        if (armaments.NullOrEmpty())
        {
            Log.Error($"[OARO] 在 {nameof(BranchInteractionWorker_PurchaseKnightlyArmaments)}.{nameof(GiveKnightlyArmaments)} 中武器列表为null或为空集合");
            return;
        }

        foreach (ThingDef def in armaments)
        {
            Thing armament;
            if (!def.MadeFromStuff)
            {
                armament = ThingMaker.MakeThing(def);
            }
            else
            {
                if (def.stuffCategories.Contains(StuffCategoryDefOf.Metallic))
                {
                    armament = ThingMaker.MakeThing(def, MetallicStuff);
                }
                else
                {
                    armament = ThingMaker.MakeThing(def, LeatheryFabricStuff);
                }
            }
            if (armament is not null)
            {
                armament.TryGetComp<CompQuality>()?.SetQuality(Quality, ArtGenerationContext.Outsider);
                CaravanInventoryUtility.GiveThing(parms.TargetCaravan, armament);
            }
        }

        parms.TargetCaravan.RemoveThingsOfDef(ThingDefOf.Silver, price);
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_PurchaseKnightlyArmamentsRoot_Purchased".Translate(
                MetallicStuff.Named("MetallicStuff"),
                LeatheryFabricStuff.Named("LeatheryFabricStuff"),
                Quality.GetLabel().Named(KeyLibrary_FormatArgName.Quality),
                price.Named(KeyLibrary_FormatArgName.Count)),
            ratkinOrder: parms.Branch.RatkinOrder);
        Find.WindowStack.Add(nodeTree);

        base.ApplyEffect(parms);
    }


    protected override void ApplyCost(BranchInteractionParms parms)
    {
        base.ApplyCost(parms);
        if (!parms.Branch.EffectTags.HasTag(KeyLibrary_EffectTag.PurchaseKnightlyArmamentsNoCD))
        {
            parms.Branch.CooldownManager.RegisterRecord(Def.defName, cdTicks: Def.defaultCdDays * 60000, removeWhenExpired: true);
        }
    }
}