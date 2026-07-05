using OberoniaAurea_Frame;
using System.Xml;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatModifier<T> where T : OAROStatDefBase
{
    public T statDef;
    public float value;

    public StatModifier() { }

    public StatModifier(T statDef, float value)
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
            return statDef.label + ": " + OAFrame_TextUtility.ColoredPercentString(value, includeSign: true, reverse: statDef.reverse);
        }
        else
        {
            return statDef.label + ": " + OAFrame_TextUtility.ColoredFloatString(value, includeSign: true, reverse: statDef.reverse);
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
            return statDef.label + ":" + $" ×{value.ToStringPercent("0.##")}".Colorize((statDef.reverse ^ value < 1f) ? ColorLibrary.RedReadable : Color.green);
        }
        else
        {
            return statDef.label + ":" + $" ×{value.ToStringPercent("0.##")}".Colorize((statDef.reverse ^ value < 1f) ? ColorLibrary.RedReadable : Color.green);
        }
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(statDef), xmlRoot);
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