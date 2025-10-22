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

    [Unsaved] private BranchMedalType allHasTypes = BranchMedalType.None;
    public BranchMedalType AllHasTypes => allHasTypes;
    public BranchMedalType PrimaryMedal => medalRecords[0].type;
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
            listing_Rect.SubLabel($"({mr.type}, {mr.count})", 0.8f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasMedal(BranchMedalType medal) => (allHasTypes & medal) == medal;

    public int GetMedalCount(BranchMedalType medal)
    {
        if (medal == BranchMedalType.None || !HasMedal(medal))
        {
            return 0;
        }
        for (int i = 0; i < medalRecords.Count; i++)
        {
            if (medalRecords[i].type == medal)
            {
                return medalRecords[i].count;
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
                if (record.type == medal)
                {
                    record.count += count;
                    medalRecords[i] = record;
                    totalMedalCount += count;
                }
            }
        }
        else
        {
            medalRecords.Add(new BranchMedalRecord()
            {
                type = medal,
                count = count,
                firstGotTick = Find.TickManager.TicksGame
            });

            medalRecords.SortBy(r => (int)r.type);
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
        if (totalMedalCount <= 0)
        {
            RecacheMedalsCount();
        }
    }

    private void RecacheMedalsCount()
    {
        totalMedalCount = 0;
        for (int i = 1; i < medalRecords.Count; i++)
        {
            allHasTypes |= medalRecords[i].type;
            totalMedalCount += medalRecords[i].count;
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
            AdjustMedalHediffStage(stage, medalRecords[i].type, isPrimary: i == 0);
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