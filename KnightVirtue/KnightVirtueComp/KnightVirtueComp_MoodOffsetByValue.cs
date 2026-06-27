using RimWorld;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_MoodOffsetByValue : KnightVirtueComp
{
    public KnightVirtueCompProperties_MoodOffsetByValue Props => (KnightVirtueCompProperties_MoodOffsetByValue)props;

    protected abstract float GetValueForStat();

    public override void OnRefreshBuffStage(HediffStageModifierBuilder buffStageBuilder)
    {
        MemoryThoughtHandler memories = this.Pawn.needs?.mood?.thoughts?.memories;
        if (memories is null)
            return;

        Thought_Memory thought = (Thought_Memory)memories.GetFirstMemoryOfDef(Props.thoughtDef);
        if (thought is null)
        {
            thought = (Thought_Memory)ThoughtMaker.MakeThought(Props.thoughtDef);
            thought.permanent = true;
            memories.TryGainMemory(thought);
        }

        int moodOffset = Mathf.RoundToInt(Props.offsetsByValue.Evaluate(GetValueForStat()));
        thought.moodOffset = moodOffset;
    }
}
