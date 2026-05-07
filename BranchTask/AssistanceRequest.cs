using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AssistanceRequest : IExposable
{
    public enum RequestType : byte
    {
        BasicWork,
        SkillRequired,
        StatValueRequired,
        KnightVirtueRequired,
        AcademicRequired
    }

    private RequestType requestType;
    public RequestType Type => requestType;

    private string label;
    public string Title => label;

    private string requirementDesc;
    public string RequirementDesc => requirementDesc;

    private int progressCeiling;
    public int ProgressCeiling
    {
        get => progressCeiling;
        set => progressCeiling = value;
    }

    private int curProgress;
    public int CurProgress => curProgress;
    public float ProgressRatio => progressCeiling > 0 ? (float)curProgress / progressCeiling : 0f;

    private bool participating;
    public bool Participating
    {
        get => participating;
        set => participating = value;
    }

    private bool completed;
    public bool Completed => completed;

    private KnightAcademicDef relatedAcademic;
    public KnightAcademicDef RelatedAcademic
    {
        get => relatedAcademic;
        set => relatedAcademic = value;
    }

    private SkillDef relatedSkill;
    public SkillDef RelatedSkill
    {
        get => relatedSkill;
        set => relatedSkill = value;
    }

    private float skillLevelRequired;
    public float SkillLevelRequired
    {
        get => skillLevelRequired;
        set => skillLevelRequired = Mathf.Max(0, value);
    }

    private StatDef relatedStat;
    public StatDef RelatedStat
    {
        get => relatedStat;
        set => relatedStat = value;
    }

    private float statValueRequired;
    public float StatValueRequired
    {
        get => statValueRequired;
        set => statValueRequired = value;
    }

    private KnightVirtueDef relatedVirtue;
    public KnightVirtueDef RelatedVirtue
    {
        get => relatedVirtue;
        set => relatedVirtue = value;
    }

    public AssistanceRequest() { }
    public AssistanceRequest(RequestType type) { requestType = type; }

    public void Initialize(string label, string reqDesc)
    {
        this.label = label;
        this.requirementDesc = reqDesc;
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
        Scribe_Values.Look(ref label, nameof(label));
        Scribe_Values.Look(ref requirementDesc, nameof(requirementDesc));
        Scribe_Values.Look(ref progressCeiling, nameof(progressCeiling), 0);
        Scribe_Values.Look(ref curProgress, nameof(curProgress), 0);
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
