using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Utility;
using static OberoniaAurea.RatkinOrder.WorldObject_NobilityTerritory;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 一个特化的用于叛乱镇压 - 贵族领地 贵族类型初始化的类
/// 应该在QuestEffectTag后使用，否则强制贵族类型不会生效
/// </summary>
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
    public Stack<(NobilityType, bool)> Alternatives;

    public override void Cleanup()
    {
        base.Cleanup();
        Alternatives = null;
    }

    public void InitAlternatives()
    {
        int selCount = 4;
        Alternatives = new(4);
        List<NobilityType> allTypes = EnumUtility.GetValues<NobilityType>().Where(nt => nt != NobilityType.None).ToList();
        if (QuestPart_EffectTags.TryGetEffectTagsPart(quest, addPartIfMiss: false, out QuestPart_EffectTags questPart_EffectTags))
        {
            if (questPart_EffectTags.HasTag("AKindnessLord"))
            {
                Alternatives.Push((NobilityType.Kindness, true));
                allTypes.Remove(NobilityType.Kindness);
                selCount--;
            }
            if (questPart_EffectTags.HasTag("AKindnessLord"))
            {
                Alternatives.Push((NobilityType.Tyrannical, true));
                allTypes.Remove(NobilityType.Tyrannical);
                selCount--;
            }

        }

        foreach (NobilityType type in allTypes.TakeRandomDistinct(selCount))
        {
            Alternatives.Push((type, false));
        }
    }

    public (NobilityType, bool) PopAlternative()
    {
        if (Alternatives is null || Alternatives.Count == 0)
        {
            return (NobilityType.None, false);
        }
        return Alternatives.Pop();
    }
}
