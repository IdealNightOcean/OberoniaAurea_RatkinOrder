using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士属性Def
/// </summary>
public class ResidentKnightStatDef : OAROStatDefBase
{
    private static readonly Type defaultWorker = typeof(ResidentKnightStatWorker);


    private ResidentKnightStatWorker worker;

    public ResidentKnightStatWorker Worker => worker ??= (ResidentKnightStatWorker)Activator.CreateInstance(workerClass, this);

    public List<ResidentKnightStatPart> statParts;

    public ResidentKnightStatDef() : base()
    {
        workerClass = defaultWorker;
    }

    public override void PostLoad()
    {
        base.PostLoad();
        statParts?.SortByDescending(part => part.Priority);
    }
}
