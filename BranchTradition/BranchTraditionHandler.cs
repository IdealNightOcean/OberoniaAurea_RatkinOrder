using NightOcean;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTraditionHandler : IExposable
{
    protected const int MaxTraditions = 5;
    protected const int FacilityLevelPerTradition = 8;
    protected const int PopulationPerTradition = 2000;

    protected readonly Branch branch;

    protected List<BranchTradition> traditions = [];
    public IReadOnlyList<BranchTradition> Traditions => traditions;

    public int CurMaxTraditions
    {
        get
        {
            int maxTraditions = branch.FacilityHandler.TotalFacilityLevel / FacilityLevelPerTradition
                              + branch.PopulationHandler.Population / PopulationPerTradition;
            return Math.Min(maxTraditions, MaxTraditions);
        }
    }

    public LazyMutable<float> ExtraBranchPotencyFactor;
    public LazyMutable<float> ExtraMeditationFactor;


    internal BranchTraditionHandler(Branch branch)
    {
        this.branch = branch;

        ExtraBranchPotencyFactor = new LazyMutable<float>(() =>
        {
            float factor = 1f;
            foreach (BranchTradition tradition in traditions)
            {
                factor += tradition.Stage?.extraBranchPotencyFactor ?? 0f;
            }
            return factor;
        });

        ExtraMeditationFactor = new LazyMutable<float>(() =>
        {
            float factor = 1f;
            foreach (BranchTradition tradition in traditions)
            {
                factor += tradition.Stage?.extraMeditationFactor ?? 0f;
            }
            return factor;
        });
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref traditions, nameof(traditions), LookMode.Deep);
    }

    internal void PostLoadInit() { }


    public bool HasTradition(BranchTraditionDef traditionDef)
    {
        foreach (BranchTradition tradition in traditions)
        {
            if (tradition.Def == traditionDef)
                return true;
        }
        return false;
    }

    public bool CanAddradition(BranchTraditionDef traditionDef, bool byPlayer)
    {
        if (traditionDef is null)
        {
            return false;
        }
        if (traditions.Count >= CurMaxTraditions)
        {
            return false;
        }
        if (HasTradition(traditionDef))
        {
            return false;
        }
        if (byPlayer && !branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            return false;
        }
        return true;
    }

    public void AddTradition(BranchTraditionDef traditionDef)
    {
        if (HasTradition(traditionDef))
            return;

        BranchTradition tradition = BranchTradition.GenerateTradition(traditionDef);
        tradition.ApplyEffects(branch);
        traditions.Add(tradition);
        tradition.PostEstablish(branch);
    }

    public bool CanUpgradeTradition(BranchTraditionDef traditionDef)
    {
        BranchTradition tradition = traditions.FirstOrDefault(t => t.Def == traditionDef);
        if (tradition is null)
        {
            return false;
        }
        return tradition.CanUpgrade(branch);
    }

    public void UpgradeTradition(BranchTraditionDef traditionDef)
    {
        BranchTradition tradition = GetTradition(traditionDef);
        if (tradition is not null && tradition.CanUpgrade(branch))
        {
            tradition.Upgrade(branch);
        }
    }

    public BranchTradition GetTradition(BranchTraditionDef traditionDef)
    {
        foreach (BranchTradition tradition in traditions)
        {
            if (tradition.Def == traditionDef)
                return tradition;
        }
        return null;
    }

    public bool RemoveTradition(BranchTraditionDef traditionDef)
    {
        for (int i = 0; i < traditions.Count; i++)
        {
            if (traditions[i].Def == traditionDef)
            {
                traditions.RemoveAt(i);
                return true;
            }
        }

        return false;
    }
}