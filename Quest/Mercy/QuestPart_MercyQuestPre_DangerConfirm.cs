using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_MercyQuestPre_DangerConfirm : QuestPart
{
    public string InSignal;
    public string OutSignalForceInterrupt;

    public Map Map;
    public Faction SubFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignal, nameof(InSignal));
        Scribe_Values.Look(ref OutSignalForceInterrupt, nameof(OutSignalForceInterrupt));

        Scribe_References.Look(ref Map, nameof(Map));
        Scribe_References.Look(ref SubFaction, nameof(SubFaction));
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignal = null;
        OutSignalForceInterrupt = null;

        Map = null;
        SubFaction = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == InSignal)
        {
            if (!CheckSafeVisitability(Map, SubFaction, out string notSafeReason))
            {
                Dialog_NodeTree nodeTree = OAFrame_DiaUtility.ConfirmDiaNodeTree(
                   text: "OARO_MercyQuestPre_DangerConfirmInfo".Translate(notSafeReason.Named("NotSafeReason")),
                   acceptText: "OARO_MercyQuestPre_EnsureSafe".Translate(),
                   acceptAction: null,
                   rejectText: "OARO_MercyQuestPre_WarnToLeave".Translate(),
                   rejectAction: delegate
                   {
                       MercyQuestHandler.Instance.Notify_MercyQuestInterrupted();
                       Find.SignalManager.SendSignal(new Signal(OutSignalForceInterrupt));
                   });

                Find.WindowStack.Add(nodeTree);
            }
        }
    }

    private static bool CheckSafeVisitability(Map map, Faction faction, out string reasons)
    {
        bool result = true;
        StringBuilder reasonBuilder = new(32);

        if (map.weatherManager.curWeather.favorability == Favorability.VeryBad)
        {
            result = false;
            reasonBuilder.AppendLine($"- {"Weather".Translate()}: {map.weatherManager.curWeather.LabelCap}");
        }

        Faction playerFaction = Faction.OfPlayer;
        Pawn[] allPotentialPawns = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && !p.IsPrisoner && !p.Downed && !p.InContainerEnclosed).ToArray();

        IEnumerable<Faction> allHostileFactions = allPotentialPawns.Where(p => p.Faction is not null)
                                                                   .Select(p => p.Faction)
                                                                   .Where(f => f.HostileTo(playerFaction) || f.HostileTo(faction))
                                                                   .Distinct();
        foreach (Faction hostileFaction in allHostileFactions)
        {
            result = false;
            reasonBuilder.AppendLine($"- {hostileFaction.NameColored}: {hostileFaction.def.pawnsPlural.CapitalizeFirst()}");
        }
        foreach (IGrouping<MentalStateDef, Pawn> group in allPotentialPawns.Where(p => p.InAggroMentalState)
                                                                           .GroupBy(p => p.MentalStateDef))
        {
            result = false;
            if (group.Skip(1).Any())
            {
                reasonBuilder.AppendLine($"- {group.First().GetKindLabelPlural()} ({group.First().MentalStateDef.LabelCap})");
            }
            else
            {
                reasonBuilder.AppendLine($"- {group.First().LabelShort} ({group.First().MentalStateDef.LabelCap})");
            }
        }

        reasons = reasonBuilder.ToString();
        return result;
    }

}