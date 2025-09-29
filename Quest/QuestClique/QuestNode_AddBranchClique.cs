using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddBranchClique : QuestNode_AddGeneralClique
{
    public SlateRef<Branch> branch;
    public SlateRef<bool> initWithBranchPotency = true;

    protected override string GetCliqueKey()
    {
        return cliqueKey.GetValue(QuestGen.slate) ?? QuestClique.GetBranchCliqueKey(branch.GetValue(QuestGen.slate));
    }

    protected override QuestClique GenerateClique()
    {
        QuestClique questClique = base.GenerateClique();
        questClique.InitForBranch(branch.GetValue(QuestGen.slate), initWithBranchPotency.GetValue(QuestGen.slate));
        return questClique;
    }
}