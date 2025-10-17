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

    private ThingOwner<Pawn> residents;
    private List<BranchResidentRecord> residentRecords = [];

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
        if (initCtor)
        {
            residents = new ThingOwner<Pawn>(this);
            residentRecords = [];
        }
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref residents, "residents");
        Scribe_Collections.Look(ref residentRecords, "residentRecords", LookMode.Deep);
    }

    internal void PostBranchGenerated() { }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"residents: {residents.Count}");
        listing_Rect.Label($"residentRecords: {residentRecords.Count}");
    }

    public bool AddResident(Pawn pawn, int daysDeployed, ResidencyWorker worker)
    {
        if (pawn is null || worker is null)
        {
            return false;
        }

        if (residents.TryAddOrTransfer(pawn))
        {
            BranchResidentRecord record = new(pawn, daysDeployed, worker);
            int insertIndex = residentRecords.Count;
            for (int i = 0; i < residentRecords.Count; i++)
            {
                if (ResidencyInsertCompare(residentRecords[i].ResidencyWorker, worker) <= 0)
                {
                    insertIndex = i;
                    break;
                }
            }
            residentRecords.Insert(insertIndex, record);
            return true;
        }
        return false;
    }

    public void TickDay()
    {
        ResidencyWorker_Deployment.ClearStaticCache();
        List<BranchResidentRecord> expiredRecords = [];

        foreach (BranchResidentRecord record in residentRecords)
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

        ResidencyWorker_Deployment.ClearStaticCache();
    }

    public void ForceEndAllResidency()
    {
        residentRecords.Clear();
        Caravan caravan = CaravanMaker.MakeCaravan(residents.InnerListForReading, Faction.OfPlayer, branch.BaseSite.Tile, addToWorldPawnsIfNotAlready: true);
    }

    private void FinishResidency(IEnumerable<BranchResidentRecord> residentRecords)
    {
        if (residentRecords is null)
        {
            return;
        }

        foreach (BranchResidentRecord record in residentRecords)
        {
            record.ResidencyWorker?.ResidencyEnd(branch, record.Resident, record.TotalDeployDays);
        }

        List<Pawn> pawns = residentRecords.Select(rc => rc.Resident)
                                          .ToList();
        Map map = Find.AnyPlayerHomeMap;
        if (map is null)
        {
            Caravan caravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, branch.BaseSite.Tile, addToWorldPawnsIfNotAlready: true);
            Find.LetterStack.ReceiveLetter("OARO_LetterLabel_DeployFinished".Translate(),
                                           "OARO_Letter_DeployFinishedCaravan".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>())),
                                           LetterDefOf.PositiveEvent, caravan);
        }
        else
        {
            IncidentParms incidentParms = new()
            {
                target = map,
            };
            PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(incidentParms);
            PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(pawns, incidentParms);
            Find.LetterStack.ReceiveLetter("OARO_LetterLabel_DeployFinished".Translate(),
                                           "OARO_Letter_DeployFinishedJoin".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>())),
                                           LetterDefOf.PositiveEvent, pawns);
        }
    }

    internal void PostLoadInit()
    {
        residents.RemoveAll(p => p.DestroyedOrNull());
        residentRecords.RemoveAll(rc => rc is null || rc.Resident.DestroyedOrNull());
    }

    private static int ResidencyInsertCompare(ResidencyWorker x, ResidencyWorker y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return (x.Priority, x.GetType().FullName)
                  .CompareTo((y.Priority, y.GetType().FullName));
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return residents;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }
}