using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightsManager : IExposable, IOnRatkinOrderRemoved
{
    private List<ResidentKnight> residentKnights = [];
    public IReadOnlyList<ResidentKnight> ResidentKnights => residentKnights;

    private int residentLimit;
    public int ResidentLimit
    {
        get
        {
            return residentLimit + OrderInteractionUtility.ExtraResidentKnightLimit_OrderHallLevel;
        }
        set
        {
            residentLimit = Mathf.Max(0, value - OrderInteractionUtility.ExtraResidentKnightLimit_OrderHallLevel);
        }
    }

    public IEnumerable<Pawn> NoRoleKnights
    {
        get
        {
            foreach (ResidentKnight knight in residentKnights)
            {
                if (!knight.IsActive)
                {
                    yield return knight.Pawn;
                }
            }
        }
    }


    [Unsaved] private readonly Dictionary<StatDef, float> statOffsets;
    [Unsaved] private readonly Dictionary<StatDef, float> statFactors;

    [Unsaved] private readonly HediffStage buffHediffStage;
    [Unsaved] private bool buffStageDirty;

    public HediffStage BuffHediffStage
    {
        get
        {
            if (buffStageDirty)
            {
                EstablishBuffStageStatModifier();
            }
            return buffHediffStage;
        }
    }

    public ResidentKnightsManager()
    {
        statOffsets = [];
        statFactors = [];

        buffHediffStage = new HediffStage();
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
            foreach (ResidentKnight rk in residentKnights)
            {
                listing_Rect.SubLabel(rk.ToString(), widthPct: 0.8f);
            }
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref residentKnights, "residentKnights", LookMode.Deep);
        Scribe_Values.Look(ref residentLimit, "residentLimit", 0);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            residentKnights.RemoveAll(k => !ValidateResidentKnight(k));
            RegainResidentStat();
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        residentKnights.RemoveAll(k => !ValidateResidentKnight(k) || k.RatkinOrder == ratkinOrder);
        RegainResidentStat();
    }

    public void AddNewResidentKnight(Pawn pawn, RatkinOrder ratkinOrder, bool replaceCur = false)
    {
        if (pawn is null || ratkinOrder is null)
        {
            return;
        }

        if (GetResidentKnightIndexOfPawn(pawn) < 0)
        {
            residentKnights.Add(new ResidentKnight(pawn, ratkinOrder));
        }
    }

    public void RemoveResidentKnight(Pawn pawn)
    {
        if (pawn is null)
        {
            return;
        }

        int index = GetResidentKnightIndexOfPawn(pawn);

        if (index >= 0)
        {
            bool isActive = residentKnights[index].IsActive;
            residentKnights[index].ChangePosition(null);
            residentKnights.RemoveAt(index);
            if (isActive)
            {
                RegainResidentStat();
            }
        }
    }

    public ResidentKnight GetResidentKnightOfRole(ResidentKnightRoleDef def)
    {
        if (def is null)
        {
            return null;
        }

        for (int i = 0; i < residentKnights.Count; i++)
        {
            if (residentKnights[i].RoleDef == def)
            {
                return residentKnights[i];
            }
        }
        return null;
    }

    public int GetResidentKnightIndexOfPawn(Pawn pawn)
    {
        for (int i = 0; i < residentKnights.Count; i++)
        {
            if (residentKnights[i].Pawn == pawn)
            {
                return i;
            }
        }
        return -1;
    }

    public ResidentKnight GetResidentKnightOfPawn(Pawn pawn)
    {
        int index = GetResidentKnightIndexOfPawn(pawn);
        return index < 0 ? null : residentKnights[index];
    }

    public bool SetResidentKnight(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        ResidentKnight pawnRecord = GetResidentKnightOfPawn(pawn);
        if (pawnRecord is null)
        {
            return false;
        }

        ResidentKnight defRecord = GetResidentKnightOfRole(roleDef);
        if (defRecord == pawnRecord)
        {
            return true;
        }

        if (defRecord is null)
        {
            if (pawnRecord.IsActive)
            {
                pawnRecord.ChangePosition(roleDef);
                RegainResidentStat();
            }
            else
            {
                pawnRecord.ChangePosition(roleDef);
                ActiveNewResident(pawnRecord);
            }
            return true;
        }
        else if (replaceCurRole)
        {
            defRecord.ChangePosition(pawnRecord.RoleDef);
            pawnRecord.ChangePosition(roleDef);
            return true;
        }

        return false;
    }

    private void RegainResidentStat()
    {
        statOffsets.Clear();
        statFactors.Clear();

        foreach (ResidentKnight resident in residentKnights)
        {
            if (resident.IsActive)
            {
                ActiveNewResident(resident);
            }
        }

        buffStageDirty = true;
    }

    private void ActiveNewResident(ResidentKnight resident)
    {
        if (resident is null || resident.RoleDef is null)
        {
            return;
        }

        AddStatModifier(resident.RoleDef.statOffsets, isFactor: false);
        AddStatModifier(resident.RoleDef.RoleWorker.RoleStatOffsets(resident.Pawn), isFactor: false);

        AddStatModifier(resident.RoleDef.statFactors, isFactor: true);
        AddStatModifier(resident.RoleDef.RoleWorker.RoleStatFactors(resident.Pawn), isFactor: true);

        void AddStatModifier(IEnumerable<StatModifier> modifiers, bool isFactor)
        {
            if (modifiers is null)
            {
                return;
            }

            Dictionary<StatDef, float> target = isFactor ? statFactors : statOffsets;
            buffStageDirty = true;

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

    private void EstablishBuffStageStatModifier()
    {
        buffHediffStage.statOffsets = statOffsets.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();
        buffHediffStage.statFactors = statFactors.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();
        buffStageDirty = false;
    }

    private static bool ValidateResidentKnight(ResidentKnight k)
    {
        return k is not null && k.Pawn is not null && k.RatkinOrder is not null;
    }
}