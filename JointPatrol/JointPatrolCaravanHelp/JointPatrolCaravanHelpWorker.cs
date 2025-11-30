using OberoniaAurea_Frame;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolCaravanHelpWorker
{
    public JointPatrolCaravanHelpDef Def { get; set; }
    public abstract bool Notify_CaravanArrived(Caravan caravan, Branch branch, WorldObject_InteractiveBase incidentSite);

    public virtual string HelpDescription(Branch branch)
    {
        return $"{Def.label} ({branch.Name})";
    }

    public virtual string GetRewardText(Branch branch)
    {
        return Def.rewardText.Formatted(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), Def.Named("CARAVANHELPDEF"));
    }

    public virtual void ApplyEffect(Branch branch)
    {
        if (!branch.IsValid())
        {
            return;
        }

        if (!branch.RatkinOrder.JointPatrolManager.ApplyJointCaravanHelpEffect(Def, branch))
        {
            return;
        }

        OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
            label: "OARO_JointPatrolCaravanIncident_ThankLabel".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
            text: GetRewardText(branch),
            def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);

        if (Rand.Chance(Def.recommendationChance))
        {
            orderLetter.Text += ("\n\n" + "OARO_JointPatrolCaravanIncident_ThankWithRecommendation".Translate());
            OrderRecommendation recommendation = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
            recommendation.SetRatkinOrder(branch.RatkinOrder);
            orderLetter.Attachments = [recommendation];
        }
        OrderLetterBox.Instance.ReceiveLetter(orderLetter);
    }

}