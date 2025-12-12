using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_InviteBranchCreation(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    public static readonly CachedTexture TargeterMouseAttachment = new("UI/Overlays/LaunchableMouseAttachment");

    public override AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        AcceptanceReport baseAcceptance = base.CanUseInteraction(ratkinOrder, map, resultOnly);
        if (!baseAcceptance)
        {
            return baseAcceptance;
        }

        int silverNeeded = ratkinOrder.BranchManager.SilverNeededForNextBranchCreation;
        if (!map.HasEnoughThingsOfDef(ThingDefOf.Silver, silverNeeded))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, silverNeeded.ToString());
        }

        return true;
    }

    protected override void ApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(map.Tile)));
        Find.WorldSelector.ClearSelection();

        Find.WorldTargeter.BeginTargeting(
            action: SelectAction,
            canTargetTiles: true,
            mouseAttachment: TargeterMouseAttachment.Texture,
            closeWorldTabWhenFinished: true,
            onUpdate: null,
            extraLabelGetter: null,
            canSelectTarget: (t) => t.Tile.LayerDef == PlanetLayerDefOf.Surface,
            originForClosest: map.Tile,
            showCancelButton: true);


        bool SelectAction(GlobalTargetInfo t)
        {
            AcceptanceReport acceptanceReport = IsValidTileForInviteBranchCreation(ratkinOrder, map, t.Tile, resultOnly: false);
            if (!acceptanceReport)
            {
                Messages.Message("OARO_CannotSelTileAsBranchSite".Translate(acceptanceReport.Reason.Named(KeyLibrary_FormatArgName.Reason)), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                text: "OARO_InviteBranchCreationConfirm".Translate(ratkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)),
                ratkinOrder: ratkinOrder,
                acceptAction: delegate
                {
                    if (BranchUtility.GenerateBranchOnTile(ratkinOrder, t.Tile))
                    {
                        base.ApplyInteraction(ratkinOrder, map);
                    }
                });

            Find.WindowStack.Add(nodeTree);
            return true;
        }
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        ratkinOrder.BranchManager.Notify_NewBranchInviteCreated();
        return (true, true);
    }

    protected override void DoInteractionCost(RatkinOrder ratkinOrder, Map map)
    {
        base.DoInteractionCost(ratkinOrder, map);
        map.DestoryThingsOfDef(ThingDefOf.Silver, ratkinOrder.BranchManager.SilverNeededForNextBranchCreation);
    }

    private static AcceptanceReport IsValidTileForInviteBranchCreation(RatkinOrder ratkinOrder, Map map, PlanetTile tile, bool resultOnly)
    {
        if (map is null || !ratkinOrder.IsValid() || !tile.Valid)
        {
            return false;
        }

        if (tile.LayerDef != PlanetLayerDefOf.Surface)
        {
            return resultOnly ? false : "OARO_SurfaceOnly".Translate();
        }

        List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;

        WorldObject curWO = allWorldObjects.Where(w => w.Tile == tile).FirstOrFallback(fallback: null);
        if (curWO is null)
        {
            if (allWorldObjects.Any(w => w.Tile.Layer == tile.Layer && Find.WorldGrid.ApproxDistanceInTiles(w.Tile, tile) <= 3f))
            {
                return resultOnly ? false : "OARO_TooCloseToOtherWorldObjects".Translate(3.ToString());
            }
            return true;
        }

        return curWO.CanBeSiteForNewBranch(ratkinOrder);
    }
}