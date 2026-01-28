using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTraditionDef : Def
{
    private static readonly Type DefaultTraditionClass = typeof(BranchTradition);

    /// <summary>
    /// 传统功能类
    /// </summary>
    public Type traditionClass = typeof(BranchTradition);

    /// <summary>
    /// 基础设立消耗（印记类型）
    /// </summary>
    public BranchMedalDef medalDef;

    /// <summary>
    /// 可修行的课业
    /// </summary>
    public Def academicDef;

    /// <summary>
    /// 基础设立消耗（印记数量）
    /// </summary>
    public int baseMedalCost = 5;

    /// <summary>
    /// 基础设立消耗（自新点数）
    /// </summary>
    public float baseRenewalPointsCost = 10f;

    /// <summary>
    /// 非对应大类荣誉分队额外消耗（印记数量）
    /// </summary>
    public int mismatchMedalCost = 5;

    /// <summary>
    /// 非对应大类荣誉分队额外消耗（自新点数）
    /// </summary>
    public float mismatchRenewalPointsCost = 10f;

    /// <summary>
    /// 传承设立推荐信消耗
    /// </summary>
    public int inheritRecommendationLetterCost = 2;
    /// <summary>
    /// 升级消耗（印记数量）
    /// </summary>
    public int upgradeMedalCost = 5;

    /// <summary>
    /// 等级阶段配置
    /// </summary>
    public List<BranchTraditionStage> levelStages;

    /// <summary>
    /// 最大等级
    /// </summary>
    public int MaxLevel => levelStages?.Count ?? 0;

    public BranchTraditionStage GetLevelStage(int level)
    {
        if (level > 0 && level <= levelStages.Count)
        {
            return levelStages[level - 1];
        }
        return null;
    }
}