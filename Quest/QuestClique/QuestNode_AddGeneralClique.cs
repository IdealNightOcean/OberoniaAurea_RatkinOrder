using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddGeneralClique : QuestNode
{
    [NoTranslate]
    public SlateRef<string> cliqueKey;

    [MustTranslate]
    public SlateRef<string> cliqueName = string.Empty;
    public SlateRef<float?> initPotency;
    public SlateRef<float?> initWillingness;

    [MustTranslate]
    public SlateRef<string> activeDesc = string.Empty;
    [MustTranslate]
    public SlateRef<string> inactiveDesc = string.Empty;



    public SlateRef<bool> isActivatable = true;
    public SlateRef<bool> isCommunicable;
    public SlateRef<bool> isBribable;
    public SlateRef<int> briberyCost = -1;
    public SlateRef<BranchBuildingDef> preferredBuilding;

    public SlateRef<bool> defaultActive;

    public SlateRef<bool> replaceCur;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        string cliqueKey = GetCliqueKey();
        if (string.IsNullOrEmpty(cliqueKey))
        {
            return;
        }

        if (!QuestGen.quest.TryGetCliquesManager(addPartIfMiss: true, out QuestPart_CliquesManager questPart_CliquesManager))
        {
            return;
        }

        QuestClique questClique = GenerateClique(cliqueKey);
        if (questClique is not null)
        {
            questPart_CliquesManager.TryAddClique(questClique, replaceCur.GetValue(slate), defaultActive.GetValue(slate));
        }
    }

    protected virtual string GetCliqueKey()
    {
        return cliqueKey.GetValue(QuestGen.slate);
    }

    protected virtual QuestClique GenerateClique(string cliqueKey)
    {
        Slate slate = QuestGen.slate;
        QuestClique questClique = new(cliqueKey)
        {
            Name = cliqueName.GetValue(slate),
            ActiveDesc = activeDesc.GetValue(slate),
            InactiveDesc = inactiveDesc.GetValue(slate),

            Potency = initPotency.GetValue(slate) ?? 0f,
            Willingness = initWillingness.GetValue(slate) ?? 0f,

            IsActivatable = isActivatable.GetValue(slate),
            IsCommunicable = isCommunicable.GetValue(slate),
            IsBribable = isBribable.GetValue(slate),
            BriberyCost = briberyCost.GetValue(slate),

            PreferredBuilding = preferredBuilding.GetValue(slate)
        };
        return questClique;
    }
}