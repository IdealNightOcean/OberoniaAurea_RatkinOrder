using RimWorld;
using RimWorld.QuestGen;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_CheckSkillLevel : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<string> outSignalSuccess;
    public SlateRef<string> outSignalFail;
    public SlateRef<SkillDef> skill;
    public SlateRef<int> minLevel;
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
                InSignal = inSignal.GetValue(slate) ?? QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                OutSignalSuccess = outSignalFail.GetValue(slate),
                OutSignalFail = outSignalSuccess.GetValue(slate),
                MinLevel = minLevel.GetValue(slate),
                Pawn = pawn.GetValue(slate)
            };
            QuestGen.quest.AddPart(questPart_CheckSkillLevel);
        }
    }
}

public class QuestPart_CheckSkillLevel : QuestPart
{
    public string InSignal;
    public string OutSignalSuccess;
    public string OutSignalFail;

    public SkillDef Skill;
    public int MinLevel;
    public Pawn Pawn;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignal, "InSignal");
        Scribe_Values.Look(ref OutSignalSuccess, "OutSignalSuccess");
        Scribe_Values.Look(ref OutSignalFail, "OutSignalFail");

        Scribe_Defs.Look(ref Skill, "Skill");
        Scribe_Values.Look(ref MinLevel, "MinLevel", 0);
        Scribe_References.Look(ref Pawn, nameof(Pawn));
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignal = null;
        OutSignalSuccess = null;
        OutSignalFail = null;
        Skill = null;
        Pawn = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == InSignal)
        {
            if (Pawn is not null)
            {
                if (Pawn.skills is null || Pawn.skills.GetSkill(Skill).GetLevel() < MinLevel)
                {
                    if (!String.IsNullOrEmpty(OutSignalFail))
                    {
                        Find.SignalManager.SendSignal(new Signal(OutSignalFail));
                    }
                }
                else if (!String.IsNullOrEmpty(OutSignalSuccess))
                {
                    Find.SignalManager.SendSignal(new Signal(OutSignalSuccess));
                }
            }
        }
    }
}