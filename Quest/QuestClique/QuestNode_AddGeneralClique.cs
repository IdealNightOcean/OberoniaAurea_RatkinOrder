using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddGeneralClique : QuestNode
{
    [NoTranslate]
    public SlateRef<string> cliqueKey = "UNKOWN";

    [MustTranslate]
    public SlateRef<string> cliqueName;
    public SlateRef<float> initPotency;

    [MustTranslate]
    public SlateRef<string> activeDesc;
    [MustTranslate]
    public SlateRef<string> inactiveDesc;

    public SlateRef<float> initWillingness;
    public SlateRef<bool> canBribable;
    public SlateRef<bool> defaultActive;

    public SlateRef<bool> replaceCur;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        string cliqueKey = this.cliqueKey.GetValue(slate);
        if (cliqueKey.NullOrEmpty())
        {
            return;
        }

        if (!QuestPart_CliquesManager.TryGetCliquesManager(QuestGen.quest, addPartIfMiss: true, out QuestPart_CliquesManager questPart_CliquesManager))
        {
            return;
        }

        QuestClique questClique = new()
        {
            Name = cliqueName.GetValue(slate),
            Potency = initPotency.GetValue(slate),
            ActiveDesc = activeDesc.GetValue(slate) ?? string.Empty,
            InactiveDesc = inactiveDesc.GetValue(slate) ?? string.Empty,
            CanBribable = canBribable.GetValue(slate)
        };
        questClique.AdjustCliqueWillingness(initWillingness.GetValue(slate), record: false);

        if (questPart_CliquesManager.AddClique(cliqueKey, questClique, replaceCur.GetValue(slate)))
        {
            if (defaultActive.GetValue(slate))
            {
                questPart_CliquesManager.ActiveClique(cliqueKey);
            }
        }
    }
}