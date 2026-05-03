using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequest : IExposable
{
    public enum RequestType : byte
    {
        BasicWork,
        SkillRequired,
        AttributeRequired,
        VirtueRequired,
        AcademicRequired
    }

    private RequestType requestType;
    public RequestType Type => requestType;

    private string title;
    public string Title => title;

    private string requirementDesc;
    public string RequirementDesc => requirementDesc;

    private int progressCeiling;
    public int ProgressCeiling => progressCeiling;

    private int curProgress;
    public int CurProgress => curProgress;
    public float ProgressRatio => progressCeiling > 0 ? (float)curProgress / progressCeiling : 0f;

    private float dailyProgress;
    public float DailyProgress => dailyProgress;

    private bool participating;
    public bool Participating
    {
        get => participating;
        set => participating = value;
    }

    private bool completed;
    public bool Completed => completed;

    private KnightAcademicDef relatedAcademic;
    public KnightAcademicDef RelatedAcademic => relatedAcademic;

    private SkillDef relatedSkill;
    public SkillDef RelatedSkill => relatedSkill;

    private int skillLevelRequired;
    public int SkillLevelRequired => skillLevelRequired;

    private StatDef relatedStat;
    public StatDef RelatedStat => relatedStat;

    private float statValueRequired;
    public float StatValueRequired => statValueRequired;

    private KnightVirtueDef relatedVirtue;
    public KnightVirtueDef RelatedVirtue => relatedVirtue;

    public AssistanceRequest() { }

    public void Initialize(RequestType type,
                           string title,
                           string reqDesc,
                           int ceiling,
                           float daily,
                           KnightAcademicDef academic = null,
                           SkillDef skill = null,
                           int skillLvl = 0,
                           StatDef stat = null,
                           float statVal = 0f,
                           KnightVirtueDef virtue = null)
    {
        requestType = type;
        this.title = title;
        requirementDesc = reqDesc;
        progressCeiling = ceiling;
        dailyProgress = daily;
        relatedAcademic = academic;
        relatedSkill = skill;
        skillLevelRequired = skillLvl;
        relatedStat = stat;
        statValueRequired = statVal;
        relatedVirtue = virtue;
    }

    public void AddProgress(float amount)
    {
        if (completed) return;
        curProgress += (int)amount;
        if (curProgress >= progressCeiling)
        {
            curProgress = progressCeiling;
            completed = true;
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref requestType, nameof(requestType));
        Scribe_Values.Look(ref title, nameof(title));
        Scribe_Values.Look(ref requirementDesc, nameof(requirementDesc));
        Scribe_Values.Look(ref progressCeiling, nameof(progressCeiling), 0);
        Scribe_Values.Look(ref curProgress, nameof(curProgress), 0);
        Scribe_Values.Look(ref dailyProgress, nameof(dailyProgress), 0f);
        Scribe_Values.Look(ref participating, nameof(participating), false);
        Scribe_Values.Look(ref completed, nameof(completed), false);
        Scribe_Defs.Look(ref relatedAcademic, nameof(relatedAcademic));
        Scribe_Defs.Look(ref relatedSkill, nameof(relatedSkill));
        Scribe_Values.Look(ref skillLevelRequired, nameof(skillLevelRequired), 0);
        Scribe_Defs.Look(ref relatedStat, nameof(relatedStat));
        Scribe_Values.Look(ref statValueRequired, nameof(statValueRequired), 0f);
        Scribe_Defs.Look(ref relatedVirtue, nameof(relatedVirtue));
    }
}
