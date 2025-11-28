using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchMedalHandler : IExposable
{
    private Dictionary<BranchMedalDef, BranchMedalRecord> medalRecords = new(4);

    private BranchMedalDef primaryMedal;
    public BranchMedalDef PrimaryMedal => primaryMedal ??= medalRecords?.FirstOrFallback().Key;

    public BranchTaskType ProtogenicTaskType => PrimaryMedal?.focusedTaskType ?? BranchTaskType.General;

    public int MedalTypeCount => medalRecords.Count;
    public IReadOnlyDictionary<BranchMedalDef, BranchMedalRecord> MedalRecords => medalRecords;

    [Unsaved] private int totalMedalCount = -1;
    public int TotalMedalCount
    {
        get
        {
            if (totalMedalCount < 0)
            {
                totalMedalCount = Mathf.Max(0, medalRecords.Sum(kv => kv.Value.Count));
            }
            return totalMedalCount;
        }
        private set { totalMedalCount = value; }
    }

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
        Scribe_Defs.Look(ref primaryMedal, "primaryMedal");
        Scribe_Collections.Look(ref medalRecords, "medalRecords", LookMode.Def, LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"主印记: {PrimaryMedal}");
        listing_Rect.Label("所有印记:");
        foreach (KeyValuePair<BranchMedalDef, BranchMedalRecord> kv in medalRecords)
        {
            listing_Rect.SubLabel($"({kv.Key.label} - {kv.Value})", 0.8f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasMedal(BranchMedalDef medal) => medalRecords.ContainsKey(medal);

    public int GetMedalCount(BranchMedalDef medal)
    {
        if (medal is null || !HasMedal(medal))
        {
            return 0;
        }
        if (medalRecords.TryGetValue(medal, out BranchMedalRecord record))
        {
            return record.Count;
        }
        return 0;
    }

    public void AddMedal(BranchMedalDef medal, int count = 1)
    {
        if (medal is null)
        {
            return;
        }

        if (medalRecords.TryGetValue(medal, out BranchMedalRecord record))
        {
            record.Count += count;
        }
        else
        {
            record = new BranchMedalRecord()
            {
                Count = count,
                FirstGotTick = Find.TickManager.TicksGame
            };
        }

        medalRecords[medal] = record;
        totalMedalCount += count;
        medalHediffsDirty = true;
    }

    /// <summary>
    /// 初始化主要勋章
    /// </summary>
    internal void PostBranchGenerated()
    {
        primaryMedal = DefDatabase<BranchMedalDef>.AllDefsListForReading.RandomElement();
        AddMedal(primaryMedal, 1);
    }

    internal void PostLoadInit()
    {
        if (medalRecords.Remove(null) | (medalRecords.RemoveAll(kv => !kv.Value.Validate()) > 0))
        {
            Log.Error($"[OARO] Some Medal Records of were null or invalid after loading and have been removed.");
        }
        totalMedalCount = -1;
    }

    private void RecacheMedalHediffStage()
    {
        HediffStage stage = new()
        {
            statOffsets = [],
            statFactors = []
        };

        StringBuilder medalLabels = new(32);
        foreach (KeyValuePair<BranchMedalDef, BranchMedalRecord> kv in medalRecords)
        {
            try
            {
                BranchMedalDef medalDef = kv.Key;
                bool isPrimaryMedal = primaryMedal == medalDef;
                medalDef.BuffWorker.AdjuestHediffBuffStage(stage, isPrimaryMedal, kv.Value.Count);
                if (isPrimaryMedal)
                {
                    medalLabels.AppendLine($"{medalDef.LabelCap} (★)".Colorize(medalDef.color));
                }
                else
                {
                    medalLabels.AppendLine(medalDef.LabelCap.Colorize(medalDef.color));
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

        if (medalLabels.Length > 0)
        {
            stage.extraTooltip = medalLabels.ToString();
        }
        medalHediffStage = stage;
    }
}