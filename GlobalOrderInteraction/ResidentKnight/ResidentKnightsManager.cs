using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightsManager : IExposable, IOnBranchDestroyed
{

    private Dictionary<Pawn, ResidentKnight> residentKnights = [];
    private Dictionary<ResidentKnightRoleDef, Pawn> rolesToKnights = [];

    public IReadOnlyDictionary<Pawn, ResidentKnight> ResidentKnights => residentKnights;
    public IReadOnlyDictionary<ResidentKnightRoleDef, Pawn> RolesToKnights => RolesToKnights;


    [Unsaved] private readonly Dictionary<StatDef, float> statOffsets = [];
    [Unsaved] private readonly Dictionary<StatDef, float> statFactors = [];

    [Unsaved] private readonly HediffStage buffHediffStage = new();
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

    public void ExposeData()
    {
        Scribe_Collections.Look(ref residentKnights, "residentKnights", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {

            rolesToKnights.RemoveAll(kv => kv.Key is null || kv.Value is null);
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

    public bool IsResidentKnight(Pawn pawn) => residentKnights.ContainsKey(pawn);

    public bool TryGetKnightRecord(Pawn pawn, out ResidentKnight record) => residentKnights.TryGetValue(pawn, out record);

    public bool TryGetKnightOfRole(ResidentKnightRoleDef roleDef, out Pawn knight) => rolesToKnights.TryGetValue(roleDef, out knight);


    private void RemoveAllInvalidRecord(Predicate<ResidentKnight> extraRemove = null)
    {
        HashSet<Pawn> pawnsToRemove = [];
        HashSet<ResidentKnightRoleDef> rolesToRemove = [];

        if (extraRemove is null)
        {
            foreach (KeyValuePair<Pawn, ResidentKnight> kv in residentKnights)
            {
                (Pawn knight, ResidentKnight record) = kv;
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
            foreach (KeyValuePair<Pawn, ResidentKnight> kv in residentKnights)
            {
                (Pawn knight, ResidentKnight record) = kv;
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

    public void AddResidentKnight(Pawn pawn, KnightRecord knightRecord)
    {
        if (!residentKnights.ContainsKey(pawn))
        {
            residentKnights.Add(pawn, new ResidentKnight(knightRecord.Branch));
        }
    }

    public void RemoveResidentKnight(Pawn pawn)
    {
        if (pawn is null || !residentKnights.TryGetValue(pawn, out ResidentKnight record))
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

    public bool SetResidentKnight(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (!residentKnights.TryGetValue(pawn, out ResidentKnight pawnRecord))
        {
            return false;
        }

        ResidentKnight curRolePawnRecord = null;
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

        nextBuffStatRegainTick = -1;
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

    private void RegainRoleBuffStat()
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