using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightsManager : IExposable, IOnBranchDestroyed
{
    public static ResidentKnightsManager Instance { get; private set; }

    public static int ResidentKnightCeiling
    {
        get
        {
            int ceiling = 1;
            ceiling += OrderHallHandler.Instance.OrderHallLevel switch
            {
                < 3 => 0,
                < 5 => 3,
                < 6 => 5,
                _ => 7
            };
            return ceiling;
        }
    }

    private Dictionary<Pawn, ResidentKnightRecord> residentKnights = [];
    private Dictionary<ResidentKnightRoleDef, ResidentKnightRecord> rolesToKnights = [];

    public int KnightsCount => residentKnights.Count;
    public IReadOnlyDictionary<Pawn, ResidentKnightRecord> ResidentKnights => residentKnights;
    public IReadOnlyDictionary<ResidentKnightRoleDef, ResidentKnightRecord> RolesToKnights => RolesToKnights;

    [Unsaved] private LazyMutable<KnightPersonality> allHasPersonalityTypes;
    public KnightPersonality AllHasPersonalityTypes => allHasPersonalityTypes.Value;

    [Unsaved] private LazyMutable<int> instructorKnightsCount;
    public int InstructorKnightsCount => instructorKnightsCount.Value;

    [Unsaved] private readonly Dictionary<StatDef, float> statOffsets = [];
    [Unsaved] private readonly Dictionary<StatDef, float> statFactors = [];
    [Unsaved] private HediffStage buffHediffStage;
    [Unsaved] private int nextBuffStatRegainTick = -1;

    public HediffStage BuffHediffStage
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
    private List<ResidentKnightRecord> rolesToKnightValues;

    internal ResidentKnightsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AcceptedBranchDemandHandler));
        Instance = this;

        buffHediffStage = new HediffStage();
        allHasPersonalityTypes = new(() => residentKnights.Values.Aggregate(KnightPersonality.None, (acc, rk) => acc | (rk?.Personality ?? KnightPersonality.None)));
        instructorKnightsCount = new(() => residentKnights.Values.Where(rk => rk?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_Instructor).Count());
    }
    public static void ClearStaticCache() => Instance = null;

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

        if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            residentKnightKeys = null;
            residentKnightValues = null;
            rolesToKnightKeys = null;
            rolesToKnightValues = null;
        }

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (residentKnights.RemoveAll(kv => kv.Key is null || kv.Value is null) > 0)
            {
                Log.Error("[OARO] Some resident knight records were removed due to null keys or values during loading.");
            }
            if (rolesToKnights.RemoveAll(kv => kv.Key is null || kv.Value is null) > 0)
            {
                Log.Error("[OARO] Some role to knight records were removed due to null keys or values during loading.");
            }

            nextBuffStatRegainTick = -1;
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentKnight(Pawn pawn) => residentKnights.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out ResidentKnightRecord record) => residentKnights.TryGetValue(pawn, out record);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightOfRole(ResidentKnightRoleDef roleDef, out ResidentKnightRecord record) => rolesToKnights.TryGetValue(roleDef, out record);

    public void TickDay()
    {
        foreach (ResidentKnightRecord record in residentKnights.Values)
        {
            if (record.IsValid)
            {
                float gainPoints = record.Knight.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationDailyGain);
                record.MeditationPoints += gainPoints;
            }
        }
    }

    private void RemoveAllInvalidRecord(Predicate<ResidentKnightRecord> extraRemove = null)
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
        OnKnightChanged();

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

    public void Notify_MercyQuestSucceed()
    {
        float gainPoints = 0f;
        foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
        {
            gainPoints = 200f * kv.Key.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
            kv.Value.MeditationPoints += gainPoints;
        }
    }

    public void AddResidentKnight(Pawn pawn, Branch branch)
    {
        if (!residentKnights.ContainsKey(pawn))
        {
            residentKnights.Add(pawn, new ResidentKnightRecord(pawn, branch));
            OnKnightChanged();
        }
    }

    public void RemoveResidentKnight(Pawn pawn)
    {
        if (pawn is null || !residentKnights.TryGetValue(pawn, out ResidentKnightRecord record))
        {
            return;
        }
        residentKnights.Remove(pawn);
        OnKnightChanged();
        if (record.CurRole is not null)
        {
            rolesToKnights.Remove(record.CurRole);
            nextBuffStatRegainTick = -1;
        }
    }

    public bool TrySetResidentKnight(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (SetResidentKnight(pawn, roleDef, replaceCurRole))
        {
            nextBuffStatRegainTick = -1;
            return true;
        }
        return false;
    }

    private bool SetResidentKnight(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (!residentKnights.TryGetValue(pawn, out ResidentKnightRecord pawnRecord))
        {
            return false;
        }

        if (rolesToKnights.TryGetValue(roleDef, out ResidentKnightRecord curRolePawnRecord))
        {
            if (curRolePawnRecord.Knight == pawn)
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
        if (curRolePawnRecord is null && pOldRole is null)
        {
            pawnRecord.CurRole = roleDef;
            rolesToKnights[roleDef] = pawnRecord;
            return true;
        }

        //两人交接职位
        if (curRolePawnRecord is not null && pOldRole is null)
        {
            curRolePawnRecord.CurRole = null;
            pawnRecord.CurRole = roleDef;
            rolesToKnights[roleDef] = pawnRecord;
            return true;
        }

        //本人职位改变
        if (curRolePawnRecord is null && pOldRole is not null)
        {
            pawnRecord.CurRole = roleDef;
            rolesToKnights.Remove(pOldRole);
            return true;
        }

        //双方交换职位
        if (curRolePawnRecord is not null && pOldRole is not null)
        {
            curRolePawnRecord.CurRole = pOldRole;
            rolesToKnights[pOldRole] = curRolePawnRecord;

            pawnRecord.CurRole = roleDef;
            rolesToKnights[roleDef] = pawnRecord;
            return true;
        }

        return false;
    }

    private void OnKnightChanged()
    {
        allHasPersonalityTypes.MarkDirty();
        instructorKnightsCount.MarkDirty();
    }

    private void RegainRoleBuffStat()
    {
        statOffsets.Clear();
        statFactors.Clear();
        nextBuffStatRegainTick = Find.TickManager.TicksGame + 60000;

        foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnightRecord> kv in rolesToKnights)
        {
            (ResidentKnightRoleDef roldDef, Pawn pawn) = (kv.Key, kv.Value.Knight);

            AddStatModifier(roldDef.statOffsets, isFactor: false);
            AddStatModifier(roldDef.RoleWorker.RoleStatOffsets(pawn), isFactor: false);

            AddStatModifier(roldDef.statFactors, isFactor: true);
            AddStatModifier(roldDef.RoleWorker.RoleStatFactors(pawn), isFactor: true);
        }

        buffHediffStage.statOffsets = statOffsets.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();
        buffHediffStage.statFactors = statFactors.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();

        void AddStatModifier(IEnumerable<StatModifier> modifiers, bool isFactor)
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