using System;
using System.Collections.Generic;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public class CertainDateLetterDef : SpecialLetterDefBase
{
    private static readonly int curYear = DateTime.Now.Year;

    public int month;
    public int day;
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
            yield return "has an invalid month value. Month must be between 1 and 12.";
        }

        int maxDay = DateTime.DaysInMonth(curYear, month);
        if (day <= 0 || day > maxDay)
        {
            yield return $"has an invalid day value. Day must be between 1 and {maxDay}.";
        }

        if (delayableDays < 0)
        {
            yield return "has an invalid delayableDays value. DelayableDays must be greater than 0.";
            delayableDays = 0;
        }
    }
}