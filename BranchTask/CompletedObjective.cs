using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompletedObjective : IExposable
{
    public enum ObjectiveType : byte
    {
        Normal,
        Assistance
    }

    private ObjectiveType objectiveType;
    public ObjectiveType Type => objectiveType;

    private KnightChivalryDef medalType;
    public KnightChivalryDef MedalType => medalType;

    private int assistanceCount;
    public int AssistanceCount => assistanceCount;

    public bool IsAssistance => objectiveType == ObjectiveType.Assistance;

    public CompletedObjective() { }

    public CompletedObjective(ObjectiveType type, KnightChivalryDef medalType, int assistanceCount = 0)
    {
        objectiveType = type;
        this.medalType = medalType;
        this.assistanceCount = assistanceCount;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref objectiveType, nameof(objectiveType));
        Scribe_Defs.Look(ref medalType, nameof(medalType));
        Scribe_Values.Look(ref assistanceCount, nameof(assistanceCount), 0);
    }
}
