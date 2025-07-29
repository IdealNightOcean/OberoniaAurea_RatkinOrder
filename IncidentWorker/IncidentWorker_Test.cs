using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

internal class IncidentWorker_Test : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return true;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Slate slate = new();
        slate.Set("mercyQuest", OARO_QuestScriptDefOf.OARO_Mercy_PastureFlu);
        OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_MercyPre_HelpSeeker, slate, forced: true);

        return true;
    }
}
