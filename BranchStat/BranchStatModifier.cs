using System.Xml;
using UnityEngine;
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

    public string ToStringOffset()
    {
        if (statDef is null)
        {
            return "(null BranchStat)";
        }
        if (statDef.statType == BranchStatDef.StatType.Percent)
        {
            return statDef.label + ": " + value.ToStringPercentSigned("0.##").Colorize((statDef.reverse ^ value < 0f) ? ColorLibrary.RedReadable : Color.green);
        }
        else
        {
            return statDef.label + ": " + value.ToStringWithSign("0.##").Colorize((statDef.reverse ^ value < 0f) ? ColorLibrary.RedReadable : Color.green);
        }
    }

    public string ToStringFactor()
    {
        if (statDef is null)
        {
            return "(null BranchStat)";
        }
        if (statDef.statType == BranchStatDef.StatType.Percent)
        {
            return statDef.label + ": ×" + value.ToStringPercent("0.##").Colorize((statDef.reverse ^ value < 1f) ? ColorLibrary.RedReadable : Color.green);
        }
        else
        {
            return statDef.label + ": ×" + value.ToString("0.##").Colorize((statDef.reverse ^ value < 1f) ? ColorLibrary.RedReadable : Color.green);
        }
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