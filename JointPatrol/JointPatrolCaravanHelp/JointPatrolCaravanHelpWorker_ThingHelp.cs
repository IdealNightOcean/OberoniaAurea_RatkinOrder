using OberoniaAurea_Frame;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpWorker_ThingHelp : JointPatrolCaravanHelpWorker
{
    public override string HelpDescription(Branch branch)
    {
        JointPatrolCaravanHelp_ThingHelpExtension modEx_ThingHelp = Def.GetModExtension<JointPatrolCaravanHelp_ThingHelpExtension>();
        if (modEx_ThingHelp is null || modEx_ThingHelp.requireReason is null)
        {
            return base.HelpDescription(branch);
        }

        return modEx_ThingHelp.requireReason.Formatted(branch.Named(KeyLibrary_FormatArgName.BranchName),
                                                       modEx_ThingHelp.requireThing.Named(KeyLibrary_FormatArgName.THING),
                                                       modEx_ThingHelp.requireCount.Named(KeyLibrary_FormatArgName.Count),
                                                       Def.Named("CARAVANHELPDEF"));
    }

    public override bool Notify_CaravanArrived(Caravan caravan, Branch branch, WorldObject_InteractiveBase incidentSite)
    {
        JointPatrolCaravanHelp_ThingHelpExtension modEx_ThingHelp = Def.GetModExtension<JointPatrolCaravanHelp_ThingHelpExtension>();
        if (modEx_ThingHelp is null)
        {
            return false;
        }
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(GiveNode(caravan, branch, modEx_ThingHelp), branch.RatkinOrder);
        Find.WindowStack.Add(nodeTree);

        return true;
    }

    private DiaNode GiveNode(Caravan caravan, Branch branch, JointPatrolCaravanHelp_ThingHelpExtension modEx_ThingHelp)
    {
        DiaNode rootNode = new(modEx_ThingHelp.requireReason.Formatted(
            branch.Named(KeyLibrary_FormatArgName.BranchName),
            modEx_ThingHelp.requireThing.Named(KeyLibrary_FormatArgName.THING),
            modEx_ThingHelp.requireCount.Named(KeyLibrary_FormatArgName.Count)));

        DiaOption giveOpt = new("OARO_Give".Translate())
        {
            action = delegate
            {
                caravan.RemoveThingsOfDef(modEx_ThingHelp.requireThing, modEx_ThingHelp.requireCount);
                base.ApplyEffect(branch);
            }

        };
        rootNode.options.Add(giveOpt);
        rootNode.options.Add(OAFrame_DiaUtility.DefaultCancelOption);
        return rootNode;
    }
}
