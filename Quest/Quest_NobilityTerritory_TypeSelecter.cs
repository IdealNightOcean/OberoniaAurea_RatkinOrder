using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal sealed class QuestNode_NobilityTerritory_TypeSelecter : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestPart_NobilityTerritory_TypeSelecter questPart_NobilityTerritory_TypeSelecter = new();
        questPart_NobilityTerritory_TypeSelecter.InitAlternatives();
        QuestGen.quest.AddPart(questPart_NobilityTerritory_TypeSelecter);
    }
}

internal sealed class QuestPart_NobilityTerritory_TypeSelecter : QuestPart
{
    public Stack<(WorldObject_NobilityTerritory.NobilityType, bool)> Alternatives = new(4);

    public void InitAlternatives()
    {
        int selCount = 4;
        List<WorldObject_NobilityTerritory.NobilityType> allTypes = Enum.GetValues(typeof(WorldObject_NobilityTerritory.NobilityType)).Cast<WorldObject_NobilityTerritory.NobilityType>().ToList();
        allTypes.Remove(WorldObject_NobilityTerritory.NobilityType.None);
        if (QuestPart_EffectTags.TryGetEffectTagsPart(quest, addPartIfMiss: false, out QuestPart_EffectTags questPart_EffectTags))
        {
            if (questPart_EffectTags.HasTag("AKindnessLord"))
            {
                Alternatives.Push((WorldObject_NobilityTerritory.NobilityType.Kindness, true));
                allTypes.Remove(WorldObject_NobilityTerritory.NobilityType.Kindness);
                selCount--;
            }
            if (questPart_EffectTags.HasTag("AKindnessLord"))
            {
                Alternatives.Push((WorldObject_NobilityTerritory.NobilityType.Tyrannical, true));
                allTypes.Remove(WorldObject_NobilityTerritory.NobilityType.Tyrannical);
                selCount--;
            }

        }

        foreach (WorldObject_NobilityTerritory.NobilityType type in allTypes.TakeRandomDistinct(selCount))
        {
            Alternatives.Push((type, false));
        }
    }

    public (WorldObject_NobilityTerritory.NobilityType, bool) PopAlternative()
    {
        if (Alternatives.Count == 0)
        {
            return (WorldObject_NobilityTerritory.NobilityType.None, false);
        }
        return Alternatives.Pop();
    }
}
