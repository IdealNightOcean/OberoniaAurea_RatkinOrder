using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_CustomizedArmaments(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    private DiaNode WeaponNode(BranchInteractionParms parms)
    {
        List<ThingDef> customizableThings = Def.GetModExtension<ThingList_Extension>()?.thingList ?? [];

        DiaNode rootNode = new("OARO_CustomizedArmaments_Weapon".Translate(parms.Branch.Named(OARO_KeyLibrary_FormatArgName.BranchName)));

        foreach (ThingDef tDef in customizableThings)
        {
            DiaOption tDefOpt = new(tDef.label)
            {
                resolveTree = false
            };

            if (tDef.MadeFromStuff)
            {
                tDefOpt.linkLateBind = () => StuffNode(parms, tDef);
            }
            else if (tDef.HasComp<CompQuality>())
            {
                tDefOpt.linkLateBind = () => QualityNode(parms, tDef, ThingDefOf.Steel);
            }
            else
            {
                tDefOpt.linkLateBind = () => ConfirmNode(parms, tDef, ThingDefOf.Steel, QualityCategory.Normal);
            }
            rootNode.options.Add(tDefOpt);
        }

        rootNode.options.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultCancelOption);
        return rootNode;
    }

    private DiaNode StuffNode(BranchInteractionParms parms, ThingDef thingDef)
    {
        DiaNode rootNode = new("OARO_CustomizedArmaments_Stuff".Translate());
        foreach (ThingDef stuffDef in GenStuff.AllowedStuffsFor(thingDef))
        {
            DiaOption sDefOpt = new(stuffDef.label)
            {
                linkLateBind = () => thingDef.HasComp<CompQuality>() ? QualityNode(parms, thingDef, stuffDef)
                                                                     : ConfirmNode(parms, thingDef, ThingDefOf.Steel, QualityCategory.Normal),
                resolveTree = false
            };
            rootNode.options.Add(sDefOpt);
        }
        DiaOption backOpt = new("GoBack".Translate())
        {
            linkLateBind = () => WeaponNode(parms)
        };
        rootNode.options.Add(backOpt);
        rootNode.options.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultCancelOption);
        return rootNode;
    }

    private DiaNode QualityNode(BranchInteractionParms parms, ThingDef thingDef, ThingDef stuffDef)
    {
        int caravanSilver = parms.TargetCaravan.GetCountOfThingDef(ThingDefOf.Silver);
        DiaNode rootNode = new("OARO_CustomizedArmaments_Quality".Translate());

        foreach (QualityCategory quality in QualityUtility.AllQualityCategories)
        {
            DiaOption qualityOpt = new(quality.GetLabel())
            {
                linkLateBind = () => ConfirmNode(parms, thingDef, stuffDef, quality),
                resolveTree = false
            };
            int needSilver = GetPrice(thingDef, stuffDef, quality);
            if (caravanSilver < needSilver)
            {
                qualityOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, needSilver.ToString()));
            }
            rootNode.options.Add(qualityOpt);
        }
        DiaOption backOpt = new("GoBack".Translate())
        {
            linkLateBind = () => StuffNode(parms, thingDef)
        };
        rootNode.options.Add(backOpt);
        rootNode.options.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultCancelOption);

        return rootNode;
    }

    private DiaNode ConfirmNode(BranchInteractionParms parms, ThingDef thingDef, ThingDef stuffDef, QualityCategory quality)
    {
        int needSilver = GetPrice(thingDef, stuffDef, quality);
        int coolingDays = GetDelayDays(quality);
        return OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.ConfirmDiaNode(
            text: "OARO_CustomizedArmaments_Confirm".Translate(
                thingDef.Named(KeyLibrary_FormatArgName.THING),
                stuffDef.Named(KeyLibrary_FormatArgName.STUFF),
                quality.GetLabel().Named(KeyLibrary_FormatArgName.Quality),
                needSilver.ToString().Named("Price"),
                coolingDays.Named("DelayDays")),
            acceptText: "Confirm".Translate(),
            acceptAction: () => Customization(parms, thingDef, stuffDef, quality),
            rejectText: "Cancel".Translate(),
            rejectAction: null);
    }

    private void Customization(BranchInteractionParms parms, ThingDef thingDef, ThingDef stuffDef, QualityCategory quality)
    {
        int coolingDays = GetDelayDays(quality);

        Thing thing = ThingMaker.MakeThing(thingDef, stuffDef);
        if (thingDef.MadeFromStuff)
        {
            thing = ThingMaker.MakeThing(thingDef, stuffDef);
        }
        else
        {
            thing = ThingMaker.MakeThing(thingDef);
        }

        thing.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Outsider);

        Branch branch = parms.Branch;

        OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
            label: "OARO_CustomizedArmaments_CompletedLabel".Translate(branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
            text: "OARO_CustomizedArmaments_CompletedText".Translate(
                branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                GenLabel.ThingsLabel([thing]).Named(KeyLibrary_FormatArgName.ThingsInfo)),
            def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);

        orderLetter.AddAttachment(thing);
        OrderLetterBox.Instance.ReceiveLetter(orderLetter, coolingDays);

        parms.TargetCaravan.RemoveThingsOfDef(ThingDefOf.Silver, GetPrice(thingDef, stuffDef, quality));
        branch.CooldownManager.RegisterRecord(Def.defName, cdTicks: coolingDays * 60000);

        PostApplyEffect(parms, succeeded: true);

    }

    /// <returns>
    /// <para>- doPostApply：始终返回 <see langword="false"/> 以阻止 <see cref="BranchInteractionWorker.ApplyEffect"/> 执行回调方法 <see cref="BranchInteractionWorker.PostApplyEffect"/></para>
    /// </returns>
    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(WeaponNode(parms), parms.RatkinOrder);
        Find.WindowStack.Add(nodeTree);

        return (true, false);
    }

    private static int GetPrice(ThingDef thingDef, ThingDef stuffDef, QualityCategory quality)
    {
        StatRequest statReq = StatRequest.For(thingDef, stuffDef: stuffDef, quality: quality);
        return Mathf.CeilToInt(StatDefOf.MarketValue.Worker.GetValue(statReq));
    }
    private static int GetDelayDays(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful or QualityCategory.Poor or QualityCategory.Normal => 5,
            QualityCategory.Good => 7,
            QualityCategory.Excellent => 10,
            QualityCategory.Masterwork => 15,
            QualityCategory.Legendary => 20,
            _ => 7
        };
    }
}