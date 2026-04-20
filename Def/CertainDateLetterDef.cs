using System;
using System.Collections.Generic;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 特定日期信件Def
/// </summary>
public class CertainDateLetterDef : SpecialLetterDefBase
{
    private static readonly int curYear = DateTime.Now.Year;

    public int month;
    public int day;

    /// <summary>
    /// 可延迟时间（Day）
    /// </summary>
    public int delayableDays;

    public DateTime EarliestDate => new DateTime(year: curYear, month: month, day: day).Date;
    public DateTime LatestDate => EarliestDate.AddDays(delayableDays).Date;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (month <= 0 || month > 12)
        {
            month = Mathf.Clamp(month, 1, 12);
            yield return $"'{nameof(month)}' 值无效。'{nameof(month)}' 必须在 1 到 12 之间。";
        }

        int maxDay = DateTime.DaysInMonth(curYear, month);
        if (day <= 0 || day > maxDay)
        {
            yield return $"'{nameof(day)}' 值无效。'{nameof(day)}' 必须在 1 到 {maxDay} 之间。";
        }

        if (delayableDays < 0)
        {
            yield return $"'{nameof(delayableDays)}' 值无效。'{nameof(delayableDays)}' 必须大于 0。";
            delayableDays = 0;
        }
    }
}