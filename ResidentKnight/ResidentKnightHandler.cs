using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightHandler : IExposable, IOnRatkinOrderRemoved
{
    private List<ResidentKnight> residentKnights = [];
    public IReadOnlyList<ResidentKnight> ResidentKnights => residentKnights;

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

    [Unsaved] private readonly List<StatModifier> statOffsets;
    [Unsaved] private readonly List<StatModifier> statFactors;

    [Unsaved] private readonly HediffStage buffHediffStage;
    public HediffStage BuffHediffStage => buffHediffStage;

    public ResidentKnightHandler()
    {
        statOffsets = [];
        statFactors = [];

        buffHediffStage = new HediffStage()
        {
            statOffsets = statOffsets,
            statFactors = statFactors
        };
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref residentKnights, "residentKnights", LookMode.Deep);

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
                ActiveNewResident(roleDef);
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
                ActiveNewResident(resident.RoleDef);
            }
        }
    }

    private void ActiveNewResident(ResidentKnightRoleDef roleDef)
    {
        if (roleDef is null)
        {
            return;
        }

        if (roleDef.statOffsets is not null)
        {
            foreach (StatModifier offset in roleDef.statOffsets)
            {
                bool merged = false;
                for (int i = 0; i < statOffsets.Count; i++)
                {
                    if (statOffsets[i].stat == offset.stat)
                    {
                        statOffsets[i].value += offset.value;
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    statOffsets.Add(new StatModifier() { stat = offset.stat, value = offset.value });
                }
            }
        }

        if (roleDef.statFactors is not null)
        {
            foreach (StatModifier factor in roleDef.statFactors)
            {
                bool merged = false;
                for (int i = 0; i < statOffsets.Count; i++)
                {
                    if (statOffsets[i].stat == factor.stat)
                    {
                        statOffsets[i].value *= factor.value;
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    statOffsets.Add(new StatModifier() { stat = factor.stat, value = factor.value });
                }
            }
        }
    }

    private static bool ValidateResidentKnight(ResidentKnight k)
    {
        return k is not null && k.Pawn is not null && k.RatkinOrder is not null;
    }
}