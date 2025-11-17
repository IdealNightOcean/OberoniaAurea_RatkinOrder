using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentPart_Fund : JointPatrolIncidentPart
{
    [MustTranslate] public string changeReason;
    public float change;

    public override void ApplyPart(JointPatrolIncidentDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        record.Branch.RatkinOrder.FundHandler.AdjustFundsImmediately(change, changeReason ?? def.label);
        effectExplain.AppendLine("OARO_ChangeOffset_Fund".Translate(change.ToStringPercentSigned("0.##")));
    }
}