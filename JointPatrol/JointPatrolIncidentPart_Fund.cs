using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentPart_Fund : JointPatrolIncidentPart
{
    [MustTranslate] public string changeReason;
    public float change;

    public override void ApplyPart(JointPatrolIncidentDef def, Branch branch, StringBuilder effectExplain)
    {
        branch.RatkinOrder.FundHandler.AdjustFundsImmediately(change, changeReason);
        effectExplain.AppendLine("OARO_ChangeOffset_Fund".Translate(change.ToStringPercentSigned("0.##")));
    }
}