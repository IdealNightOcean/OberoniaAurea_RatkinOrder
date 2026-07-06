using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_MiningExploration(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    private const int RequiredSilverAmount = 4000;

    protected override AcceptanceReport TargetValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (OARO_ModDefOf.Rakinia_RockRatkin is null || OAFrame_FactionUtility.FirstAvailableFactionOfDef(OARO_ModDefOf.Rakinia_RockRatkin, FactionValidationParams.NonHostileNormalFaction) is null)
        {
            return resultOnly ? false : "OARO_NoNonHostileRockRatkin".Translate();
        }
        return base.TargetValidate(parms, resultOnly);
    }

    protected override void ApplyEffect(BranchInteractionParms parms)
    {
        int caravanSilver = parms.TargetCaravan.GetCountOfThingDef(ThingDefOf.Silver);
        DiaNode rootNode = new("OARO_MiningExploration_Root".Translate());
        if (caravanSilver < RequiredSilverAmount)
        {
            DiaOption lackOpt = new("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, RequiredSilverAmount.ToString()))
            {
                resolveTree = true
            };
            rootNode.options.Add(lackOpt);
        }
        else
        {
            IEnumerable<ThingDef> metallicDefs = DefDatabase<ThingDef>.AllDefsListForReading.Where(IsMetallic);
            rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);
            foreach (ThingDef metallicDef in metallicDefs)
            {
                DiaOption metallicOpt = new(metallicDef.label)
                {
                    action = () => MetallicDelivery(parms, metallicDef),
                    linkLateBind = () => OAFrame_DiaUtility.ConfirmDiaNode(
                        text: "OARO_MiningExploration_Reply".Translate(parms.Branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                                                                       metallicDef.Named(KeyLibrary_FormatArgName.THING)),
                        acceptText: "Confirm".Translate()),
                    resolveTree = false
                };
                rootNode.options.Add(metallicOpt);
            }
            rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);
        }

        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(rootNode, parms.RatkinOrder);
        Find.WindowStack.Add(nodeTree);
    }

    private void MetallicDelivery(BranchInteractionParms parms, ThingDef metallicDef)
    {
        int stackCount = Mathf.CeilToInt(RequiredSilverAmount / metallicDef.GetStatValueAbstract(StatDefOf.MarketValue));
        Thing thing = ThingMaker.MakeThing(metallicDef);
        thing.stackCount = stackCount;

        Branch branch = parms.Branch;
        OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
            label: "OARO_MiningExploration_Label".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
            text: "OARO_MiningExploration_Text".Translate(
                Faction.OfPlayer.Named("playerFaction"),
                branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                metallicDef.Named(KeyLibrary_FormatArgName.THING)),
            def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);

        orderLetter.AddAttachment(thing);
        OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays: Rand.Range(8, 12));

        base.ApplyEffect(parms);
    }

    private static bool IsMetallic(ThingDef t)
    {
        if (t.category != ThingCategory.Item)
        {
            return false;
        }
        if (t.thingCategories is null || !t.thingCategories.Contains(ThingCategoryDefOf.ResourcesRaw))
        {
            return false;
        }
        return t.stuffProps?.categories?.Contains(StuffCategoryDefOf.Metallic) ?? false;
    }

}