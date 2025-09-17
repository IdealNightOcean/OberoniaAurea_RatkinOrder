using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetWorldObjectName : QuestNode
{
    [MayTranslate]
    public SlateRef<string> fixedName;

    public SlateRef<RulePackDef> nameMaker;
    public SlateRef<WorldObject> worldObejct;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        if (worldObejct.GetValue(slate) is not INameableWorldObject nameableWorldObject)
        {
            return;
        }

        string fixedName = this.fixedName.GetValue(slate);
        if (!fixedName.NullOrEmpty())
        {
            nameableWorldObject.Name = fixedName;
            return;
        }

        RulePackDef nameMaker = this.nameMaker.GetValue(slate);
        if (nameMaker is null)
        {
            string name = NameGenerator.GenerateName(nameMaker);
            nameableWorldObject.Name = name;
        }
    }
}