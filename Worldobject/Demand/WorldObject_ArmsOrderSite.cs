using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 军备订单交易点（特化类）
/// </summary>
public sealed class WorldObject_ArmsOrderSite : WorldObject_InteractiveBase, IThingRequester
{
    private static readonly Texture2D FulfillIcon = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");

    private ThingDef requestDef;
    private int requestCount = -1;
    private int requestCountLeft = -1;

    public bool IsRequestActive => requestCountLeft > 0 && requestDef is not null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref requestDef, "requestDef");
        Scribe_Values.Look(ref requestCount, "requestCount", -1);
        Scribe_Values.Look(ref requestCountLeft, "requestCountLeft", -1);
    }

    public override void Notify_CaravanArrived(Caravan caravan) { }

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan))
        {
            yield return gizmo;
        }
        if (IsRequestActive)
        {
            Command_Action command_Fulfillment = new Command_Action()
            {
                icon = FulfillIcon,
                action = delegate { FulfillRequest(caravan); }
            };

            if (!CaravanInventoryUtility.HasThings(caravan, requestDef, requestCount))
            {
                command_Fulfillment.Disable();
            }
            yield return command_Fulfillment;
        }
    }

    public void InitThingRequest(ThingDef requestDef, int requestCount)
    {
        this.requestDef = requestDef;
        this.requestCount = requestCount;
        requestCountLeft = requestCount;
    }

    public void DisableRequest()
    {
        requestCount = -1;
        requestCountLeft = -1;
        requestDef = null;
    }

    public void FulfillRequest(Caravan caravan)
    {
        List<Thing> bannerRifles = CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == requestDef).ToList();
        if (bannerRifles.NullOrEmpty())
        {
            return;
        }

        bool hasCompQuality = bannerRifles[0].TryGetComp<CompQuality>() is not null;

        int takeCount;
        List<Thing> takeThings = [];
        Thing takeThing = null;
        bannerRifles.SortBy(t => -GetQualityIndex(t));

        for (int i = 0; i < bannerRifles.Count; i++)
        {
            takeThing = bannerRifles[i];
            takeCount = Mathf.Min(requestCountLeft, takeThing.stackCount);
            takeThings.Add(takeThing.holdingOwner.Take(takeThing, takeCount));
            if (requestCountLeft <= 0)
            {
                break;
            }
        }

        bool perfectFulfill = takeThing is not null && GetQualityIndex(takeThing) >= (int)QualityCategory.Good;

        foreach (Thing t in takeThings)
        {
            t.Destroy();
        }

        if (perfectFulfill)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "PerfectRequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT), caravan.Named("CARAVAN"));
        }
        else
        {
            QuestUtility.SendQuestTargetSignals(questTags, "RequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT), caravan.Named("CARAVAN"));
        }


        int GetQualityIndex(Thing t)
        {
            if (hasCompQuality)
            {
                t.TryGetQuality(out QualityCategory quality);
                return (int)quality;
            }
            else
            {
                return 0;
            }
        }
    }
}
