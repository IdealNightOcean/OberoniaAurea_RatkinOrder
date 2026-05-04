using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolProperties
{
    public Type workerClass = typeof(JointPatrolPropWorker);

    private JointPatrolPropWorker worker;
    public JointPatrolPropWorker Worker => worker ??= (JointPatrolPropWorker)Activator.CreateInstance(workerClass);

    [MustTranslate]
    public string taskLabel = string.Empty;
    public string TaskLabelCap => taskLabel.CapitalizeFirst();

    public PathedTexture2D entryBackgroundTexture;

    public PathedTexture2D entryShadeTexture;

    public PathedTexture2D targetBackground;
}