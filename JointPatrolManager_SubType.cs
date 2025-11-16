using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class JointPatrolManager
{
    public enum PatrolLevel : byte
    {
        Popedom,
        Kingdom,
        Border
    }

    [Flags]
    public enum PatrolInteractionType : byte
    {
        None = 0,
        Military = 1,
        Information = 2,
        Diplomacy = 4
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

    public class JointBranchRecord : IExposable
    {
        private static readonly PatrolInteractionType allInteraction = PatrolInteractionType.Military | PatrolInteractionType.Information | PatrolInteractionType.Diplomacy;

        public Branch Branch;
        private float reconnaissance;
        public float Reconnbissance => reconnaissance;

        public PatrolInteractionType CurInteractions;

        public int NextIncidentCheckTick = -1;

        public void ExposeData()
        {
            Scribe_References.Look(ref Branch, "Branch");
            Scribe_Values.Look(ref reconnaissance, "reconnaissance", 0f);
            Scribe_Values.Look(ref CurInteractions, "CurInteractions", PatrolInteractionType.None);
            Scribe_Values.Look(ref NextIncidentCheckTick, "NextIncidentCheckTick", -1);
        }

        /// <summary>
        /// 4小时更新一次 (10000 tick)
        /// </summary>
        public void RecordUpdate()
        {
            GetReconnaissance();

        }

        public void ActiveInteraction(PatrolInteractionType interaction)
        {
            if (HasInteraction(interaction))
            {
                return;
            }

            CurInteractions |= interaction;
        }

        public bool HasInteraction(PatrolInteractionType interaction) => (CurInteractions & interaction) != 0;

        private void GetReconnaissance()
        {
            reconnaissance = (Branch.Squad.MemberCount * 10f)
                  * (1f + Branch.MedalHandler.MedalTypeCount * 0.1f)
                  * (1f + Branch.FacilityHandler.TotalFacilityLevel * 0.02f)
                  * (Branch.IsBranchOfType(Branch.BranchType.Honor) ? 1.2f : 1f);
        }

    }

}