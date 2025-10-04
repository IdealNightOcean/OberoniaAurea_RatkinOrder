using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal sealed class QuestNode_TownUnderConstructionWatcher : QuestNode
{
    public SlateRef<WorldObject_TownUnderConstruction> town;
    public SlateRef<string> inSignalSettled;

    public SlateRef<string> outSignalFailed;
    public SlateRef<string> outSignalSecceed;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        throw new NotImplementedException();
    }
}


internal sealed class QuestPart_TownUnderConstructionWatcher : QuestPart
{
    public WorldObject_TownUnderConstruction Town;
    public string InSignalSettled;
    public string OutSignalFailed;
    public string OutSignalSecceed;

    private float populationMulti = 1f;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == QuestPart_CliquesManager.SignalCliqueActived(quest))
        {
            if (signal.args.TryGetArg("SUBJECT", out QuestClique clique))
            {
                switch (clique.Key)
                {
                    case "TravelRatkin":
                        {
                            populationMulti = 1.2f;
                            break;
                        }
                    case "SeniorResident":
                        {
                            Town.Population += (100f * populationMulti);
                            break;
                        }
                    case "VillageResident":
                        {
                            Town.Population += (200f * populationMulti);
                            break;
                        }
                    case "RemoteResident":
                        {
                            Town.Population += (150f * populationMulti);
                            break;
                        }
                    case "FramerResident":
                        {
                            Town.Population += (250f * populationMulti);
                            break;
                        }
                    default: break;
                }
            }
        }

        if (signal.tag == InSignalSettled)
        {
            if (Town.Population < 1600f)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalFailed));
                return;
            }
            Find.SignalManager.SendSignal(new Signal(OutSignalSecceed));

            if (Town.ConstructionScale >= 3)
            {

                if (Town.ConstructionScale >= 4)
                {

                }
                else
                {
                    
                }
            }
        }
    }
}