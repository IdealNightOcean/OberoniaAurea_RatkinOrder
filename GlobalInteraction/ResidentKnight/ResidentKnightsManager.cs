using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightsManager : IExposable, IOnBranchDestroyed
{
    public static int ResidentKnightCeiling
    {
        get
        {
            int ceiling = 1;
            ceiling += OrderHallHandler.OrderHallLevel switch
            {
                < 3 => 0,
                < 5 => 3,
                < 6 => 5,
                _ => 7
            };
            return ceiling;
        }
    }

    private static Dictionary<Pawn, ResidentKnightRecord> residentKnights = [];
    private static Dictionary<ResidentKnightRoleDef, Pawn> rolesToKnights = [];

    public static int KnightsCount => residentKnights.Count;
    public static IReadOnlyDictionary<Pawn, ResidentKnightRecord> ResidentKnights => residentKnights;
    public static IReadOnlyDictionary<ResidentKnightRoleDef, Pawn> RolesToKnights => RolesToKnights;

    [Unsaved] private static readonly Dictionary<StatDef, float> statOffsets = [];
    [Unsaved] private static readonly Dictionary<StatDef, float> statFactors = [];

    [Unsaved] private static HediffStage buffHediffStage;
    [Unsaved] private static int nextBuffStatRegainTick = -1;

    public static HediffStage BuffHediffStage
    {
        get
        {
            if (Find.TickManager.TicksGame > nextBuffStatRegainTick)
            {
                RegainRoleBuffStat();
            }
            return buffHediffStage;
        }
    }

    private List<Pawn> residentKnightKeys;
    private List<ResidentKnightRecord> residentKnightValues;
    private List<ResidentKnightRoleDef> rolesToKnightKeys;
    private List<Pawn> rolesToKnightValues;

    public ResidentKnightsManager()
    {
        ResetStaticValue();
        buffHediffStage = new HediffStage();
    }

    public static void ResetStaticValue()
    {
        residentKnights.Clear();
        rolesToKnights.Clear();
        statOffsets.Clear();
        statFactors.Clear();
        buffHediffStage = null;
    }

    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            residentKnightKeys = residentKnights.Keys.ToList();
            residentKnightValues = residentKnights.Values.ToList();
            rolesToKnightKeys = rolesToKnights.Keys.ToList();
            rolesToKnightValues = rolesToKnights.Values.ToList();
        }

        Scribe_Collections.Look(ref residentKnights, "residentKnights", LookMode.Reference, LookMode.Deep, ref residentKnightKeys, ref residentKnightValues);
        Scribe_Collections.Look(ref rolesToKnights, "rolesToKnights", LookMode.Def, LookMode.Reference, ref rolesToKnightKeys, ref rolesToKnightValues);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            rolesToKnights.RemoveAll(kv => kv.Key is null || kv.Value is null);
            nextBuffStatRegainTick = -1;

            residentKnightKeys = null;
            residentKnightValues = null;
            rolesToKnightKeys = null;
            rolesToKnightValues = null;
        }
    }

    public static void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label("ResidentKnights:");
        if (residentKnights.NullOrEmpty())
        {
            listing_Rect.SubLabel("None", widthPct: 0.8f);
        }
        else
        {
            foreach (var kv in residentKnights)
            {
                listing_Rect.SubLabel(kv.Key.Name + ": " + kv.Value.ToString(), widthPct: 0.8f);
            }
        }
    }

    public static bool IsResidentKnight(Pawn pawn) => residentKnights.ContainsKey(pawn);

    public static bool TryGetKnightRecord(Pawn pawn, out ResidentKnightRecord record) => residentKnights.TryGetValue(pawn, out record);

    public static bool TryGetKnightOfRole(ResidentKnightRoleDef roleDef, out Pawn knight) => rolesToKnights.TryGetValue(roleDef, out knight);

    public static void TickDay()
    {
        float gainPoints = 0f;
        foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
        {
            gainPoints = kv.Key.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationBase) * kv.Key.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
            kv.Value.MeditationPoints += gainPoints;
        }
    }


    private static void RemoveAllInvalidRecord(Predicate<ResidentKnightRecord> extraRemove = null)
    {
        HashSet<Pawn> pawnsToRemove = [];
        HashSet<ResidentKnightRoleDef> rolesToRemove = [];

        if (extraRemove is null)
        {
            foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
            {
                (Pawn knight, ResidentKnightRecord record) = kv;
                if (knight is null || record is null || record.Branch is null)
                {
                    pawnsToRemove.Add(knight);
                    if (record?.CurRole is not null)
                    {
                        rolesToRemove.Add(record.CurRole);
                    }
                }
            }
        }
        else
        {
            foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
            {
                (Pawn knight, ResidentKnightRecord record) = kv;
                if (knight is null || record is null || record.Branch is null || extraRemove.Invoke(record))
                {
                    pawnsToRemove.Add(knight);
                    if (record?.CurRole is not null)
                    {
                        rolesToRemove.Add(record.CurRole);
                    }
                }
            }
        }
        if (rolesToKnights.RemoveAll(kv => kv.Key is null || kv.Value is null) > 0)
        {
            nextBuffStatRegainTick = -1;
        }

        if (pawnsToRemove.Count <= 0)
        {
            return;
        }
        foreach (Pawn p in pawnsToRemove)
        {
            residentKnights.Remove(p);
        }

        if (rolesToRemove.Count > 0)
        {
            foreach (ResidentKnightRoleDef role in rolesToRemove)
            {
                rolesToKnights.Remove(role);
            }

            nextBuffStatRegainTick = -1;
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder) => RemoveAllInvalidRecord((record) => record.Branch.RatkinOrder == ratkinOrder);
    public void Notify_BranchDestroyed(Branch branch) => RemoveAllInvalidRecord((record) => record.Branch == branch);

    public static void Notify_MercyQuestSucceed()
    {
        float gainPoints = 0f;
        foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
        {
            gainPoints = 200f * kv.Key.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
            kv.Value.MeditationPoints += gainPoints;
        }
    }

    public static void AddResidentKnight(Pawn pawn, KnightRecord knightRecord, ResidentKnightAcademicDef genealAcademicDef = null)
    {
        if (!residentKnights.ContainsKey(pawn))
        {
            residentKnights.Add(pawn, new ResidentKnightRecord(knightRecord, genealAcademicDef));
        }
    }

    public static void RemoveResidentKnight(Pawn pawn)
    {
        if (pawn is null || !residentKnights.TryGetValue(pawn, out ResidentKnightRecord record))
        {
            return;
        }
        residentKnights.Remove(pawn);
        if (record.CurRole is not null)
        {
            rolesToKnights.Remove(record.CurRole);
            nextBuffStatRegainTick = -1;
        }
    }

    public static bool TrySetResidentKnight(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (SetResidentKnight(pawn, roleDef, replaceCurRole))
        {
            nextBuffStatRegainTick = -1;
            return true;
        }
        return false;
    }

    private static bool SetResidentKnight(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (!residentKnights.TryGetValue(pawn, out ResidentKnightRecord pawnRecord))
        {
            return false;
        }

        ResidentKnightRecord curRolePawnRecord = null;
        if (rolesToKnights.TryGetValue(roleDef, out Pawn curRolePawn))
        {
            if (curRolePawn == pawn)
            {
                return true;
            }
            if (!replaceCurRole)
            {
                return false;
            }
        }

        ResidentKnightRoleDef pOldRole = pawnRecord.CurRole;

        //新增职位
        if (curRolePawn is null && pOldRole is null)
        {
            pawnRecord.CurRole = roleDef;
            rolesToKnights[roleDef] = pawn;
            return true;
        }

        //两人交接职位
        if (curRolePawn is not null && pOldRole is null)
        {
            curRolePawnRecord.CurRole = null;
            pawnRecord.CurRole = roleDef;
            rolesToKnights[roleDef] = pawn;
            return true;
        }

        //本人职位改变
        if (curRolePawn is null && pOldRole is not null)
        {
            pawnRecord.CurRole = roleDef;
            rolesToKnights.Remove(pOldRole);
            return true;
        }

        //双方交换职位
        if (curRolePawn is not null && pOldRole is not null)
        {
            curRolePawnRecord.CurRole = pOldRole;
            pawnRecord.CurRole = roleDef;
            rolesToKnights[roleDef] = pawn;
            return true;
        }

        return false;
    }

    private static void RegainRoleBuffStat()
    {
        statOffsets.Clear();
        statFactors.Clear();
        nextBuffStatRegainTick = Find.TickManager.TicksGame + 60000;

        foreach (KeyValuePair<ResidentKnightRoleDef, Pawn> rolePawn in rolesToKnights)
        {
            (ResidentKnightRoleDef roldDef, Pawn pawn) = rolePawn;
            AddStatModifier(roldDef.statOffsets, isFactor: false);
            AddStatModifier(roldDef.RoleWorker.RoleStatOffsets(pawn), isFactor: false);

            AddStatModifier(roldDef.statFactors, isFactor: true);
            AddStatModifier(roldDef.RoleWorker.RoleStatFactors(pawn), isFactor: true);
        }

        buffHediffStage.statOffsets = statOffsets.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();
        buffHediffStage.statFactors = statFactors.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();

        static void AddStatModifier(IEnumerable<StatModifier> modifiers, bool isFactor)
        {
            if (modifiers is null)
            {
                return;
            }
            Dictionary<StatDef, float> target = isFactor ? statFactors : statOffsets;

            foreach (StatModifier modifier in modifiers)
            {
                if (target.TryGetValue(modifier.stat, out float curValue))
                {
                    if (isFactor)
                    {
                        target[modifier.stat] = curValue * modifier.value;
                    }
                    else
                    {
                        target[modifier.stat] = curValue + modifier.value;
                    }

                }
                else
                {
                    target[modifier.stat] = modifier.value;
                }
            }
        }
    }
}