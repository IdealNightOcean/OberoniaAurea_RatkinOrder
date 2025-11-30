using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class JointPatrolManager
{
    public enum PatrolState : byte
    {
        Invalid,
        Prepare,
        Ongoing
    }

    public enum PatrolLevel : byte
    {
        Popedom,
        Kingdom,
        Border
    }

    public enum HelpPolicy : byte
    {
        None,
        OnlyFriendly,
        All
    }

    public class JointInteractionRecord : IExposable
    {
        public int TriggerTick;
        public string Label;
        public string Description;
        public Branch RelatedBranch;

        public void ExposeData()
        {
            Scribe_Values.Look(ref TriggerTick, "TriggerTick", 0);
            Scribe_Values.Look(ref Label, "Label");
            Scribe_Values.Look(ref Description, "Description");
            Scribe_References.Look(ref RelatedBranch, "RelatedBranch");
        }
    }
}