using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetter_InDistressKnight : OrderLetter_SimpleAttachments
{
    public override void PostReaded(Building_OrderLetterBox letterBox = null)
    {
        base.PostReaded(letterBox);

        RelatedOrder?.EsteemHandler.AdjustEsteem(5, byPlayer: true, reason: "OARO_HelpInDistressKnight".Translate());

        if (RelatedBranch is not null)
        {
            RelatedBranch.SetFriendly(active: true);
            RelatedBranch.CommanderVisitable = true;
            RelatedBranch.HasSupportAuthority = true;
        }
    }
}