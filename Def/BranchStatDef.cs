using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatDef : Def
{
    public enum StatType
    {
        Float,
        Percent,
        Int,
    }
    public StatType statType = StatType.Float; //属性类型
    public Type workerClass = typeof(BranchStatWorker); //属性计算器类
    private BranchStatWorker worker;
    public BranchStatWorker Worker => worker ??= (BranchStatWorker)Activator.CreateInstance(workerClass, args: this);

    public float baseValue = 0f; //基础值

    public bool cacheable = true;
    public float minValue = int.MinValue + 1; //最小值
    public float maxValue = int.MaxValue - 1; //最大值
    public bool nonNegative = false; //是否为非负数
    public int cacheDuration = 10000; //缓存持续时间 (单位：tick)
    public List<BranchStatPart> statParts; //属性修正器列表

    public override void PostLoad()
    {
        if (statType == StatType.Int)
        {
            minValue = Mathf.Round(minValue);
            maxValue = Mathf.Round(maxValue);
        }

        if (minValue > maxValue)
        {
            (minValue, maxValue) = (maxValue, minValue);
        }

        if (nonNegative)
        {
            minValue = Mathf.Max(minValue, 0f);
            maxValue = Mathf.Max(maxValue, 0f);
        }

        baseValue = Mathf.Clamp(baseValue, minValue, maxValue);

        statParts?.SortByDescending(part => part.priority);

    }
}