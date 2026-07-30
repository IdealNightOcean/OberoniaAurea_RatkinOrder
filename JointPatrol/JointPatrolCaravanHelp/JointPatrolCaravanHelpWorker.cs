using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld.Planet;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolCaravanHelpWorker
{
    public JointPatrolCaravanHelpDef Def { get; set; }
    protected Branch Branch { get; private set; }
    protected Caravan Caravan { get; private set; }
    protected WorldObject_InteractiveBase IncidentSite { get; private set; }
    protected readonly StringBuilder extraRewardText = new(64);

    public abstract bool Notify_CaravanArrived(Caravan caravan, Branch branch, WorldObject_InteractiveBase incidentSite);

    public virtual string RequestHelpReason(Branch branch)
    {
        return Def.requestHelpReason?.Formatted(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), Def.Named(OARO_KeyLibrary_FormatArgName.CARAVANHELPDEF)) ?? $"{Def.label} ({branch.Name})";
    }

    private string GetRewardText(Branch branch)
    {
        string rewardText = Def.rewardText.Formatted(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), Def.Named(OARO_KeyLibrary_FormatArgName.CARAVANHELPDEF));
        if (extraRewardText.Length > 0)
        {
            rewardText += ("\n" + extraRewardText.ToString());
        }
        extraRewardText.Clear();
        return rewardText;
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
            label: "OARO_JointPatrolCaravanIncident_ThankLabel".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
            text: TaggedString.Empty,
            def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);

        if (Rand.Chance(Def.recommendationChance))
        {
            OrderRecommendation recommendation = RecommendationUtility.MakeRecommendationForPlayer(count: 1);
            orderLetter.AddAttachment(recommendation);

            extraRewardText.AppendLine();
            extraRewardText.AppendLine("OARO_JointPatrolCaravanIncident_ThankWithRecommendation".Translate());
        }

        orderLetter.Text = GetRewardText(branch);
        OrderLetterBox.Instance.ReceiveLetter(orderLetter);
    }

}