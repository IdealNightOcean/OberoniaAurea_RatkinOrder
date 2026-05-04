using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部印记处理器 - 包含印记的记录、主要印记、印记带来的属性加成等相关内容
/// </summary>
public class BranchMedalHandler : IExposable
{
    private Dictionary<KnightChivalryDef, BranchMedalRecord> medalRecords = new(4);

    private KnightChivalryDef primaryChivalry;
    public KnightChivalryDef PrimaryChivalry => primaryChivalry ??= medalRecords?.FirstOrFallback().Key;

    public MedalProperties PrimaryMedal => PrimaryChivalry?.medal;

    public int MedalTypeCount => medalRecords.Count;
    public IReadOnlyDictionary<KnightChivalryDef, BranchMedalRecord> MedalRecords => medalRecords;


    [Unsaved] private int totalMedalCount = -1;
    public int TotalMedalCount => totalMedalCount;

    [Unsaved] private HediffStage medalHediffStage;
    [Unsaved] private bool medalHediffsDirty = true;
    public HediffStage MedalHediffStage
    {
        get
        {
            if (medalHediffsDirty)
            {
                RecacheMedalHediffStage();
            }
            return medalHediffStage;
        }
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref primaryChivalry, nameof(primaryChivalry));
        Scribe_Collections.Look(ref medalRecords, nameof(medalRecords), LookMode.Def, LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"主印记: {PrimaryMedal}");
        listing_Rect.Label("所有印记:");
        foreach (KeyValuePair<KnightChivalryDef, BranchMedalRecord> kv in medalRecords)
        {
            KnightChivalryDef chivalry = kv.Key;
            if (chivalry.medal is null)
            {
                listing_Rect.SubLabel($"({chivalry.label}(×) - {kv.Value})", 0.8f);
            }
            else
            {
                listing_Rect.SubLabel($"({chivalry.medal.medalLabel} - {kv.Value})", 0.8f);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasMedal(KnightChivalryDef chivalry) => medalRecords.ContainsKey(chivalry);

    public int GetMedalCount(KnightChivalryDef chivalry)
    {
        if (chivalry is null || !HasMedal(chivalry))
        {
            return 0;
        }
        if (medalRecords.TryGetValue(chivalry, out BranchMedalRecord record))
        {
            return record.Count;
        }
        return 0;
    }

    public void AdjustMedal(KnightChivalryDef chivalry, int count)
    {
        if (chivalry is null || count == 0)
        {
            return;
        }

        if (medalRecords.TryGetValue(chivalry, out BranchMedalRecord record))
        {
            record.Count += count;
        }
        else if (count > 0)
        {
            record = new BranchMedalRecord()
            {
                Count = count,
                FirstGotTick = Find.TickManager.TicksGame
            };
        }
        else
        {
            // 减少不存在的勋章，直接返回
            return;
        }

        if (record.Count > 0)
        {
            medalRecords[chivalry] = record;
        }
        else
        {
            medalRecords.Remove(chivalry);
            // 如果移除的勋章是主要勋章，则重新指定主要勋章
            if (primaryChivalry == chivalry)
            {
                _ = PrimaryMedal;
            }
        }

        RecacheTotalMedalCount();
        medalHediffsDirty = true;
    }

    /// <summary>
    /// 初始化主要勋章
    /// </summary>
    internal void PostBranchGenerated()
    {
        primaryChivalry = OrderDefDatabase.MedalChivalries.RandomElement();
        AdjustMedal(primaryChivalry, 1);
    }

    internal void PostLoadInit()
    {
        if (medalRecords.RemoveAll(kv => kv.Key.medal is null || kv.Value.Count <= 0) > 0)
        {
            Log.Error($"[OARO] 部分勋章记录在加载后为null或无效，已被移除。");
        }
        RecacheTotalMedalCount();
    }

    private void RecacheTotalMedalCount()
    {
        totalMedalCount = 0;
        foreach (BranchMedalRecord record in medalRecords.Values)
        {
            totalMedalCount += record.Count;
        }
    }

    private void RecacheMedalHediffStage()
    {
        HediffStage stage = new()
        {
            statOffsets = [],
            statFactors = []
        };

        StringBuilder medalLabels = new(32);
        Dictionary<StatDef, float> statOffsetValues = [];
        Dictionary<StatDef, float> statFactorValues = [];
        foreach (KeyValuePair<KnightChivalryDef, BranchMedalRecord> kv in medalRecords)
        {
            if (kv.Key.medal is null || kv.Value.Count <= 0)
            {
                continue;
            }
            try
            {
                KnightChivalryDef chivalry = kv.Key;
                MedalProperties medalProp = chivalry.medal;
                bool isPrimaryMedal = primaryChivalry.IsSameDefNonNullable(chivalry);

                if (!medalProp.statOffsetsByCount.NullOrEmpty())
                {
                    foreach (StatModifierBySeverity modifier in medalProp.statOffsetsByCount)
                    {
                        float value = modifier.valueBySeverity.Evaluate(kv.Value.Count);
                        if (statOffsetValues.TryGetValue(modifier.stat, out float oldValue))
                        {
                            statOffsetValues[modifier.stat] = oldValue + value;

                        }
                        else
                        {
                            statOffsetValues[modifier.stat] = value;
                        }
                    }
                }

                if (!medalProp.statFactorsByCount.NullOrEmpty())
                {
                    foreach (StatModifierBySeverity modifier in medalProp.statFactorsByCount)
                    {
                        float value = modifier.valueBySeverity.Evaluate(kv.Value.Count);
                        if (statFactorValues.TryGetValue(modifier.stat, out float oldValue))
                        {
                            statFactorValues[modifier.stat] = oldValue * value;

                        }
                        else
                        {
                            statFactorValues[modifier.stat] = value;
                        }
                    }
                }

                if (isPrimaryMedal)
                {
                    medalLabels.AppendLine($"{medalProp.MedalLabelCap} (★)".Colorize(chivalry.color));
                }
                else
                {
                    medalLabels.AppendLine(medalProp.MedalLabelCap.Colorize(chivalry.color));
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "processing BuffWorker",
                    typeName: nameof(BranchMedalHandler),
                    methodName: nameof(RecacheMedalHediffStage),
                    needStackTrace: true);
            }
        }

        foreach (KeyValuePair<StatDef, float> kv in statOffsetValues)
        {
            stage.statOffsets.Add(new StatModifier() { stat = kv.Key, value = kv.Value });
        }
        foreach (KeyValuePair<StatDef, float> kv in statFactorValues)
        {
            stage.statFactors.Add(new StatModifier() { stat = kv.Key, value = kv.Value });
        }
        if (medalLabels.Length > 0)
        {
            stage.extraTooltip = medalLabels.ToString();
        }
        medalHediffStage = stage;
    }
}