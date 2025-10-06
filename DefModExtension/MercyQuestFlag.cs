using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestFlag : DefModExtension
{
    public override void ResolveReferences(Def parentDef)
    {
        if (parentDef is QuestScriptDef scriptDef)
        {
            MercyQuestDataBase.Add(scriptDef);
        }
        else
        {
            Log.Error($"MercyQuestFlag cannot be used to mark {parentDef.GetType()}; it only supports QuestScriptDef.");
        }
    }
}