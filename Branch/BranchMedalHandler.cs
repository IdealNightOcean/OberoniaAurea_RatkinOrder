using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchMedalHandler : IExposable, IPostLoadInit, IDrawDevWindow
{
    private List<BranchMedalRecord> medalRecords = new(4);

    public BranchMedalType PrimaryMedal => medalRecords[0].type;
    public int MedalTypeCount => medalRecords.Count;
    public IReadOnlyList<BranchMedalRecord> MedalRecords => medalRecords;

    private int totalMedalCount;
    public int TotalMedalCount => totalMedalCount;

    [Unsaved] private List<(HediffDef, float)> medalHediffs;
    [Unsaved] private bool medalHediffsDirty = true;
    public IReadOnlyList<(HediffDef, float)> MedalHediffs
    {
        get
        {
            if (medalHediffsDirty)
            {
                RecacheMedalHediff();
            }
            return medalHediffs;
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
    public bool HasMedal(BranchMedalType medal) => GetMedalCount(medal) > 0;

    public int GetMedalCount(BranchMedalType medal)
    {
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

        for (int i = 0; i < medalRecords.Count; i++)
        {
            BranchMedalRecord record = medalRecords[i];
            if (record.type == medal)
            {
                record.count += count;
                medalRecords[i] = record;
                totalMedalCount += count;
                return;
            }
        }

        medalRecords.Add(new BranchMedalRecord()
        {
            type = medal,
            count = count,
            firstGotTick = Find.TickManager.TicksGame
        });

        totalMedalCount += count;
        medalHediffsDirty = true;
    }

    /// <summary>
    /// 初始化主要勋章
    /// </summary>
    public void PostBranchGenerated()
    {
        BranchMedalType primaryMedal = BranchUtility.BranchMedalsArr[Rand.Range(1, BranchUtility.BranchMedalsArr.Length)];
        medalRecords.Add(new BranchMedalRecord()
        {
            type = primaryMedal,
            firstGotTick = 0,
            count = 1
        });
        totalMedalCount = 1;
        medalHediffsDirty = true;
    }

    public void PostLoadInit()
    {
        medalRecords.RemoveAll(r => !BranchMedalRecord.Validate(r));
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
            totalMedalCount += medalRecords[i].count;
        }
    }

    private void RecacheMedalHediff()
    {
        medalHediffs ??= [];
        medalHediffs.Clear();

        HediffDef gainHediff = GetMedalHediffDef(medalRecords[0].type);
        if (gainHediff is not null)
        {
            medalHediffs.Add((gainHediff, 1.5f));
        }
        for (int i = 1; i < medalRecords.Count; i++)
        {
            gainHediff = GetMedalHediffDef(medalRecords[i].type);
            if (gainHediff is not null)
            {
                medalHediffs.Add((gainHediff, 0.5f));
            }
        }
        medalHediffsDirty = false;

        static HediffDef GetMedalHediffDef(BranchMedalType medalType)
        {
            return medalType switch
            {
                BranchMedalType.Tenacity => HediffDefOf.CubeRage,
                BranchMedalType.Courage => HediffDefOf.CubeRage,
                BranchMedalType.Intervene => HediffDefOf.CubeRage,
                BranchMedalType.Justice => HediffDefOf.CubeRage,
                _ => null,
            };
        }
    }
}