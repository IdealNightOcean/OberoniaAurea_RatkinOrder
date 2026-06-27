using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueWithComps : KnightVirtue
{
    public List<KnightVirtueComp> comps;

    public override void PostAdd()
    {
        base.PostAdd();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostAdd();
            }
        }
    }

    public override void PostActive()
    {
        base.PostActive();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostActive();
            }
        }
    }

    public override void PostRemove()
    {
        base.PostRemove();
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].PostRemove();
            }
        }
    }

    public override void OnRefreshBuffStage(HediffStageModifierBuilder buffStageBuilder)
    {
        base.OnRefreshBuffStage(buffStageBuilder);
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].OnRefreshBuffStage(buffStageBuilder);
            }
        }
    }

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        base.Notify_KilledPawn(victim, dinfo);
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].Notify_KilledPawn(victim, dinfo);
            }
        }
    }

    public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            }
        }
    }

    public override void Notify_Stimulate(Pawn recipient)
    {
        base.Notify_Stimulate(recipient);
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].Notify_Stimulate(recipient);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            InitializeComps();
        }
        if (comps is not null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                comps[i].CompExposeData();
            }
        }
    }

    public T GetComp<T>() where T : KnightVirtueComp
    {
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is T)
            {
                return comps[i] as T;
            }
        }
        return null;
    }

    private void InitializeComps()
    {
        if (Def.comps is null)
            return;

        comps = new List<KnightVirtueComp>(Def.comps.Count);
        for (int i = 0; i < Def.comps.Count; i++)
        {
            KnightVirtueComp virtueComp = null;
            try
            {
                virtueComp = (KnightVirtueComp)Activator.CreateInstance(Def.comps[i].compClass);
                virtueComp.Initialize(this, Def.comps[i]);
                comps.Add(virtueComp);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"实例化或初始化 {nameof(KnightVirtueComp)} ",
                    typeName: nameof(KnightVirtueWithComps),
                    methodName: nameof(InitializeComps),
                    needStackTrace: true);

                comps.Remove(virtueComp);
            }
        }
    }
}

