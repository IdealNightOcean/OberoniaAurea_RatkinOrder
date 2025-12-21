using System;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionDef : InteractionDefBase
{
    public enum InteractionTarget
    {
        None,
        Caravan,
        Map
    }

    /// <summary>
    /// 交互功能类
    /// </summary>
    public Type workerClass;
    private BranchInteractionWorker worker;
    public BranchInteractionWorker Worker => worker ??= (BranchInteractionWorker)Activator.CreateInstance(workerClass, args: this);

    public InteractionTarget target = InteractionTarget.None;

    /// <summary>
    /// 是否仅作为建筑交互
    /// </summary>
    public bool onlyBuildingInteraction;

    /// <summary>
    /// 仅限友好分部
    /// </summary>
    public bool friendlyOnly;

    /// <summary>
    /// 仅限荣誉分部
    /// </summary>
    public bool honorOnly;
    /// <summary>
    /// 限制荣誉类型
    /// </summary>
    public BranchHonorDef honorDef;

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
        if (honorDef is not null && !honorOnly)
        {
            yield return $"{honorDef} is not null but {honorOnly}  is false";
        }
    }
}