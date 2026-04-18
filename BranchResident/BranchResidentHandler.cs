using NightOcean.Collection;
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
    private Dictionary<BranchResidentDef, BranchResidentLoadBox> residentRecords;

    public SimpleValueCache<float> DailyXpFactorCache { get; }

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
        Scribe_Deep.Look(ref residentPawns, nameof(residentPawns));
        Scribe_Collections.Look(ref residentRecords, nameof(residentRecords), LookMode.Def, LookMode.Deep);
    }

    internal void PostBranchGenerated() { }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"驻派人员总数: {residentPawns.Count}");
        listing_Rect.Label($"驻派记录总数: {residentRecords.Count}");
        listing_Rect.SubLabel("二者应该相等，否则大概率有问题。", 0.8f);
        if (listing_Rect.ButtonText("结束所有驻派"))
        {
            ForceEndAllResidency();
        }
    }

    public bool AddResident(BranchResident resident)
    {
        if (resident is null || resident.Pawn is null)
        {
            return false;
        }

        if (residentPawns.TryAddOrTransfer(resident.Pawn))
        {
            resident.Pawn.jobs.StopAll();
            resident.StartResidency(branch);
            if (residentRecords.TryGetValue(resident.Def, out BranchResidentLoadBox residentList))
            {
                residentList.records.Add(resident);
            }
            else
            {
                residentRecords.Add(resident.Def, new BranchResidentLoadBox { records = [resident] });
            }
            return true;
        }
        else
        {
            PlayerDespawnedPawnsTempRetention.Instance.AddPawn(resident.Pawn);
            return false;
        }
    }

    public void TickDay()
    {
        List<BranchResident> expiredRecords = [];

        foreach (BranchResidentLoadBox residentListLoadBox in residentRecords.Values)
        {
            expiredRecords.AddRange(residentListLoadBox.records.ExtractMatching(r => (--r.DeployDaysLeft) <= 0));
        }

        if (expiredRecords.Count > 0)
        {
            residentRecords.RemoveAll(kv => kv.Value.records.NullOrEmpty());
            FinishResidency(expiredRecords);
        }
    }

    public void ForceEndAllResidency()
    {
        List<BranchResident> expiredRecords = [];
        foreach (BranchResidentLoadBox residentListLoadBox in residentRecords.Values)
        {
            expiredRecords.AddRange(residentListLoadBox.records);
        }
        if (expiredRecords.Count > 0)
        {
            FinishResidency(expiredRecords);
        }
        residentRecords.Clear();
        residentPawns.Clear();
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
            try
            {
                resident.EndResidency(branch);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "end residency",
                    typeName: nameof(BranchResidentHandler),
                    methodName: nameof(FinishResidency),
                    needStackTrace: true);
            }
            finally
            {
                if (resident?.Pawn is not null)
                {
                    residentPawns.Remove(resident.Pawn);
                    pawns.Add(resident.Pawn);
                }
            }
        }

        if (pawns.NullOrEmpty())
        {
            return;
        }

        if (caravan is not null)
        {
            foreach (Pawn pawn in pawns)
            {
                if (pawn.Faction != Faction.OfPlayer)
                {
                    OAFrame_PawnUtility.MakePawnJoinPlayer(pawn);
                }

                caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
            }
            Find.LetterStack.ReceiveLetter(label: "OARO_ResidencyFinished_Label".Translate(),
                               text: "OARO_ResidencyFinishedText_JoinCaravan".Translate(
                                   caravan.Named(KeyLibrary_FormatArgName.Caravan),
                                   GenLabel.ThingsLabel(pawns.Cast<Thing>()).Named(KeyLibrary_FormatArgName.PawnsInfo)),
                               textLetterDef: LetterDefOf.PositiveEvent, lookTargets: caravan);
            return;
        }

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
        if (map is not null)
        {
            IncidentParms arrivalParms = new()
            {
                target = map,
            };
            if (ModUtility.TryMakePawnArrival(pawns, arrivalParms, PawnsArrivalModeDefOf.EdgeWalkIn, joinPlayer: true, sendStandardLetter: false))
            {
                Find.LetterStack.ReceiveLetter(label: "OARO_ResidencyFinished_Label".Translate(),
                                               text: "OARO_ResidencyFinishedText_Map".Translate(
                                                   map.Named("map"),
                                                   GenLabel.ThingsLabel(pawns.Cast<Thing>()).Named(KeyLibrary_FormatArgName.PawnsInfo)),
                                               textLetterDef: LetterDefOf.PositiveEvent,
                                               lookTargets: pawns);
                return;
            }
        }

        ResidentsToCaravan(pawns);
    }

    private void ResidentsToCaravan(IEnumerable<Pawn> pawns)
    {
        foreach (Pawn pawn in pawns)
        {
            if (pawn.Faction != Faction.OfPlayer)
            {
                OAFrame_PawnUtility.MakePawnJoinPlayer(pawn);
            }
        }
        Caravan residentCaravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, branch.BaseSite.Tile, addToWorldPawnsIfNotAlready: true);
        Find.LetterStack.ReceiveLetter(label: "OARO_ResidencyFinished_Label".Translate(),
                                       text: "OARO_ResidencyFinishedText_NewCaravan".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>()).Named(KeyLibrary_FormatArgName.PawnsInfo)),
                                       textLetterDef: LetterDefOf.PositiveEvent,
                                       lookTargets: residentCaravan);
    }

    internal void PostLoadInit()
    {
        residentPawns.RemoveAll(p => p.DestroyedOrNull());
        residentRecords.RemoveAll(kv => kv.Key is null || kv.Value is null);
    }

    public ThingOwner GetDirectlyHeldThings() => residentPawns;
    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    /// <summary>
    /// 用于保存加载的中转类（RimWorld不能保存值为集合类型的字典）
    /// </summary>
    private class BranchResidentLoadBox : IExposable
    {
        public List<BranchResident> records = [];

        public void ExposeData()
        {
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                records.RemoveAll(r => r is null || r.Pawn is null);
            }
        }
    }
}