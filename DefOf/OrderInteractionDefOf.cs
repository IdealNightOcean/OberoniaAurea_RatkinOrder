using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderInteractionDefOf
{
    public static OrderInteractionDef OARO_EnhanceRelationship;
    public static OrderInteractionDef OARO_SponsorOrder;
    public static OrderInteractionDef OARO_InviteBranchCreation;

    public static OrderInteractionDef OARO_ExchangeSupply;

    static OrderInteractionDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderInteractionDefOf));
    }
}
