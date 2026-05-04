using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingCompProperties_Memorial : BranchBuildingCompProperties
{
    public KnightChivalryDef medalChivalry;
    public int medalCount = 1;

    public BranchBuildingCompProperties_Memorial()
    {
        compClass = typeof(BranchBuildingComp_Memorial);
    }
}

public class BranchBuildingComp_Memorial : BranchBuildingComp
{
    private BranchBuildingCompProperties_Memorial Props => (BranchBuildingCompProperties_Memorial)props;
    public override void PostInitActive()
    {
        base.PostInitActive();

        if (parent.Def.IsHonorSymbol && (parent.Branch.IsBranchOfType(Branch.BranchType.Friendly) || parent.RatkinOrder.Relationship >= EsteemHandler.RelationshipKind.Friendly))
        {
            OrderLetterUtility.ReceiveLetter(
                label: "OARO_MemorialInitActiveLabel".Translate(parent.Def.honorDef.Named(KeyLibrary_FormatArgName.HONORDEF)),
                text: "OARO_MemorialInitActiveText".Translate(parent.Branch.NameColored.Named(KeyLibrary_FormatArgName.BranchName),
                                                         parent.Label.Named("BuildingLabel"),
                                                         parent.Def.honorDef.Named(KeyLibrary_FormatArgName.HONORDEF)),
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: parent.Branch.RatkinOrder,
                relatedBranch: parent.Branch,
                sender: parent.Branch.NameColored,
                relatedLetterType: OrderLetter.RelatedLetterType.Positive);
        }

        if (Props.medalChivalry?.medal is not null)
        {
            BranchMedalHandler medalHandler = parent.Branch.MedalHandler;

            int count = Props.medalCount - medalHandler.GetMedalCount(Props.medalChivalry);
            if (count > 0)
            {
                medalHandler.AdjustMedal(Props.medalChivalry, count);
            }
        }
    }
}