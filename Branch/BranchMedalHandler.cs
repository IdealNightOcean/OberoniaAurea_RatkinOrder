using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchMedalRecord;

namespace OberoniaAurea.RatkinOrder;

public class BranchMedalHandler : IExposable
{
    private List<BranchMedalRecord> medalRecords = new(4);

    [Unsaved] private BranchMedalType allHasTypes = 0;
    public BranchMedalType AllHasTypes => allHasTypes;
    public BranchMedalType PrimaryMedal => medalRecords[0].Type;
    public int MedalTypeCount => medalRecords.Count;
    public IReadOnlyList<BranchMedalRecord> MedalRecords => medalRecords;

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
        Scribe_Collections.Look(ref medalRecords, "medalRecords", LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"PrimaryMedal: {PrimaryMedal}");
        listing_Rect.Label("Medals:");
        foreach (BranchMedalRecord mr in medalRecords)
        {
            listing_Rect.SubLabel($"({mr.Type}, {mr.Count})", 0.8f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasMedal(BranchMedalType medal) => (allHasTypes & medal) != 0;

    public int GetMedalCount(BranchMedalType medal)
    {
        if (medal == BranchMedalType.None || !HasMedal(medal))
        {
            return 0;
        }
        for (int i = 0; i < medalRecords.Count; i++)
        {
            if (medalRecords[i].Type == medal)
            {
                return medalRecords[i].Count;
            }
        }
        return 0;
    }

    public void AddMedal(BranchMedalType medal, short count = 1)
    {
        if (medal == BranchMedalType.None)
        {
            return;
        }

        if (HasMedal(medal))
        {
            for (int i = 0; i < medalRecords.Count; i++)
            {
                BranchMedalRecord record = medalRecords[i];
                if (record.Type == medal)
                {
                    record.Count += count;
                    medalRecords[i] = record;
                    totalMedalCount += count;
                }
            }
        }
        else
        {
            medalRecords.Add(new BranchMedalRecord()
            {
                Type = medal,
                Count = count,
                FirstGotTick = Find.TickManager.TicksGame
            });

            medalRecords.SortBy(r => (int)r.Type);
            allHasTypes |= medal;
        }

        totalMedalCount += count;
        medalHediffsDirty = true;
    }

    /// <summary>
    /// 初始化主要勋章
    /// </summary>
    internal void PostBranchGenerated()
    {
        BranchMedalType primaryMedal = BranchUtility.BranchMedalsArr[Rand.Range(1, BranchUtility.BranchMedalsArr.Length)];
        AddMedal(primaryMedal, 1);
    }

    internal void PostLoadInit()
    {
        medalRecords.RemoveAll(r => !r.Validate());
        totalMedalCount = 0;
        for (int i = 0; i < medalRecords.Count; i++)
        {
            allHasTypes |= medalRecords[i].Type;
            totalMedalCount += medalRecords[i].Count;
        }
    }

    private void RecacheMedalHediffStage()
    {
        HediffStage stage = new()
        {
            extraTooltip = string.Empty
        };
        for (int i = 0; i < medalRecords.Count; i++)
        {
            AdjustMedalHediffStage(stage, medalRecords[i].Type, isPrimary: i == 0);
        }
        if (stage.extraTooltip.NullOrEmpty())
        {
            stage.extraTooltip = null;
        }
        medalHediffStage = stage;

        static void AdjustMedalHediffStage(HediffStage stage, BranchMedalType medalType, bool isPrimary)
        {
            Color color = Color.white;
            switch (medalType)
            {
                case BranchMedalType.Tenacity:
                    {
                        stage.painFactor *= (isPrimary ? 0.85f : 0.95f);
                        color = Color.yellow;
                        break;
                    }
                case BranchMedalType.Courage:
                    {
                        stage.statOffsets ??= [];
                        stage.statOffsets.Add(new StatModifier()
                        {
                            stat = StatDefOf.MeleeHitChance,
                            value = isPrimary ? 4f : 2f
                        });
                        color = ColorLibrary.RedReadable;
                        break;
                    }
                case BranchMedalType.Rescue:
                    {
                        stage.statOffsets ??= [];
                        stage.statOffsets.Add(new StatModifier()
                        {
                            stat = StatDefOf.MedicalTendSpeed,
                            value = isPrimary ? 0.12f : 0.06f
                        });
                        color = Color.cyan;
                        break;
                    }
                case BranchMedalType.Justice:
                    {
                        stage.statOffsets ??= [];
                        stage.statOffsets.Add(new StatModifier()
                        {
                            stat = StatDefOf.MoveSpeed,
                            value = isPrimary ? 0.15f : 0.10f
                        });
                        stage.statOffsets.Add(new StatModifier()
                        {
                            stat = StatDefOf.WorkSpeedGlobal,
                            value = isPrimary ? 0.05f : 0.03f
                        });
                        color = ColorLibrary.Orange;
                        break;
                    }
                default: return; //无对应则不增加描述
            }

            if (isPrimary)
            {
                stage.extraTooltip += $"{("OARO_BranchMedalType_" + medalType.ToString()).Translate().Colorize(color)} {"★".Colorize(color)}\n";
            }
            else
            {
                stage.extraTooltip += $"OARO_BranchMedalType_{medalType}".Translate().Colorize(color) + "\n";
            }
        }
    }
}