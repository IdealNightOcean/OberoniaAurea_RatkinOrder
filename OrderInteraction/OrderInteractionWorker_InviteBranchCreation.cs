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

    public override void TryApplyInteraction(RatkinOrder ratkinOrder, Map map)
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
            canSelectTarget: CanSelectTarget,
            originForClosest: map.Tile,
            showCancelButton: true);


        bool SelectAction(GlobalTargetInfo t)
        {
            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                text: "OARO_InviteBranchCreationConfirm".Translate(ratkinOrder.Name.Named("ORDERNAME")),
                ratkinOrder: ratkinOrder,
                acceptAction: delegate
                {
                    if (!BranchUtility.IsValidTileForInviteBranchCreation(ratkinOrder, map, t.Tile, resultOnly: true))
                    {
                        return;
                    }
                    if (BranchUtility.GenerateBranchOnTile(ratkinOrder, map, t.Tile) is null)
                    {
                        return;
                    }
                    base.TryApplyInteraction(ratkinOrder, map);
                });

            Find.WindowStack.Add(nodeTree);
            return true;
        }

        bool CanSelectTarget(GlobalTargetInfo t) => BranchUtility.IsValidTileForInviteBranchCreation(ratkinOrder, map, t.Tile, resultOnly: true);
    }

    protected override void DoInteractionCost(RatkinOrder ratkinOrder, Map map)
    {
        base.DoInteractionCost(ratkinOrder, map);
        map.DestoryThingsOfDef(ThingDefOf.Silver, ratkinOrder.BranchManager.SilverNeededForNextBranchCreation);
    }

    protected override void InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        ratkinOrder.BranchManager.Notify_NewBranchInviteCreated();
    }
}