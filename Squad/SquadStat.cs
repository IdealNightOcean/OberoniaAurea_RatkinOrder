using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadStat : IExposable
{
    public enum SquadMedal : byte
    {
        None,
        Tenacity,
        Courage,
        Intervene,
        Justice
    }
    public static readonly SquadMedal[] SquadMedalArr = (SquadMedal[])Enum.GetValues(typeof(SquadMedal));
    public struct MedalRecord : IExposable
    {
        public SquadMedal type;
        public short count;
        public int firstGotTick;

        public MedalRecord()
        {
            type = SquadMedal.None;
            count = 1;
            firstGotTick = -1;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type", defaultValue: SquadMedal.None);
            Scribe_Values.Look(ref firstGotTick, "firstGotTick", -1);
        }
    }

    private float memberCount; //分队成员数量
    private float commanderCount; //分队骑士长数量
    private float supply; //分队补给数量

    public float memberCeiling = 1f;
    public float commanderCeiling = 1f;
    public float supplyCeiling = 1f;

    private List<MedalRecord> medalRecords = new(4);
    public SquadMedal PrimaryMedal => medalRecords[0].type;
    public int MedalTypeCount => medalRecords.Count;
    public IReadOnlyList<MedalRecord> MedalRecords => medalRecords;

    public float MemberCount
    {
        get => memberCount;
        set => memberCount = Mathf.Clamp(value, 0f, memberCeiling);
    }
    public int MemberCountInt => Mathf.FloorToInt(memberCount); //分队成员数量（整数）

    public float CommanderCount
    {
        get => commanderCount;
        set => commanderCount = Mathf.Clamp(value, 0f, commanderCeiling);
    }
    public int CommanderCountInt => Mathf.FloorToInt(commanderCount); //分队骑士长数量（整数）

    public float AllCrewCount => memberCount + commanderCount;
    public int AllCrewCountInt => Mathf.FloorToInt(memberCount + commanderCount);

    public float Supply
    {
        get => supply;
        set => supply = Mathf.Clamp(value, 0f, supplyCeiling);
    }


    public float MemberPercentage
    {
        get
        {
            if (memberCeiling <= 0f)
            {
                return 1f;
            }
            return memberCount / memberCeiling;
        }
    }

    public float CommanderPercentage
    {
        get
        {
            if (commanderCeiling <= 0f)
            {
                return 1f;
            }
            return commanderCount / commanderCeiling;
        }
    }

    public SquadStat(bool initConstruct)
    {
        if (initConstruct)
        {
            SquadMedal primaryMedal = SquadMedalArr[Rand.Range(1, SquadMedalArr.Length)];
            medalRecords.Add(new MedalRecord()
            {
                type = primaryMedal,
                firstGotTick = 0,
                count = 1
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateCeiling(Squad squad, bool updateStatCache)
    {
        Branch branch = squad.Branch;
        memberCeiling = BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_SquadMemberCeiling, immediateUpdate: updateStatCache);
        commanderCeiling = BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_SquadCommanderCeiling, immediateUpdate: updateStatCache);
        supplyCeiling = BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_SquadSupplyCeiling, immediateUpdate: updateStatCache);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasMedal(SquadMedal medal)
    {
        return GetMedalCount(medal) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short GetMedalCount(SquadMedal medal)
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

    public void AddMedal(SquadMedal medal, short count = 1)
    {
        if (medal == SquadMedal.None)
        {
            return;
        }

        for (int i = 0; i < medalRecords.Count; i++)
        {
            MedalRecord record = medalRecords[i];
            if (record.type == medal)
            {
                record.count += count;
                medalRecords[i] = record;
                return;
            }
        }
        medalRecords.Add(new MedalRecord()
        {
            type = medal,
            count = count,
            firstGotTick = Find.TickManager.TicksGame
        });
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref memberCount, "memberCount", 0f);
        Scribe_Values.Look(ref commanderCount, "commanderCount", 0f);
        Scribe_Values.Look(ref supply, "supply", 0f);

        Scribe_Values.Look(ref memberCeiling, "memberCeiling", 0f);
        Scribe_Values.Look(ref commanderCeiling, "commanderCeiling", 0f);
        Scribe_Values.Look(ref supplyCeiling, "supplyCeiling", 0f);

        Scribe_Collections.Look(ref medalRecords, "medalRecords", LookMode.Deep);
    }
}
