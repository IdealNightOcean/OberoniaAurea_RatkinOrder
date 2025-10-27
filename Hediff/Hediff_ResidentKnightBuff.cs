using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ResidentKnightBuff : Hediff
{
    public override int CurStageIndex => buffStageIndex;

    private int buffStageIndex;

    public void SetBuffStage(int buffStageIndex)
    {
        this.buffStageIndex = Mathf.Min(def.stages?.Count ?? 0, buffStageIndex);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref buffStageIndex, "buffStageIndex", 0);
    }
}
