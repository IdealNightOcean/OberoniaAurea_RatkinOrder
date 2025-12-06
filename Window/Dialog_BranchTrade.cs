using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Dialog_BranchTrade(Pawn playerNegotiator, ITrader trader, bool giftsOnly = false) : Dialog_Trade(playerNegotiator, trader, giftsOnly)
{
    protected BranchInteractionParms Parms { get; set; }

    public Action<BranchInteractionParms, bool> PostApplyBranchInteraction { get; set; }

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