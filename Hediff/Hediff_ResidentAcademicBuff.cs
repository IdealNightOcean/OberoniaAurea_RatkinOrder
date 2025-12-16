using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ResidentAcademicBuff : HediffWithComps
{
    public override int CurStageIndex => buffStageIndex;

    private int buffStageIndex;

    public virtual void Notify_AcademicStageChanged(int newAcademicStageIndex)
    {
        buffStageIndex = Mathf.Min(def.stages?.Count ?? 0, newAcademicStageIndex);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref buffStageIndex, nameof(buffStageIndex), 0);
    }
}
