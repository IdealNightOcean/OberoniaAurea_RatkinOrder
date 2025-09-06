using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_TemporaryEncampment : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated, IThingRequester
{
    private static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");

    private Branch branch;
    public Branch Branch => branch;
    public Squad Squad => branch.Squad;

    private bool hasSupplyRequest;
    private ThingDef requestDef;
    private int requestCount;

    public bool IsRequestActive => hasSupplyRequest && requestDef is not null;

    public override int TicksNeeded => 2500;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");

        Scribe_Values.Look(ref hasSupplyRequest, "hasSupplyRequest", defaultValue: false);
        Scribe_Defs.Look(ref requestDef, "requestDef");
        Scribe_Values.Look(ref requestCount, "requestCount", 0);
    }

    public void InitOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
        }
    }

    public void InitThingRequest(ThingDef requestDef, int requestCount)
    {
        this.requestDef = requestDef;
        this.requestCount = requestCount;
        hasSupplyRequest = true;
    }

    public void DisableRequest()
    {
        hasSupplyRequest = false;
        requestDef = null;
        requestCount = 0;
    }

    public override void PostAdd()
    {
        base.PostAdd();
        if (Rand.Chance(0.5f))
        {
            DisableRequest();
            QuestUtility.SendQuestTargetSignals(questTags, "NoSupplyRequest", this.Named("SUBJECT"));
        }
        else
        {
            TryInitSupplyRequest();
        }
    }

    protected override void InterruptWork() { }

    protected override void FinishWork() { }

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan))
        {
            yield return gizmo;
        }
        if (hasSupplyRequest)
        {
            Command_Action command_FulfillRequest = new()
            {
                defaultLabel = "CommandFulfillTradeOffer".Translate(),
                defaultDesc = "CommandFulfillTradeOfferDesc".Translate(),
                icon = TradeCommandTex,
                action = delegate { FulfillRequest(caravan); }
            };
            yield return command_FulfillRequest;
        }
    }

    public void FulfillRequest(Caravan caravan)
    {
        if (!CaravanInventoryUtility.HasThings(caravan, requestDef, requestCount, t => t.GetRotStage() != RotStage.Fresh))
        {
            Messages.Message("CommandFulfillTradeOfferFailInsufficient".Translate(TradeRequestUtility.RequestedThingLabel(requestDef, requestCount)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CommandFulfillTradeOfferConfirm".Translate(GenLabel.ThingLabel(requestDef, null, requestCount)), delegate
        {
            OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, requestDef, requestCount);
            QuestUtility.SendQuestTargetSignals(questTags, "TradeRequestFulfilled", this.Named("SUBJECT"), caravan.Named("CARAVAN"));
            hasSupplyRequest = false;
        }));
    }

    private void TryInitSupplyRequest()
    {
        ThingDefCountRangeClass requestThing = def.GetModExtension<EncampmentSupplyExtension>()?.requestThingDefs?.RandomElementWithFallback(null);
        if (requestThing is null)
        {
            DisableRequest();
            QuestUtility.SendQuestTargetSignals(questTags, "NoSupplyRequest", this.Named("SUBJECT"));
            return;
        }

        InitThingRequest(requestThing.thingDef, requestThing.countRange.RandomInRange);

        ChoiceLetter_TemporaryEncampment choiceLetter = (ChoiceLetter_TemporaryEncampment)LetterMaker.MakeLetter(label: "OARO_LetterLabel_TemporaryEncampmentRequest".Translate(),
                                                                                                                 text: "OARO_LetterLabel_TemporaryEncampmentRequest".Translate(requestDef.LabelCap, requestCount),
                                                                                                                 def: LetterDefOf.PositiveEvent,
                                                                                                                 lookTargets: this,
                                                                                                                 relatedFaction: branch?.RatkinOrder.Faction,
                                                                                                                 quest: quest);
        choiceLetter.SetWorldObject(this);
        choiceLetter.StartTimeout(15000);
        Find.LetterStack.ReceiveLetter(choiceLetter);
    }

    public void RejectSupplyRequest()
    {
        if (hasSupplyRequest)
        {
            DisableRequest();
            QuestUtility.SendQuestTargetSignals(questTags, "NoSupplyRequest", this.Named("SUBJECT"));
        }
    }
}