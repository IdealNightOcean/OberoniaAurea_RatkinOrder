using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Dialog_BranchTrade_SingleUse(Pawn playerNegotiator, ITrader trader, bool giftsOnly = false) : Dialog_BranchTrade(playerNegotiator, trader, giftsOnly)
{
    protected bool ConfirmClose { get; set; }

    public override bool OnCloseRequest()
    {
        if (ConfirmClose)
        {
            return true;
        }
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_UIUtility.ConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_BranchInteraction_SingleUseTradeConfirmColse".Translate(),
            ratkinOrder: Parms.RatkinOrder,
            acceptText: "Confirm".Translate(),
            acceptAction: delegate
            {
                ConfirmClose = true;
                Close();
            },
            rejectText: "Cancel".Translate());
        Find.WindowStack.Add(nodeTree);

        return false;
    }
}

public class Dialog_BranchTrade(Pawn playerNegotiator, ITrader trader, bool giftsOnly = false) : Dialog_Trade(playerNegotiator, trader, giftsOnly)
{
    protected BranchInteractionParms Parms { get; set; }

    public event Action<BranchInteractionParms, bool> PostApplyBranchInteraction;

    public void InitForInteraction(BranchInteractionParms parms)
    {
        Parms = parms;
    }

    public override void PostClose()
    {
        try
        {
            PostApplyBranchInteraction?.Invoke(Parms, true);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"call-back: {nameof(PostApplyBranchInteraction)}",
                typeName: nameof(Dialog_BranchTrade),
                methodName: nameof(PostClose),
                needStackTrace: true);
        }
        finally
        {
            PostApplyBranchInteraction = null;
        }

        base.PostClose();
    }
}