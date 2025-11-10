using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResidentHandler : IExposable, IThingHolder, IPawnRetentionHolder
{
    [Unsaved] private readonly Branch branch;

    private ThingOwner<Pawn> residentPawns;
    private List<BranchResident> residentRecords;

    [Unsaved] public readonly SimpleValueCache<float> DailyXpFactorCache;

    public IThingHolder ParentHolder
    {
        get
        {
            if (!branch.BaseSite.Spawned)
            {
                return null;
            }
            return Find.World;
        }
    }

    internal BranchResidentHandler(Branch branch, bool initCtor)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        DailyXpFactorCache = new SimpleValueCache<float>(cacheInterval: 2500, defaultValue: 1f, checker: () => branch.GetStatValue(BranchStatDefOf.OARO_DeployeeDailyXpFactor));
        if (initCtor)
        {
            residentPawns = new ThingOwner<Pawn>(this);
            residentRecords = [];
        }
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref residentPawns, "residents");
        Scribe_Collections.Look(ref residentRecords, "residentRecords", LookMode.Deep);
    }

    internal void PostBranchGenerated() { }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"residents: {residentPawns.Count}");
        listing_Rect.Label($"residentRecords: {residentRecords.Count}");
    }

    public bool AddResident(BranchResident resident)
    {
        if (resident?.Resident is null)
        {
            return false;
        }

        if (residentPawns.TryAddOrTransfer(resident.Resident))
        {
            resident.StartResidency(branch);
            residentRecords.BinaryInsertion(resident, new ResidencyInsertComparer());
            return true;
        }
        return false;
    }

    public void TickDay()
    {
        List<BranchResident> expiredRecords = [];

        foreach (BranchResident record in residentRecords)
        {
            if ((record.DeployDaysLeft -= 1) <= 0)
            {
                expiredRecords.Add(record);
            }
        }

        if (expiredRecords.Count > 0)
        {
            residentRecords.RemoveAll(r => r.DeployDaysLeft <= 0);
            FinishResidency(expiredRecords);
        }
    }

    public void ForceEndAllResidency()
    {
        residentRecords.Clear();
        Caravan caravan = CaravanMaker.MakeCaravan(residentPawns.InnerListForReading, Faction.OfPlayer, branch.BaseSite.Tile, addToWorldPawnsIfNotAlready: true);
    }

    private void FinishResidency(IEnumerable<BranchResident> residentRecords, Caravan caravan = null)
    {
        if (residentRecords is null)
        {
            return;
        }

        List<Pawn> pawns = [];
        foreach (BranchResident resident in residentRecords)
        {
            pawns.Add(resident.Resident);
            resident.EndResidency(branch);
        }

        if (caravan is not null)
        {
            foreach (Pawn pawn in pawns)
            {
                caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
            }
            Find.LetterStack.ReceiveLetter(label: "OARO_ResidencyFinished_Label".Translate(),
                               text: "OARO_ResidencyFinishedText_JoinCaravan".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>())),
                               textLetterDef: LetterDefOf.PositiveEvent, lookTargets: caravan);
            return;
        }

        Map map = Find.AnyPlayerHomeMap;
        if (map is null)
        {
            Caravan residentCaravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, branch.BaseSite.Tile, addToWorldPawnsIfNotAlready: true);
            Find.LetterStack.ReceiveLetter(label: "OARO_ResidencyFinished_Label".Translate(),
                                           text: "OARO_ResidencyFinishedText_NewCaravan".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>())),
                                           textLetterDef: LetterDefOf.PositiveEvent, lookTargets: residentCaravan);
        }
        else
        {
            IncidentParms arrivalParms = new()
            {
                target = map,
            };
            PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(arrivalParms);
            PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(pawns, arrivalParms);
            Find.LetterStack.ReceiveLetter(label: "OARO_ResidencyFinished_Label".Translate(),
                                           text: "OARO_ResidencyFinishedText_Map".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>())),
                                           textLetterDef: LetterDefOf.PositiveEvent, lookTargets: pawns);
        }
    }

    internal void PostLoadInit()
    {
        residentPawns.RemoveAll(p => p.DestroyedOrNull());
        residentRecords.RemoveAll(rc => rc is null || rc.Resident.DestroyedOrNull());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return residentPawns;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    /// <summary>
    /// 二分插入使用的
    /// </summary>
    private class ResidencyInsertComparer : IComparer<BranchResident>
    {
        public int Compare(BranchResident x, BranchResident y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return 1;
            if (y is null) return -1;

            return (y.Priority, y.GetType().FullName).CompareTo((x.Priority, x.GetType().FullName));
        }
    }
}