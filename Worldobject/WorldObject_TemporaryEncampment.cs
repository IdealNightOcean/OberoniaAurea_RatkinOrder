using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_TemporaryEncampment : WorldObject_SquadAssociatedBase
{
    private static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");
    private static readonly ThingDef[] RequestThingDefsArr =
        [
            ThingDefOf.Cloth,
            ThingDefOf.Pemmican,
            ThingDefOf.MealSimple,
            ThingDefOf.Kibble,


        ];

    public bool hasSupplyRequest;
    public ThingDef requestThingDef;
    public int requestCount;

    public override int TicksNeeded => 2500;

    public override void PostAdd()
    {
        base.PostAdd();
        if (Rand.Chance(0.5f))
        {
            hasSupplyRequest = false;
            QuestUtility.SendQuestTargetSignals(questTags, "NoSupplyRequest", this.Named("SUBJECT"));
        }
        else
        {
            hasSupplyRequest = true;
            InitSupplyRequest();
        }
    }

    protected override void InterruptWork() { }

    protected override void FinishWork()
    {

    }

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

    private void FulfillRequest(Caravan caravan)
    {
        if (!CaravanInventoryUtility.HasThings(caravan, requestThingDef, requestCount, (Thing t) => t.GetRotStage() != RotStage.Fresh))
        {
            Messages.Message("CommandFulfillTradeOfferFailInsufficient".Translate(TradeRequestUtility.RequestedThingLabel(requestThingDef, requestCount)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CommandFulfillTradeOfferConfirm".Translate(GenLabel.ThingLabel(requestThingDef, null, requestCount)), delegate
        {
            OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, requestThingDef, requestCount);
            QuestUtility.SendQuestTargetSignals(questTags, "TradeRequestFulfilled", this.Named("SUBJECT"), caravan.Named("CARAVAN"));
            hasSupplyRequest = false;
        }));
    }

    private void InitSupplyRequest()
    {
        requestThingDef = RequestThingDefsArr[Rand.Range(0, RequestThingDefsArr.Length)];
        if (requestThingDef == ThingDefOf.MealSimple)
        {
            requestCount = squad.SquadStat.AllCrewCountInt * 2;
        }
        else
        {
            requestCount = Rand.RangeInclusive(400, 600);
        }

        ChoiceLetter_TemporaryEncampment choiceLetter = (ChoiceLetter_TemporaryEncampment)LetterMaker.MakeLetter(label: "OARO_LetterLabel_TemporaryEncampmentRequest".Translate(),
                                                                                                                 text: "OARO_LetterLabel_TemporaryEncampmentRequest".Translate(requestThingDef.LabelCap, requestCount),
                                                                                                                 def: LetterDefOf.PositiveEvent,
                                                                                                                 lookTargets: this,
                                                                                                                 relatedFaction: RatkinOrder.Faction,
                                                                                                                 quest: quest);
        choiceLetter.SetWorldObject(this);
        choiceLetter.StartTimeout(15000);
        Find.LetterStack.ReceiveLetter(choiceLetter);
    }

    public void RejectSupplyRequest()
    {
        if (hasSupplyRequest)
        {
            hasSupplyRequest = false;
            requestThingDef = null;
            requestCount = 0;
            QuestUtility.SendQuestTargetSignals(questTags, "NoSupplyRequest", this.Named("SUBJECT"));
        }
    }
}