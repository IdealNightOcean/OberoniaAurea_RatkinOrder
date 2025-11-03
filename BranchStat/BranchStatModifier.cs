using System.Xml;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatModifier
{
    public BranchStatDef statDef;
    public float value;

    public BranchStatModifier() { }

    public BranchStatModifier(BranchStatDef statDef, float value)
    {
        this.statDef = statDef;
        this.value = value;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "statDef", xmlRoot);
        value = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
    }

    public override string ToString()
    {
        if (statDef is null)
        {
            return "(null BranchStat)";
        }
        return statDef.defName + "-" + value;
    }
}