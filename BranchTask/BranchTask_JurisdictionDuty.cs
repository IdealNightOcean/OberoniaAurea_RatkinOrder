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
        Scribe_Values.Look(ref playerParticipated, nameof(playerParticipated), defaultValue: false);
        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            dutySite?.SetDutyWorker(this);
        }
    }

    protected override void PostTaskStart()
    {
        dutyData = new JurisdictionDutyData(this);
        GenerateDutySite();
        base.PostTaskStart();
    }

    protected override void PostTaskEnd(bool interrupt)
    {
        if (!interrupt)
        {
            SettlementDuty();
        }
        DestroyDutySite();

        if (playerParticipated)
        {
            Messages.Message(
                text: "OARO_Message_BranchTaskEnded".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                                                               this.Def.Named(KeyLibrary_FormatArgName.DEF)),
                def: MessageTypeDefOf.NeutralEvent);
        }
    }

    public override void TickHour()
    {
        dutyData?.TickHour(this);
    }

    public void Notify_CaravanStartedWork() => playerParticipated = true;

    private void GenerateDutySite()
    {
        if (dutySite is not null) return;

        WorldObjectDef siteDef = OARO_WorldObjectDefOf.OARO_WO_JurisdictionDutySite;

        PlanetTile tile = FindBestDutySiteTile();
        if (!tile.Valid)
            return;

        WorldObject_JurisdictionDutySite site = (WorldObject_JurisdictionDutySite)WorldObjectMaker.MakeWorldObject(siteDef);
        site.Tile = tile;
        site.SetDutyWorker(this);
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
                return tile;
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

        float progressRatio = dutyData.ProgressRatio;

        StringBuilder endSB = new();

        SettlementOrderReward(progressRatio, endSB);
        SettlementPlayerRewards(endSB);

        TaggedString label = "OARO_DutySettlement_Label".Translate(branch.Name);
        TaggedString text = endSB.ToString();
        OrderLetterUtility.ReceiveLetter(
            label: label,
            text: text,
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch
        );
    }

    private void SettlementOrderReward(float progressRatio, StringBuilder endSB)
    {
        float securityGain = Rand.Range(0.08f, 0.16f) * dutyData.ProgressRatio;
        if (branch.BuildingHandler.HasBuilding(BranchBuildingDefOf.OARO_LargeWarningTower))
        {
            securityGain *= 1.5f;
        }
        branch.PopulationHandler.AdjustPublicSecurity(securityGain);
        endSB.AppendLine("OARO_Jurisdiction_OrderReward_PublicSecGain".Translate(securityGain.ToStringPercentSigned("0.##").Named(KeyLibrary_FormatArgName.Value)));

        BranchTaskHandler.RadicalismDegree degree = branch.TaskHandler.CurRadicalismDegree;
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
                    endSB.AppendLine("OARO_Jurisdiction_FundGain".Translate(fundGain.ToStringPercentSigned("0.##")));
                    break;
                }
            case BranchTaskType.Assistance or BranchTaskType.Supervision:
                {
                    float processGain = branch.BranchManager.AllBranches.Count * 0.06f
                                      + branch.Potency * 0.0001f;
                    processGain *= degreeMultiplier * focusMultiplier * progressRatio;
                    ratkinOrder.ReformationManager.ReformProgress += processGain;
                    endSB.AppendLine("OARO_Jurisdiction_ReformProgressGain".Translate(processGain.ToStringPercentSigned("0.##")));
                    break;
                }
        }
        */

        SettlementOrderReward_Objectives(endSB);
    }

    public void SettlementOrderReward_Objectives(StringBuilder endSB)
    {
        IReadOnlyList<CompletedObjective> objectives = dutyData.CompletedObjectives;
        if (objectives is null || objectives.Count <= 0)
            return;


        endSB.AppendLine("OARO_Jurisdiction_ObjectivesCompleted".Translate(objectives.Count.Named(KeyLibrary_FormatArgName.Count)));
        Dictionary<KnightChivalryDef, int> medalRewards = [];
        for (int i = 0; i < objectives.Count; i++)
        {
            CompletedObjective objective = objectives[i];
            if (objective.MedalType is null)
                continue;

            medalRewards[objective.MedalType] = medalRewards.TryGetValue(objective.MedalType, fallback: 0) + 1;

            if (objective.IsAssistance)
            {
                medalRewards[objective.MedalType] = medalRewards.TryGetValue(objective.MedalType, fallback: 0) + objective.AssistanceCount;
            }
        }

        foreach ((KnightChivalryDef medalType, int medalCount) in medalRewards)
        {
            branch.MedalHandler.AdjustMedal(medalType, medalCount);
            endSB.AppendLine("OARO_Jurisdiction_OrderReward_Medal".Translate(
                medalType.Named(KeyLibrary_FormatArgName.DEF),
                medalCount.Named(KeyLibrary_FormatArgName.Count)));
        }

    }

    private void SettlementPlayerRewards(StringBuilder endSB)
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

        endSB.AppendLine("OARO_Jurisdiction_AssistanceRequestCompleted".Translate(completedCount.Named(KeyLibrary_FormatArgName.Count)));

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
        if (!caravanPawns.NullOrEmpty())
        {
            int pawnCount = caravanPawns.Count;
            List<ResidentKnight> caravanKnights = [];
            for (int i = 0; i < caravanPawns.Count; i++)
            {
                Pawn pawn = caravanPawns[i];
                if (ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight knight))
                {
                    knight.MeditationPoints += meditationPoints;
                    endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_Meditation".Translate(
                                        pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                        meditationPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Value)));
                    caravanKnights.Add(knight);
                }
                else
                {
                    SkillDef randomSkill = DefDatabase<SkillDef>.AllDefsListForReading.RandomElement();
                    pawn.skills.GetSkill(randomSkill).Learn(meditationPoints);
                    endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_Skill".Translate(
                                       pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                       randomSkill.Named(KeyLibrary_FormatArgName.SKILL),
                                       meditationPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Value)));
                }
            }

            endSB.AppendLine();
            endSB.AppendLine();
            KnightVirtueReward(caravanKnights, virtueChance, endSB);

            endSB.AppendLine();
            endSB.AppendLine();
            AcademicReward(caravanKnights, academicChance, endSB);

            endSB.AppendLine();
            endSB.AppendLine();

            IReadOnlyList<ResidentKnight> allKnights = ResidentPawnsManager.Instance.ResidentKnights;
            float meditationPointsOther = meditationPoints * 0.1f;
            for (int i = 0; i < allKnights.Count; i++)
            {
                allKnights[i].MeditationPoints += meditationPointsOther;
            }
            endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_MeditationOther".Translate(meditationPointsOther.ToString("f0").Named(KeyLibrary_FormatArgName.Value)));

            RatkinOrder ratkinOrder = branch.RatkinOrder;
            if (ratkinOrder.Faction is not null)
            {
                ratkinOrder.Faction.TryAffectGoodwillWith(Faction.OfPlayer, completedCount, canSendMessage: true);
                endSB.AppendLine();
                endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_FactionGoodwill".Translate(
                                    ratkinOrder.Faction.Named(KeyLibrary_FormatArgName.FACTION),
                                    completedCount.Named(KeyLibrary_FormatArgName.Change)));
            }
        }
    }

    private void KnightVirtueReward(List<ResidentKnight> caravanKnights, float virtueChance, StringBuilder endSB)
    {
        if (dutyData.DutyAcademics is null || dutyData.DutyAcademics.Count <= 0)
            return;

        List<KnightVirtueDef> potentialVirtues = [];
        foreach (KnightAcademicDef academicDef in dutyData.DutyAcademics)
        {
            if (academicDef.chivalry?.AllKnightVirtues is null)
                continue;

            potentialVirtues.AddRange(academicDef.chivalry.AllKnightVirtues);
        }


        while (virtueChance >= 1f)
        {
            virtueChance -= 1f;
            ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
            if (knight is not null)
            {
                KnightVirtueDef randomVirtue = potentialVirtues.RandomElementWithFallback();
                if (randomVirtue is not null && knight.VirtueHandler.UpgradeVirtue(randomVirtue, upgrade: 1, reason: "OARO_VirtueUpgradeReason_AssistanceReward".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName))))
                {
                    endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_KnightVirtue".Translate(
                                 knight.Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                 randomVirtue.Named(KeyLibrary_FormatArgName.DEF)));
                }
            }
        }
        if (Rand.Chance(virtueChance))
        {
            ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
            if (knight is not null)
            {
                KnightVirtueDef randomVirtue = potentialVirtues.RandomElementWithFallback();
                if (randomVirtue is not null && knight.VirtueHandler.UpgradeVirtue(randomVirtue, upgrade: 1, reason: "OARO_VirtueUpgradeReason_AssistanceReward".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName))))
                {
                    endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_KnightVirtue".Translate(
                                 knight.Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                 randomVirtue.Named(KeyLibrary_FormatArgName.DEF)));
                }
            }
        }
    }

    private void AcademicReward(List<ResidentKnight> caravanKnights, float academicChance, StringBuilder endSB)
    {
        if (dutyData.DutyAcademics is null || dutyData.DutyAcademics.Count <= 0)
            return;

        while (academicChance >= 1f)
        {
            academicChance -= 1f;
            ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
            if (knight is null)
                break;

            KnightAcademicDef randomAcademic = dutyData.DutyAcademics?.RandomElementWithFallback();
            if (randomAcademic is not null)
            {
                knight.AcademicHandler.UpgradeAcademic(randomAcademic);
                endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_Academic".Translate(
                                 knight.Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                 randomAcademic.Named(KeyLibrary_FormatArgName.DEF)));
            }
        }

        if (Rand.Chance(academicChance))
        {
            ResidentKnight knight = caravanKnights.RandomElementWithFallback(null);
            if (knight is not null)
            {
                KnightAcademicDef randomAcademic = dutyData.DutyAcademics?.RandomElementWithFallback();
                if (randomAcademic is not null)
                {
                    knight.AcademicHandler.UpgradeAcademic(randomAcademic);
                    endSB.AppendLine("OARO_Jurisdiction_AssistanceRewards_Academic".Translate(
                                     knight.Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                     randomAcademic.Named(KeyLibrary_FormatArgName.DEF)));
                }
            }
        }
    }
}
