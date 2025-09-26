using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_TriggerMercyQuest : QuestPart
{
    public string InSignalAccept;
    public string InSignalReject;

    private bool canTriggered = true;

    public QuestScriptDef MmercyQuestDef;

    public Faction SubFaction;
    public Faction ParentFaction;

    public Pawn HelpSeeker;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref InSignalAccept, "InSignalAccept");
        Scribe_Values.Look(ref InSignalReject, "InSignalReject");

        Scribe_Values.Look(ref canTriggered, "canTriggered", defaultValue: true);

        Scribe_Defs.Look(ref MmercyQuestDef, "MmercyQuestDef");

        Scribe_References.Look(ref SubFaction, "SubFaction");
        Scribe_References.Look(ref ParentFaction, "ParentFaction");

        Scribe_References.Look(ref HelpSeeker, "HelpSeeker");
    }

    public override void Cleanup()
    {
        InSignalAccept = null;
        InSignalReject = null;

        MmercyQuestDef = null;

        SubFaction = null;
        ParentFaction = null;

        HelpSeeker = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (canTriggered)
        {
            if (signal.tag == InSignalAccept)
            {
                canTriggered = false;
                TryTriggerQuest();
            }
            else if (signal.tag == InSignalReject)
            {
                canTriggered = false;
            }
        }
    }

    protected bool TryTriggerQuest()
    {
        Slate slate = GenerateQuestSlate();
        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, MmercyQuestDef, slate, forced: true);
    }

    protected virtual Slate GenerateQuestSlate()
    {
        Slate slate = new();

        slate.Set(KeyLibrary_SlateStoreAs.HelpSeeker, HelpSeeker);
        slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFaction, SubFaction);

        if (ParentFaction is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFaction, ParentFaction);
        }

        return slate;
    }
}
