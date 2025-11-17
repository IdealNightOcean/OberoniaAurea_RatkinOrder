using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolIncidentPart
{
    public abstract void ApplyPart(JointPatrolIncidentDef def, JointBranchRecord record, StringBuilder effectExplain);
}