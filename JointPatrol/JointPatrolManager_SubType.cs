using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class JointPatrolManager
{
    public enum PatrolState : byte
    {
        Prepare,
        Ongoing,
        Invalid
    }

    public enum PatrolLevel : byte
    {
        Popedom,
        Kingdom,
        Border
    }

    public class JointIncidentRecord : IExposable
    {
        public int TriggerTick;
        public JointPatrolIncidentDef Def;
        public string Description;
        public Branch RelatedBranch;

        public void ExposeData()
        {
            Scribe_Values.Look(ref TriggerTick, "TriggerTick", 0);
            Scribe_Values.Look(ref Description, "Description");
            Scribe_Defs.Look(ref Def, "Def");
            Scribe_References.Look(ref RelatedBranch, "RelatedBranch");
        }
    }



}