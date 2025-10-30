using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddBranchClique : QuestNode_AddGeneralClique
{
    public SlateRef<Branch> branch;

    protected override string GetCliqueKey()
    {
        string cliqueKey = this.cliqueKey.GetValue(QuestGen.slate);
        if (!string.IsNullOrEmpty(cliqueKey))
        {
            return cliqueKey;
        }
        Branch branch = this.branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        return branch is null ? string.Empty : QuestClique.GetBranchCliqueKey(branch);
    }

    protected override QuestClique GenerateClique(string cliqueKey)
    {
        Slate slate = QuestGen.slate;
        Branch branch = this.branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        if (branch is null)
        {
            return null;
        }
        QuestClique questClique = base.GenerateClique(cliqueKey);
        questClique.InitForBranch(branch);
        if (!initPotency.GetValue(slate).HasValue)
        {
            questClique.Potency = QuestClique.BranchPotencyToCliquePotency(50);
        }
        if (!initWillingness.GetValue(slate).HasValue)
        {
            if (branch.IsOnJointPatrol())
            {
                questClique.Willingness = Rand.Range(0.3f, 0.75f);
            }
            else
            {
                questClique.Willingness = Rand.Range(0.2f, 0.4f);
            }
        }
        return questClique;
    }
}