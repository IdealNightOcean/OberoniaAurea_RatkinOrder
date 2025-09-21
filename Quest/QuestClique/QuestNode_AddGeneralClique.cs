using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddGeneralClique : QuestNode
{
    [NoTranslate]
    public SlateRef<string> cliqueKey;

    public SlateRef<string> cliqueName;
    public SlateRef<float> initPotency;
    [MayTranslate]
    public SlateRef<string> initPotencyDesc;
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
            Name = cliqueName.GetValue(slate) ?? "UNKOWN",
            Potency = initPotency.GetValue(slate),
            PotencyDesc = initPotencyDesc.GetValue(slate) ?? string.Empty,
            Willingness = initWillingness.GetValue(slate)
        };

        if (questPart_CliquesManager.AddClique(cliqueKey, questClique, replaceCur.GetValue(slate)))
        {
            if (defaultActive.GetValue(slate))
            {
                questPart_CliquesManager.ActiveClique(cliqueKey);
            }
        }
    }
}