using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResidentHandler(Branch branch) : IExposable, IThingHolder, IPostLoadInit, IPawnRetentionHolder, IDrawDevWindow
{
    [Unsaved] public readonly Branch Branch = branch ?? throw new ArgumentNullException(nameof(branch));

    private ThingOwner<Pawn> residents;
    private List<BranchResidentRecord> residentRecords = [];

    public IThingHolder ParentHolder
    {
        get
        {
            if (!Branch.WorldObject.Spawned)
            {
                return null;
            }
            return Find.World;
        }
    }

    public void PostBranchGenerated()
    {
        EnsureComponentsInit();
    }

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
        Caravan caravan = CaravanMaker.MakeCaravan(residents.InnerListForReading, Faction.OfPlayer, Branch.WorldObject.Tile, addToWorldPawnsIfNotAlready: true);
    }

    private void FinishResidency(IEnumerable<BranchResidentRecord> residentRecords)
    {
        if (residentRecords is null)
        {
            return;
        }

        foreach (BranchResidentRecord record in residentRecords)
        {
            record.ResidencyWorker?.ResidencyEnd(Branch, record.Resident, record.TotalDeployDays);
        }

        List<Pawn> pawns = residentRecords.Select(rc => rc.Resident)
                                          .ToList();
        Map map = Find.AnyPlayerHomeMap;
        if (map is null)
        {
            Caravan caravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, Branch.WorldObject.Tile, addToWorldPawnsIfNotAlready: true);
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

    public void PostLoadInit()
    {
        EnsureComponentsInit();
        residents.RemoveAll(p => p.DestroyedOrNull());
        residentRecords.RemoveAll(rc => rc is null || rc.Resident.DestroyedOrNull());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureComponentsInit()
    {
        residents ??= new ThingOwner<Pawn>(this);
        residentRecords ??= [];
    }

    private static int ResidencyInsertCompare(ResidencyWorker x, ResidencyWorker y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return (x.Priority, x.GetType().FullName)
                  .CompareTo((y.Priority, y.GetType().FullName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ThingOwner GetDirectlyHeldThings()
    {
        return residents;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref residents, "residents");
        Scribe_Collections.Look(ref residentRecords, "residentRecords", LookMode.Deep);
    }

}