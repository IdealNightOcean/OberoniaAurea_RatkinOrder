using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddBranchClique : QuestNode
{
    public SlateRef<Branch> branch;
    public SlateRef<float> initWillingness;
    public SlateRef<bool> defaultActive;

    public SlateRef<bool> replaceCur;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Branch branch = this.branch.GetValue(slate);
        if (branch is null)
        {
            return;
        }

        if (!QuestPart_CliquesManager.TryGetCliquesManager(QuestGen.quest, addPartIfMiss: true, out QuestPart_CliquesManager questPart_CliquesManager))
        {
            return;
        }

        QuestClique questClique = new(branch)
        {
            Willingness = initWillingness.GetValue(slate)
        };

        string cliqueKey = QuestClique.GetBranchCliqueKey(branch);
        if (questPart_CliquesManager.AddClique(cliqueKey, questClique, replaceCur.GetValue(slate)))
        {
            if (defaultActive.GetValue(slate))
            {
                questPart_CliquesManager.ActiveClique(cliqueKey);
            }
        }
    }
}