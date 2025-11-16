using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolIncidentPart
{
    public abstract void ApplyPart(JointPatrolIncidentDef def, Branch branch, StringBuilder effectExplain);
}