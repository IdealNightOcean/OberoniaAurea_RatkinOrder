using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchMedalHandler : IExposable
{
    private Dictionary<BranchMedalDef, BranchMedalRecord> medalRecords = new(4);

    private BranchMedalDef primaryMedal;
    public BranchMedalDef PrimaryMedal
    {
        get
        {
            if (primaryMedal is null)
            {
                if (!medalRecords.NullOrEmpty())
                {
                    primaryMedal = medalRecords.First().Key;
                }
            }
            return primaryMedal;
        }
    }

    public BranchTaskType FocusedTaskType => PrimaryMedal?.focusedTaskType ?? BranchTaskType.General;

    public int MedalTypeCount => medalRecords.Count;
    public IReadOnlyDictionary<BranchMedalDef, BranchMedalRecord> MedalRecords => medalRecords;

    private int totalMedalCount;
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
        Scribe_Values.Look(ref totalMedalCount, "totalMedalCount", 0);
        Scribe_Defs.Look(ref primaryMedal, "primaryMedal");
        Scribe_Collections.Look(ref medalRecords, "medalRecords", LookMode.Def, LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"PrimaryMedal: {PrimaryMedal}");
        listing_Rect.Label("Medals:");
        foreach (var kv in medalRecords)
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

    public void AddMedal(BranchMedalDef medal, short count = 1)
    {
        if (medal is null)
        {
            return;
        }

        if (medalRecords.TryGetValue(medal, out BranchMedalRecord record))
        {
            record.Count++;
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
        medalRecords.RemoveAll(kv => kv.Key is null || !kv.Value.Validate());
        totalMedalCount = medalRecords.Sum(kv => kv.Value.Count);
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
                Log.Error($"An exception occurred while processing BuffWorker in {nameof(BranchMedalHandler)}.{nameof(RecacheMedalHediffStage)}.\nException:\n{ex.Message}");
            }
        }

        if (medalLabels.Length > 0)
        {
            stage.extraTooltip = medalLabels.ToString();
        }
        medalHediffStage = stage;
    }
}