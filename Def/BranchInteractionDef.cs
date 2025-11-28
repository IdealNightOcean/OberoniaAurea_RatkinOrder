using System;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionDef : InteractionDefBase
{
    /// <summary>
    /// 交互功能类
    /// </summary>
    public Type workerClass;
    private BranchInteractionWorker worker;
    public BranchInteractionWorker Worker => worker ??= (BranchInteractionWorker)Activator.CreateInstance(workerClass, args: this);

    /// <summary>
    /// 是否仅作为建筑交互
    /// </summary>
    public bool onlyBuildingInteraction;

    /// <summary>
    /// 分部补给下限
    /// </summary>
    public float needSupply = -1f;

    /// <summary>
    /// 分部人口下限
    /// </summary>
    public int floorPopulation = -1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (workerClass is null)
        {
            yield return $"has a null {nameof(workerClass)}.";
        }
    }
}