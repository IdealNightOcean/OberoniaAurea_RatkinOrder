namespace OberoniaAurea.RatkinOrder;

/*
public static class BranchStatUtility
{
    public static string GetStatModifyExplanationStr(BranchStatRequestData requestData, float? baseValueOverride = null, bool showResultValue = true)
    {
        return GetStatModifyExplanation(requestData, baseValueOverride, showResultValue).ToString();
    }

    public static (StringBuilder, float?) GetStatModifyExplanation(BranchStatRequestData requestData, float? baseValueOverride = null, bool showResultValue = true)
    {
        if (requestData is null || requestData.StatDef is null || !requestData.Branch.IsValid())
            return (new StringBuilder(KeyLibrary_Misc.ErrorTipWithColor), null);

        StringBuilder explanation = new(256);
        try
        {
            float baseValue = baseValueOverride ?? requestData.StatDef.baseValue;
            switch (requestData.StatDef.statType)
            {
                case BranchStatDef.StatType.Int:
                    explanation.AppendLine("OARO_StatExplain_BaseValue".Translate(((int)baseValue).ToStringWithSign()));
                    break;
                case BranchStatDef.StatType.Float:
                    explanation.AppendLine("OARO_StatExplain_BaseValue".Translate(baseValue.ToStringWithSign("0.##")));
                    break;
                case BranchStatDef.StatType.Percent:
                    explanation.AppendLine("OARO_StatExplain_BaseValue".Translate(baseValue.ToStringPercent("0.##")));
                    break;
                default: break;
            }

            StatTransformer transformer = new();
            bool hasTrans = false;

            if (branch.RatkinOrder.TransformerHandler.TryGetStatTransformer(statDef, out StatTransformer tempTransformer))
            {
                hasTrans = true;
                explanation.AppendLine("OARO_StatExplain_OrderReformation".Translate());
                tempTransformer.AppendTransToExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                }
            }
            if (branch.TransformerHandler.TryGetStatTransformer(statDef, out tempTransformer))
            {
                hasTrans = true;
                explanation.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
                tempTransformer.AppendTransToExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                }
            }

            float result = (showResultValue && hasTrans) ? transformer.DoTransform(statDef, baseValue) : baseValue;

            List<BranchStatPart> statParts = statDef.statParts;
            if (statParts is not null)
            {
                explanation.AppendLine("OARO_StatExplain_StatParts".Translate());
                if (showResultValue)
                {
                    for (int i = 0; i < statParts.Count; i++)
                    {
                        statParts[i].PostTransform(branch, ref result);
                        statParts[i].ModifyExplanation(branch, statDef, explanation);
                    }
                }
                else
                {
                    for (int i = 0; i < statParts.Count; i++)
                    {
                        statParts[i].ModifyExplanation(branch, statDef, explanation);
                    }
                }
            }

            if (showResultValue)
            {
                result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
                if (statDef.statType == BranchStatDef.StatType.Int)
                {
                    result = Mathf.Round(result);
                }
                statDef.Worker.UpdateStatCache(branch, result);
                AppendStatResultExplanation(explanation, statDef, result, baseValue);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"生成BranchStat修改说明: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetStatModifyExplanation),
                needStackTrace: true);
            explanation = new("ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable));
        }

        return explanation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(new BranchStatRequestData(branch, statDef), baseValueOverride, immediateUpdate);
    }

    public static float GetNewStatValue(BranchStatRequestData requestData, float? baseValueOverride = null)
    {
        float result;

        try
        {
            BranchStatDef statDef = requestData.StatDef;
            statDef.Worker.PrepareInitialBaseValue(requestData, baseValueOverride);
            result = requestData.BaseValue;

            Branch branch = requestData.Branch;

            StatTransformer transformer = new();
            bool hasTransformer = false;
            if (branch.RatkinOrder.TransformerHandler.TryGetStatTransformer(statDef, out StatTransformer tempTransformer))
            {
                transformer.MergeWith(tempTransformer);
                hasTransformer = true;
            }
            if (branch.TransformerHandler.TryGetStatTransformer(statDef, out tempTransformer))
            {
                transformer.MergeWith(tempTransformer);
                hasTransformer = true;
            }
            if (hasTransformer)
            {
                result = transformer.DoTransform(statDef, baseValueOverride);
            }
            else
            {
                result = baseValueOverride ?? statDef.baseValue;
            }
            if (statDef.statParts is not null)
            {
                for (int i = 0; i < statDef.statParts.Count; i++)
                {
                    statDef.statParts[i].PostTransform(branch, ref result);
                }
            }

            result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Int)
            {
                result = Mathf.Round(result);
            }
        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? requestData?.StatDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的BranchStat值: [BranchStat: {requestData?.StatDef?.label}, BranchId: {requestData?.Branch?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetNewStatValue),
                needStackTrace: true);
        }

        return result;
    }

    public static float GetNewStatValueFormTrans(this Branch branch, BranchStatDef statDef, StatTransformer transformer, float? baseValueOverride = null)
    {
        float result;
        try
        {
            result = transformer.DoTransform(statDef, baseValueOverride);
            if (statDef.statParts is not null)
            {
                for (int i = 0; i < statDef.statParts.Count; i++)
                {
                    statDef.statParts[i].PostTransform(branch, ref result);
                }
            }

            result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Int)
            {
                result = Mathf.Round(result);
            }
        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? statDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的BranchStat值: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetNewStatValueFormTrans),
                needStackTrace: true);
        }
        return result;
    }
}
*/