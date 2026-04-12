using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueHandler : IExposable
{
    public const int MaxVirtues = 4;

    private List<KnightVirtue> virtues = new(4);

    public IReadOnlyList<KnightVirtue> Virtues => virtues;

    public int TotalVirtueCount => virtues.Count;

    public int TotalVirtueLevel => virtues.Sum(v => v.Level);

    private bool virtuesDirty = true;

    public bool AddVirtue(KnightVirtueDef virtueDef, int level)
    {
        if (virtues.Count >= MaxVirtues)
        {
            return false;
        }
        if (HasVirtue(virtueDef))
        {
            return false;
        }
        virtues.Add(new KnightVirtue(virtueDef, level));
        virtuesDirty = true;
        return true;
    }

    public bool UpgradeVirtue(KnightVirtueDef virtueDef)
    {
        KnightVirtue virtue = GetVirtue(virtueDef);
        if (virtue is null)
        {
            return false;
        }
        if (virtue.Level >= virtueDef.maxLevel)
        {
            return false;
        }
        virtue.Level++;
        virtuesDirty = true;
        return true;
    }

    public bool HasVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                return true;
            }
        }
        return false;
    }

    public KnightVirtue GetVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                return virtues[i];
            }
        }
        return null;
    }

    private bool RemoveVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                virtues.RemoveAt(i);
                virtuesDirty = true;
                return true;
            }
        }

        return false;
    }

    public bool AbandonVirtue(ResidentKnight record, KnightVirtueDef virtue)
    {
        if (!RemoveVirtue(virtue))
        {
            return false;
        }
        float meditationPointsToReduce = record.AcademicHandler.TotalAcademicLevel.Value * 500f;
        if (virtue.relatedPersonality == record.Personality)
        {
            meditationPointsToReduce *= 2f;
        }

        record.MeditationPoints -= meditationPointsToReduce;

        return true;
    }


    public void ExposeData()
    {
        Scribe_Collections.Look(ref virtues, nameof(virtues), LookMode.Deep);
    }

}