using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolInteractionPart_Supply : JointPatrolInteractionPart
{
    public float change;

    public override void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        record.Branch.Supply += change;
        effectExplain.AppendLine("OARO_ChangeOffset_BranchSupply".Translate(change.ToStringPercentSigned("0.##")));
    }
}