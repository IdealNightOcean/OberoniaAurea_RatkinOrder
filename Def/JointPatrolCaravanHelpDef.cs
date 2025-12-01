using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpDef : JointPatrolInteractionDef
{
    public Type workerClass;
    private JointPatrolCaravanHelpWorker worker;
    public JointPatrolCaravanHelpWorker Worker
    {
        get
        {
            if (worker is null)
            {
                worker = (JointPatrolCaravanHelpWorker)Activator.CreateInstance(workerClass);
                worker.Def = this;
            }
            return worker;
        }
    }

    public WorldObjectDef relatedWorldObject;
    public float recommendationChance;
    public int timeOutTicks = 60000;

    [MustTranslate]
    public string requestHelpReason;
    [MustTranslate]
    public string rewardText;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (workerClass is null)
        {
            yield return $"has a null {nameof(workerClass)}";
        }
        if (relatedWorldObject is null)
        {
            yield return $"has a null {nameof(relatedWorldObject)}";
        }
    }
}