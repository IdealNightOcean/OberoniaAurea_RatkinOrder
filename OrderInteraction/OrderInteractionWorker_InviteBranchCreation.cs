using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
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
            AcceptanceReport acceptanceReport = BranchUtility.IsValidTileForInviteBranchCreation(ratkinOrder, map, t.Tile, resultOnly: false);
            if (!acceptanceReport)
            {
                Messages.Message("OARO_CannotSelTileAsBranchSite".Translate(acceptanceReport.Reason), MessageTypeDefOf.RejectInput, historical: false);
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
}