using RimWorld.Planet;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_InitJointPatrolCaravanHelp : QuestNode
{
    private SlateRef<Branch> branch;
    private SlateRef<JointPatrolCaravanHelpDef> caravanHelpDef;
    private SlateRef<WorldObject> worldObject;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        WorldObject worldObject = this.worldObject.GetValue(slate);
        if (worldObject is not IJointPatrolCaravanHelpSite caravanHelpSite)
        {
            return;
        }

        JointPatrolCaravanHelpDef caravanHelpDef = this.caravanHelpDef.GetValue(slate) ?? slate.Get<JointPatrolCaravanHelpDef>("caravanHelpDef");
        if (caravanHelpDef is null)
        {
            return;
        }

        Branch branch = this.branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        if (!branch.IsValid())
        {
            return;
        }

        caravanHelpSite.InitJointPatrolCaravanHelp(branch, caravanHelpDef);

    }
}
