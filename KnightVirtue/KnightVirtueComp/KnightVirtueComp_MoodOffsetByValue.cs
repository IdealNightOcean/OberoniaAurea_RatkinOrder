using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_MoodOffsetByValue : KnightVirtueComp
{
    public KnightVirtueCompProperties_MoodOffsetByValue Props => (KnightVirtueCompProperties_MoodOffsetByValue)props;

    protected abstract float GetValueForStat();

    public override void OnRefreshBuffStage(HediffStageModifierBuilder buffStageBuilder)
    {
        Thought_Memory memory = this.Pawn.GetOrAddMemory(Props.giveParams);
        if (memory is null)
            return;

        int moodOffset = Mathf.RoundToInt(Props.offsetsByValue.Evaluate(GetValueForStat()));
        memory.moodOffset = moodOffset;
    }

    public override void PostRemove() => this.Pawn.RemoveAllMemoriesOfDef(Props.giveParams.MemoryToGive);
}
