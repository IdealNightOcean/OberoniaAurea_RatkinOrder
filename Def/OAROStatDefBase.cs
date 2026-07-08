using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class OAROStatDefBase : Def
{
    public enum StatType : byte
    {
        Float,
        Int,
        Percent
    }

    protected Type workerClass;

    /// <summary>
    /// 属性类型，标记应该如何显示属性数值
    /// </summary>
    public StatType statType = StatType.Float;

    /// <summary>是否反转好坏</summary>
    /// <remarks>
    /// <para>- 一般的Stat越大越好（如影响距离）</para> 
    /// <para>- 反转的Stat越小越好（如建设花费系数）</para> 
    /// </remarks>
    public bool reverse;

    /// <summary>
    /// 基础值
    /// </summary>
    public float baseValue = 0f;

    /// <summary>
    /// 该Stat是否可以缓存
    /// </summary>
    public bool cacheable = true;

    /// <summary>
    /// 最小值
    /// </summary>
    public float minValue = int.MinValue;

    /// <summary>
    /// 最大值
    /// </summary>
    public float maxValue = int.MaxValue;

    /// <summary>
    /// 是否为[非]负数
    /// </summary>
    public bool nonNegative = false;

    /// <summary>
    /// 缓存持续时间（Tick）
    /// </summary>
    public int cacheDuration = 10000;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (nonNegative)
        {
            if (minValue < 0f)
            {
                minValue = 0f;
                yield return $"'{nameof(nonNegative)}' 已设置为 true，但 '{minValue}' 为负数。";
            }
            if (maxValue < 0f)
            {
                maxValue = 0f;
                yield return $"'{nameof(nonNegative)}' 已设置为 true，但 '{maxValue}' 为负数。";
            }
        }

        if (minValue > maxValue)
        {
            (minValue, maxValue) = (maxValue, minValue);
            yield return $"最小值 '{minValue}' 大于最大值 '{maxValue}'。";
        }
    }

    public override void PostLoad()
    {
        if (statType == StatType.Int)
        {
            minValue = Mathf.Floor(minValue);
            maxValue = Mathf.Ceil(maxValue);
        }

        baseValue = Mathf.Clamp(baseValue, minValue, maxValue);
    }
}