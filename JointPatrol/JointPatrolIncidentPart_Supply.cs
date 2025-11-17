using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentPart_Supply : JointPatrolIncidentPart
{
    public float change;

    public override void ApplyPart(JointPatrolIncidentDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        record.Branch.Supply += change;
        effectExplain.AppendLine("OARO_ChangeOffset_BranchSupply".Translate(change.ToStringPercentSigned("0.##")));
    }
}