using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 辖区执勤 - 含执勤数据、交互点、协助需求
/// </summary>
public class BranchTask_JurisdictionDuty : BranchTask
{
    private JurisdictionDutyData dutyData;
    public JurisdictionDutyData DutyData => dutyData;

    private WorldObject_JurisdictionDutySite dutySite;
    public WorldObject_JurisdictionDutySite DutySite => dutySite;

    private bool playerParticipated;
    public bool PlayerParticipated => playerParticipated;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref dutyData, nameof(dutyData));
        Scribe_References.Look(ref dutySite, nameof(dutySite));
        Scribe_Values.Look(ref playerParticipated, nameof(playerParticipated), false);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            dutySite?.InitDutySite(branch, this);
        }
    }

    public override int BranchRestTick()
    {
        return branch.TaskHandler.CurRadicalismDegree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 45 * 60000,
            BranchTaskHandler.RadicalismDegree.Aggressive => 15 * 60000,
            _ => 30 * 60000
        };
    }

    protected override void PostTaskStart()
    {
        dutyData = new JurisdictionDutyData(this);
        GenerateDutySite();
        Notify_DutyStarted();
    }

    protected override void PostTaskEnd()
    {
        SettlementDuty();
        DestroyDutySite();
        Notify_DutyEnded();
    }

    public override void TickHour()
    {
        dutyData?.TickHour(this);
    }

    public void Notify_CaravanStartedWork() => playerParticipated = true;


    public void Notify_CaravanInterruptedWork(FixedCaravan fixedCaravan) { }

    public void Notify_CaravanFinishedWorkCycle(FixedCaravan fixedCaravan)
    {
        if (dutyData is null || fixedCaravan is null) return;
        dutyData.OnWorkCycleCompleted(fixedCaravan);
    }

    private void GenerateDutySite()
    {
        if (dutySite is not null) return;

        WorldObjectDef siteDef = OARO_WorldObjectDefOf.OARO_WO_JurisdictionDutySite;

        PlanetTile tile = FindBestDutySiteTile();
        if (!tile.Valid)
            return;

        WorldObject_JurisdictionDutySite site = (WorldObject_JurisdictionDutySite)WorldObjectMaker.MakeWorldObject(siteDef);
        site.Tile = tile;
        site.InitDutySite(branch, this);
        Find.WorldObjects.Add(site);
        dutySite = site;
    }

    private PlanetTile FindBestDutySiteTile()
    {
        PlanetTile branchTile = branch.Tile;
        List<PlanetTile> neighboringTiles = [];
        Find.WorldGrid.GetTileNeighbors(branchTile, neighboringTiles);

        for (int i = 0; i < neighboringTiles.Count; i++)
        {
            PlanetTile tile = neighboringTiles[i];
            if (!Find.WorldObjects.AnyWorldObjectAt(tile))
            {
                return tile;
            }
        }

        if (TileFinder.TryFindPassableTileWithTraversalDistance(
                rootTile: branchTile,
                minDist: 2,
                maxDist: 5,
                result: out PlanetTile result,
                validator: t => !Find.WorldObjects.AnyWorldObjectAt(t),
                ignoreFirstTilePassability: false,
                tileFinderMode: TileFinderMode.Near))
        {
            return result;
        }

        return PlanetTile.Invalid;
    }

    private void DestroyDutySite()
    {
        if (dutySite is not null)
        {
            dutySite.EndWork(interrupt: true, convertToCaravan: true);
            dutySite.SafeDestroy();
            dutySite = null;
        }
    }

    private void SettlementDuty()
    {
        if (dutyData is null) return;

        RatkinOrder ratkinOrder = branch.RatkinOrder;
        BranchTaskHandler.RadicalismDegree degree = branch.TaskHandler.CurRadicalismDegree;
        float progressRatio = dutyData.ProgressRatio;

        StringBuilder endSB = new();

        SettlementFundAndReformation(ratkinOrder, degree, progressRatio, endSB);
        SettlementPublicSecurity(endSB);
        SettlementObjectives(endSB);
        SettlementAssistanceRewards(ratkinOrder, endSB);

        SendSettlementLetter(endSB);
    }

    private void SettlementFundAndReformation(RatkinOrder ratkinOrder, BranchTaskHandler.RadicalismDegree degree, float progressRatio, StringBuilder sb)
    {
        float degreeMultiplier = degree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.75f,
            BranchTaskHandler.RadicalismDegree.Aggressive => 1.33f,
            _ => 1f
        };
        float focusMultiplier = TaskChivalry.IsSameDefNonNullable(branch.TaskHandler.FocusedTaskChivalry) ? 1.15f : 1f;

        /*
        switch (TaskChivalry)
        {
            case BranchTaskType.CrimeFighting or BranchTaskType.StabilityMaintenance:
                {
                    float fundGain = ratkinOrder.BranchManager.AllBranches.Count * 0.04f
                                   + branch.Potency * 0.0001f;
                    fundGain *= degreeMultiplier * focusMultiplier * progressRatio;
                    ratkinOrder.FundHandler.AdjustFundsImmediately(fundGain, "OARO_FundChange_BranchTask".Translate());
                    sb.AppendLine("OARO_Jurisdiction_FundGain".Translate(fundGain.ToStringPercentSigned("0.##")));
                    break;
                }
            case BranchTaskType.Assistance or BranchTaskType.Supervision:
                {
                    float processGain = branch.BranchManager.AllBranches.Count * 0.06f
                                      + branch.Potency * 0.0001f;
                    processGain *= degreeMultiplier * focusMultiplier * progressRatio;
                    ratkinOrder.ReformationManager.ReformProgress += processGain;
                    sb.AppendLine("OARO_Jurisdiction_ReformProgressGain".Translate(processGain.ToStringPercentSigned("0.##")));
                    break;
                }
        }
        */
    }

    private void SettlementPublicSecurity(StringBuilder sb)
    {
        float securityGain = Rand.Range(0.08f, 0.16f) * dutyData.ProgressRatio;
        if (branch.BuildingHandler.HasBuilding(BranchBuildingDefOf.OARO_LargeWarningTower))
        {
            securityGain *= 1.5f;
        }
        branch.PopulationHandler.AdjustPublicSecurity(securityGain);
        sb.AppendLine("OARO_Jurisdiction_PublicSecGain".Translate(securityGain.ToStringPercentSigned("0.##")));

        List<Branch> nearbyBranches = BranchUtility.GetAllAffectedBranch(branch.Tile);
        if (!nearbyBranches.NullOrEmpty())
        {
            float otherSecurityGain = 0.02f;
            BranchBuilding largeWarningTower = branch.BuildingHandler.GetBuilding(BranchBuildingDefOf.OARO_LargeWarningTower);
            if (largeWarningTower is not null && largeWarningTower.HasUpgraded)
            {
                otherSecurityGain *= 5f;
            }
            sb.AppendLine("OARO_Jurisdiction_OtherPublicSecGain".Translate(otherSecurityGain.ToStringPercentSigned("0.##")));
            for (int i = 0; i < nearbyBranches.Count; i++)
            {
                nearbyBranches[i].PopulationHandler.AdjustPublicSecurity(otherSecurityGain);
            }
        }
    }

    private void SettlementObjectives(StringBuilder sb)
    {
        IReadOnlyList<CompletedObjective> objectives = dutyData.CompletedObjectives;
        if (objectives.Count <= 0) return;

        for (int i = 0; i < objectives.Count; i++)
        {
            CompletedObjective objective = objectives[i];
            branch.MedalHandler.AdjustMedal(objective.MedalType, 1);

            if (objective.IsAssistance)
            {
                branch.MedalHandler.AdjustMedal(objective.MedalType, objective.AssistanceCount);
            }
        }

        sb.AppendLine("OARO_Jurisdiction_ObjectivesCompleted".Translate(objectives.Count));
    }

    private void SettlementAssistanceRewards(RatkinOrder ratkinOrder, StringBuilder sb)
    {
        IReadOnlyList<AssistanceRequest> requests = dutyData.AssistanceRequests;
        int completedCount = 0;
        for (int i = 0; i < requests.Count; i++)
        {
            if (requests[i].Completed)
            {
                completedCount++;
            }
        }

        if (completedCount <= 0) return;

        float meditationPoints = 0f;
        float virtueChance = 0f;
        float academicChance = 0f;

        for (int i = 0; i < completedCount; i++)
        {
            float roll = Rand.Value;
            if (roll < 0.4f)
            {
                meditationPoints += 750f;
            }
            else if (roll < 0.75f)
            {
                meditationPoints += 300f;
                academicChance += 0.25f;
            }
            else
            {
                meditationPoints += 300f;
                virtueChance += 0.10f;
            }
        }

        FixedCaravan fixedCaravan = dutySite?.AssociatedFixedCaravan;
        List<Pawn> caravanPawns = fixedCaravan?.PawnsListForReading;
        if (caravanPawns is not null && caravanPawns.Count > 0)
        {
            int pawnCount = caravanPawns.Count;
            float perPawn = meditationPoints / pawnCount;
            List<ResidentKnight> caravanKnights = [];
            for (int i = 0; i < caravanPawns.Count; i++)
            {
                Pawn pawn = caravanPawns[i];
                if (ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight knight))
                {
                    knight.MeditationPoints += perPawn * 1.1f;
                    caravanKnights.Add(knight);
                }
                else
                {
                    SkillDef randomSkill = DefDatabase<SkillDef>.AllDefsListForReading.RandomElement();
                    pawn.skills.GetSkill(randomSkill).Learn(perPawn * 0.01f);
                }
            }

            while (virtueChance >= 1f)
            {
                virtueChance -= 1f;
                ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
                if (knight is not null)
                {
                    KnightVirtueUtility.GetRandomNewVirtueLevel_Daily(knight);
                }
            }
            if (Rand.Chance(virtueChance))
            {
                ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
                if (knight is not null)
                {
                    KnightVirtueUtility.GetRandomNewVirtueLevel_Daily(knight);
                }
            }

            while (academicChance >= 1f)
            {
                academicChance -= 1f;
                ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
                if (knight is not null)
                {
                    IReadOnlyDictionary<KnightAcademicDef, int> academics = knight.AcademicHandler.Academics;
                    if (academics.Count > 0)
                    {
                        List<KnightAcademicDef> academicDefs = new(academics.Keys);
                        KnightAcademicDef academicDef = academicDefs[Rand.Range(0, academicDefs.Count)];
                        knight.AcademicHandler.UpgradeAcademic(academicDef, knight.Pawn, knight.Chivalry, directly: true);
                    }
                }
            }
        }

        IReadOnlyList<ResidentKnight> allKnights = ResidentPawnsManager.Instance.ResidentKnights;
        for (int i = 0; i < allKnights.Count; i++)
        {
            if (allKnights[i].Branch == branch)
            {
                allKnights[i].MeditationPoints += meditationPoints * 0.1f;
            }
        }

        ratkinOrder.Faction?.TryAffectGoodwillWith(Faction.OfPlayer, completedCount, canSendMessage: true);
        sb.AppendLine("OARO_Jurisdiction_AssistanceRewards".Translate(completedCount, meditationPoints.ToString("0")));
    }

    private void Notify_DutyStarted()
    {
        List<Branch> nearbyBranches = BranchUtility.GetAllAffectedBranch(branch.Tile);
        if (!nearbyBranches.NullOrEmpty())
        {
            TaggedString label = "OARO_DutyStarted_NearbyLabel".Translate(branch.Name);
            TaggedString text = "OARO_DutyStarted_NearbyText".Translate(branch.Name);
            OrderLetterUtility.ReceiveLetter(
                label: label,
                text: text,
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: branch.RatkinOrder,
                relatedBranch: branch
            );
        }
    }

    private void Notify_DutyEnded()
    {
        if (playerParticipated)
        {
            TaggedString label = "OARO_DutyEnded_Label".Translate(branch.Name);
            TaggedString text = "OARO_DutyEnded_Text".Translate(branch.Name);
            OrderLetterUtility.ReceiveLetter(
                label: label,
                text: text,
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: branch.RatkinOrder,
                relatedBranch: branch
            );
        }
    }

    private void SendSettlementLetter(StringBuilder sb)
    {
        TaggedString label = "OARO_DutySettlement_Label".Translate(branch.Name);
        TaggedString text = sb.ToString();
        OrderLetterUtility.ReceiveLetter(
            label: label,
            text: text,
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch
        );
    }
}
