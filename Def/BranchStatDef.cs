using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatDef : Def
{
    public enum StatType : byte
    {
        Float,
        Int,
        Percent
    }

    private BranchStatWorker worker;
    public BranchStatWorker Worker => worker ??= new BranchStatWorker(this);

    public StatType statType = StatType.Float; //属性类型
    /// <summary>
    /// 一般的Stat越大越好（如影响距离）
    /// 反转的Stat越小越好（如建设花费系数）
    /// </summary>
    public bool reverse;

    public float baseValue = 0f; //基础值

    public bool cacheable = true;
    public float minValue = int.MinValue; //最小值
    public float maxValue = int.MaxValue; //最大值
    public bool nonNegative = false; //是否为非负数
    public int cacheDuration = 10000; //缓存持续时间 (单位：tick)
    public List<BranchStatPart> statParts; //属性修正器列表

    public override void PostLoad()
    {
        if (nonNegative)
        {
            minValue = Mathf.Max(minValue, 0f);
            maxValue = Mathf.Max(maxValue, 0f);
        }

        if (minValue > maxValue)
        {
            (minValue, maxValue) = (maxValue, minValue);
        }

        if (statType == StatType.Int)
        {
            minValue = Mathf.FloorToInt(minValue);
            maxValue = Mathf.CeilToInt(maxValue);
        }

        baseValue = Mathf.Clamp(baseValue, minValue, maxValue);

        statParts?.SortByDescending(part => part.Priority);
    }
}