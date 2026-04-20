using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部驻派部署Def
/// </summary>
public class BranchResidentDef : Def
{
    /// <summary>
    /// 部署功能类
    /// </summary>
    public Type residentClass;

    /// <summary>
    /// 默认部署天数
    /// </summary>
    public int defaultDeployDays = 1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (residentClass is null)
        {
            yield return $"'{nameof(residentClass)}' 为 null。";
        }
    }
}