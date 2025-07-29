using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_CheckSkillLevel : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<string> outSignalSuccess;
    public SlateRef<string> outSignalFail;
    public SlateRef<SkillDef> skill;
    public SlateRef<float> level;
    public SlateRef<Pawn> pawn;

    protected override bool TestRunInt(Slate slate)
    {
        return skill.GetValue(slate) is not null;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (skill.GetValue(slate) is not null && pawn.GetValue(slate) is not null)
        {
            QuestPart_CheckSkillLevel questPart_CheckSkillLevel = new()
            {
                inSignal = inSignal.GetValue(slate) ?? QuestGen.slate.Get<string>("inSignal"),
                outSignalSuccess = outSignalFail.GetValue(slate),
                outSignalFail = outSignalSuccess.GetValue(slate),
                level = level.GetValue(slate),
                pawn = pawn.GetValue(slate)
            };
            QuestGen.quest.AddPart(questPart_CheckSkillLevel);
        }
    }
}

public class QuestPart_CheckSkillLevel : QuestPart
{
    public string inSignal;
    public string outSignalSuccess;
    public string outSignalFail;
    public SkillDef skill;
    public float level;
    public Pawn pawn;


    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == inSignal)
        {
            if (pawn is not null)
            {
                if (pawn.skills is null || pawn.skills.GetSkill(skill).GetLevel() < level)
                {
                    if (!outSignalFail.NullOrEmpty())
                    {
                        Find.SignalManager.SendSignal(new Signal(outSignalFail));
                    }
                }
                else if (!outSignalSuccess.NullOrEmpty())
                {
                    Find.SignalManager.SendSignal(new Signal(outSignalSuccess));
                }
            }
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignal = string.Empty;
        outSignalSuccess = string.Empty;
        outSignalFail = string.Empty;
        skill = null;
        pawn = null;
    }
}