using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// <see cref="HediffStageTemplate"/> 是一个用于构建 <see cref="HediffStage"/> 的模板类，允许在准备阶段累积统计偏移和因子，并在最终化后生成新的 <see cref="HediffStage"/> 实例。
/// </summary>
/// <remarks>
/// <para> - 修改模版前需要调用 <see cref="ResetTemplate"/> 方法将模板重置为可修改状态。</para>
/// <para> - 使用模版前需要调用 <see cref="FinalizeTemplate"/> 方法以确保模板准备就绪。</para>
/// </remarks>
public class HediffStageTemplate
{
    private enum TemplateState
    {
        Invalid,
        Modifying,
        Ready
    }

    private TemplateState state = TemplateState.Invalid;
    public bool IsReady => state == TemplateState.Ready;

    private readonly Dictionary<StatDef, float> offsetDict = [];
    private readonly Dictionary<StatDef, float> factorDict = [];

    public IReadOnlyDictionary<StatDef, float> OffsetDictForReading => offsetDict;
    public IReadOnlyDictionary<StatDef, float> FactorDictForReading => factorDict;

    public bool HasAnyModifier => offsetDict.Count > 0 || factorDict.Count > 0;

    private List<StatModifier> cachedOffsetModifiers;
    private List<StatModifier> cachedFactorModifiers;

    public void MarkInvalid()
    {
        state = TemplateState.Invalid;
    }

    public void ResetTemplate()
    {
        state = TemplateState.Modifying;

        offsetDict.Clear();
        factorDict.Clear();
        cachedOffsetModifiers = null;
        cachedFactorModifiers = null;
    }

    public void FinalizeTemplate()
    {
        if (state == TemplateState.Ready) return;

        cachedOffsetModifiers = new(offsetDict.Count);
        foreach (KeyValuePair<StatDef, float> kv in offsetDict)
        {
            cachedOffsetModifiers.Add(new StatModifier { stat = kv.Key, value = kv.Value });
        }

        cachedFactorModifiers = new(factorDict.Count);
        foreach (KeyValuePair<StatDef, float> kv in factorDict)
        {
            cachedFactorModifiers.Add(new StatModifier { stat = kv.Key, value = kv.Value });
        }

        offsetDict.Clear();
        factorDict.Clear();
        state = TemplateState.Ready;
    }

    public void AddOffset(StatDef stat, float offset)
    {
        if (state != TemplateState.Modifying)
        {
            Log.Error($"{nameof(HediffStageTemplate)} cannot be modified as it is not marked for {nameof(TemplateState.Modifying)}.");
            return;
        }
        if (stat is null || offset == 0f) return;
        offsetDict[stat] = offsetDict.TryGetValue(stat, out float current) ? current + offset : offset;
    }

    public void AddOffsets(IEnumerable<StatModifier> modifiers)
    {
        if (state != TemplateState.Modifying)
        {
            Log.Error($"{nameof(HediffStageTemplate)} cannot be modified as it is not marked for {nameof(TemplateState.Modifying)}.");
            return;
        }
        if (modifiers is null) return;
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.stat is null || modifier.value == 0f) continue;
            offsetDict[modifier.stat] = offsetDict.TryGetValue(modifier.stat, out float current) ? current + modifier.value : modifier.value;
        }
    }

    public void AddFactor(StatDef stat, float factor)
    {
        if (state != TemplateState.Modifying)
        {
            Log.Error($"{nameof(HediffStageTemplate)} cannot be modified as it is not marked for {nameof(TemplateState.Modifying)}.");
            return;
        }
        if (stat is null || factor == 1f) return;
        factorDict[stat] = factorDict.TryGetValue(stat, out float current) ? current * factor : factor;
    }
    public void AddFactors(IEnumerable<StatModifier> modifiers)
    {
        if (state != TemplateState.Modifying)
        {
            Log.Error($"{nameof(HediffStageTemplate)} cannot be modified as it is not marked for {nameof(TemplateState.Modifying)}.");
            return;
        }
        if (modifiers is null) return;
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.stat is null || modifier.value == 1f) continue;
            factorDict[modifier.stat] = factorDict.TryGetValue(modifier.stat, out float current) ? current * modifier.value : modifier.value;
        }
    }

    public HediffStage GetNewHediffStage()
    {
        if (state != TemplateState.Ready)
        {
            Log.Error($"{nameof(HediffStageTemplate)} is not ready. Call {nameof(FinalizeTemplate)} before getting a new HediffStage.");
            return null;
        }

        HediffStage stage = new();
        if (!cachedOffsetModifiers.NullOrEmpty())
        {
            stage.statOffsets = [.. cachedOffsetModifiers];
        }
        if (!cachedFactorModifiers.NullOrEmpty())
        {
            stage.statFactors = [.. cachedFactorModifiers];
        }
        return stage;
    }

}