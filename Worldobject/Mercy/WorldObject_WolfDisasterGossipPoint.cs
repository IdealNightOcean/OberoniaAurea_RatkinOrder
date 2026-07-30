using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

internal class WorldObject_WolfDisasterGossipPoint : WorldObject_InteractWithFixedCaravanBase
{
    private enum GossipType : byte
    {
        Intuition,
        Habitat,
        AbandonedWolfDen,
        DeliberateTraces
    }

    private enum IntuitionDirection
    {
        Fornt,
        Side,
        Back
    }

    private GossipType gossipType;
    private IntuitionDirection rightIntuitionDirection;
    private bool SearchSuccessful { get; set; }

    public override int TicksNeeded => 15000;
    protected override string VisitLabel => "OARO_WolfDisasterGossipPoint_Search";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref gossipType, nameof(gossipType));
        Scribe_Values.Look(ref rightIntuitionDirection, nameof(rightIntuitionDirection));
    }

    private void GainIntelligence(int count = 1)
    {
        if (QuestPart_WolfDisasterWatcher.GetWolfDisasterWatcher(quest, out QuestPart_WolfDisasterWatcher watcher))
        {
            watcher.GainIntelligence(count);
        }
    }

    public override void PostMake()
    {
        base.PostMake();
        gossipType = EnumUtility.GetValues<GossipType>().RandomElement();

        if (gossipType == GossipType.Intuition)
        {
            rightIntuitionDirection = EnumUtility.GetValues<IntuitionDirection>().RandomElement();
        }
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (!caravan.PawnsListForReading.Any(p => p.skills is not null && !p.skills.GetSkill(SkillDefOf.Animals).TotallyDisabled))
        {
            Messages.Message("OAFrame_MissSkillAvailablePawn".Translate(SkillDefOf.Animals.Named(KeyLibrary_FormatArgName.SKILL)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        base.Notify_CaravanArrived(caravan);
    }

    public override bool StartWork(Caravan caravan)
    {
        float sucessChange = 0.2f + OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Animals) * 0.05f;
        Messages.Message(
            text: "OARO_WolfDisasterGossipPoint_SearchStarted".Translate(
                this.Named(KeyLibrary_FormatArgName.WORLDOBJECT),
                sucessChange.ToStringPercent().Named(KeyLibrary_FormatArgName.Chance)),
            lookTargets: this,
            def: MessageTypeDefOf.NeutralEvent,
            historical: false);

        return base.StartWork(caravan);
    }

    protected override void FinishWork()
    {
        int maxSkillLevel = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
        SearchSuccessful = Rand.Chance(0.2f + maxSkillLevel * 0.05f);
    }

    protected override void InterruptWork() => SearchSuccessful = false;

    public override void PostConvertToCaravan(Caravan caravan)
    {
        if (!SearchSuccessful)
        {
            Messages.Message(
                text: "OARO_WolfDisasterGossipPoint_SearchFailed".Translate(),
                lookTargets: caravan,
                def: MessageTypeDefOf.NeutralEvent,
                historical: false);
            return;
        }

        if (Rand.Chance(0.24f))
        {
            TriggerGossipEvent(caravan);
        }

        DiaNode rootNode = gossipType switch
        {
            GossipType.Intuition => IntuitionRootNode(caravan),
            GossipType.Habitat => HabitatRootNode(caravan),
            GossipType.AbandonedWolfDen => AbandonedWolfDenRootNode(caravan),
            GossipType.DeliberateTraces => DeliberateTracesRootNode(caravan),
            _ => null
        };

        if (rootNode is null)
        {
            return;
        }

        Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(rootNode, Faction));
    }

    private void TriggerGossipEvent(Caravan caravan)
    {
        if (Rand.Bool)
        {
            GainIntelligence();
            Find.LetterStack.ReceiveLetter(
                label: "OARO_WolfDisasterGossipPoint_WitnessLabel".Translate(),
                text: "OARO_WolfDisasterGossipPoint_WitnessText".Translate(),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: caravan,
                relatedFaction: Faction,
                quest: quest);
        }
        else
        {
            List<Thing> rewardThings = [];
            Thing t = ThingMaker.MakeThing(ThingDefOf.Cloth);
            t.stackCount = Rand.Range(180, 220);
            rewardThings.Add(t);

            t = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamedSilentFail("RawRice"));
            t.stackCount = Rand.Range(360, 440);
            rewardThings.Add(t);

            t = ThingMaker.MakeThing(ThingDefOf.Silver);
            t.stackCount = Rand.Range(30, 60);
            rewardThings.Add(t);

            foreach (Thing item in rewardThings)
            {
                CaravanInventoryUtility.GiveThing(caravan, item);
            }

            Find.LetterStack.ReceiveLetter(
                label: "OARO_WolfDisasterGossipPoint_FrightenedWitnessLabel".Translate(),
                text: "OARO_WolfDisasterGossipPoint_FrightenedWitnessText".Translate(GenLabel.ThingsLabel(rewardThings).Named(KeyLibrary_FormatArgName.ThingsInfo)),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: caravan,
                relatedFaction: Faction,
                quest: quest);
        }
    }

    private DiaNode IntuitionRootNode(Caravan caravan)
    {
        (Pawn maxSkillPawn, _) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(caravan.PawnsListForReading, SkillDefOf.Animals);
        DiaNode rootNode = new("OARO_WolfDisasterGossipPoint_IntuitionRoot".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN)));

        foreach (IntuitionDirection direction in EnumUtility.GetValues<IntuitionDirection>())
        {
            DiaOption opt = new($"OARO_WolfDisasterGossipPoint_IntuitionDirection_{direction}".Translate())
            {
                resolveTree = true
            };
            if (direction == rightIntuitionDirection)
            {
                opt.action = delegate
                {
                    this.SafeDestroy();
                    GainIntelligence();
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_WolfDisasterGossipPoint_Intuition_RightDirection".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN)),
                        Faction));
                };
            }
            else
            {
                opt.action = delegate
                {
                    ThingDef berriesDef = DefDatabase<ThingDef>.GetNamedSilentFail("RawBerries");
                    Thing berries = ThingMaker.MakeThing(berriesDef);
                    berries.stackCount = 300;
                    CaravanInventoryUtility.GiveThing(caravan, berries);

                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_WolfDisasterGossipPoint_Intuition_WrongDirection".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN), 300.Named(KeyLibrary_FormatArgName.Count)),
                        Faction));
                };
            }
            rootNode.options.Add(opt);
        }

        return rootNode;
    }

    private DiaNode HabitatRootNode(Caravan caravan)
    {
        (Pawn maxSkillPawn, int maxSkillLevel) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(caravan.PawnsListForReading, SkillDefOf.Animals);
        DiaNode rootNode = new("OARO_WolfDisasterGossipPoint_HabitatRoot".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN)));
        DiaOption passOpt = new("OARO_WolfDisasterGossipPoint_Habitat_Pass".Translate())
        {
            action = PassAction,
            resolveTree = true
        };
        rootNode.options.Add(passOpt);

        DiaOption notPassOpt = new("OARO_WolfDisasterGossipPoint_Habitat_NotPass".Translate())
        {
            action = delegate
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                    text: "OARO_WolfDisasterGossipPoint_HabitatNotPass_Reply".Translate(),
                    faction: Faction));
            },
            resolveTree = true
        };

        rootNode.options.Add(notPassOpt);

        return rootNode;

        void PassAction()
        {
            if (maxSkillLevel >= 15)
            {
                this.SafeDestroy();
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    p.skills?.Learn(SkillDefOf.Animals, 1000f);
                }
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                    text: "OARO_WolfDisasterGossipPoint_HabitatPass_Sucess".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN))
                        + "\n\n"
                        + "OAFrame_AllCarvanMemberGetXP".Translate(SkillDefOf.Animals.Named(KeyLibrary_FormatArgName.SKILL), 1000.Named(KeyLibrary_FormatArgName.Count)),
                    faction: Faction));

            }
            else
            {
                this.SafeDestroy();
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    p.skills?.Learn(SkillDefOf.Animals, 1000f);
                }

                Find.WindowStack.Add(OAFrame_DiaUtility.ConfirmDiaNodeTreeWithFactionInfo(
                    text: "OARO_WolfDisasterGossipPoint_HabitatPass_Fail".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN))
                        + "\n\n"
                        + "OAFrame_AllCarvanMemberGetXP".Translate(SkillDefOf.Animals.Named(KeyLibrary_FormatArgName.SKILL), 1000.Named(KeyLibrary_FormatArgName.Count)),
                    faction: Faction,
                    acceptText: "Confirm".Translate(),
                    acceptAction: delegate
                    {
                        LongEventHandler.QueueLongEvent(delegate
                        {
                            Pawn pawn = caravan.PawnsListForReading[0];
                            Map map = CaravanIncidentUtility.GetOrGenerateMapForIncident(caravan, new IntVec3(250, 1, 250), WorldObjectDefOf.Ambush);
                            MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out IntVec3 playerStartingSpot, out IntVec3 second);
                            CaravanEnterMapUtility.Enter(caravan, map, p => CellFinder.RandomSpawnCellForPawnNear(playerStartingSpot, map), CaravanDropInventoryMode.DoNotDrop, draftColonists: true);

                            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("ManhunterPack");
                            IncidentParms parms = new()
                            {
                                target = map,
                                points = Mathf.Max(1000f, StorytellerUtility.DefaultThreatPointsNow(map) * 2f),
                                forced = true,
                            };
                            incidentDef.Worker.TryExecute(parms);
                            Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                            CameraJumper.TryJump(pawn);
                        }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);

                    }));
            }

            GainIntelligence();
        }
    }

    private DiaNode AbandonedWolfDenRootNode(Caravan caravan)
    {
        return OAFrame_DiaUtility.ConfirmDiaNode(
             text: "OARO_WolfDisasterGossipPoint_AbandonedWolfDenRoot".Translate()
                 + "\n\n"
                 + "OAFrame_AllCarvanMemberGetXP".Translate(SkillDefOf.Animals.Named(KeyLibrary_FormatArgName.SKILL), 2000.Named(KeyLibrary_FormatArgName.Count)),
             acceptText: "Confirm".Translate(),
             acceptAction: delegate
             {
                 this.SafeDestroy();
                 GainIntelligence(4);
                 foreach (Pawn p in caravan.PawnsListForReading)
                 {
                     p.skills?.Learn(SkillDefOf.Animals, 2000f);
                 }
             });
    }

    private DiaNode DeliberateTracesRootNode(Caravan caravan)
    {
        (Pawn maxSkillPawn, int maxSkillLevel) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(caravan.PawnsListForReading, SkillDefOf.Animals);
        DiaNode rootNode = new("OARO_WolfDisasterGossipPoint_DeliberateTracesRoot".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN)));
        float successChance = 1f;

        DiaOption checkOpt = new("OARO_WolfDisasterGossipPoint_DeliberateTraces_Check".Translate())
        {
            action = delegate
            {
                if (Rand.Chance(successChance))
                {
                    this.SafeDestroy();
                    GainIntelligence(1);
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_WolfDisasterGossipPoint_DeliberateTraces_Success".Translate(),
                        faction: Faction));
                }
                else
                {
                    GainIntelligence(-1);
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_WolfDisasterGossipPoint_DeliberateTraces_Fail".Translate(),
                        faction: Faction));
                }
            },
            resolveTree = true
        };

        rootNode.options.Add(checkOpt);

        DiaOption notCheckOpt = new("OARO_WolfDisasterGossipPoint_DeliberateTraces_NotCheck".Translate())
        {
            action = delegate
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                         text: "OARO_WolfDisasterGossipPoint_DeliberateTraces_NotCheckReply".Translate(),
                         faction: Faction));
            },
            resolveTree = true
        };
        rootNode.options.Add(notCheckOpt);

        return rootNode;
    }
}
