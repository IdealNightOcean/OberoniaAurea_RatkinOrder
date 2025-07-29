using System.Xml;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatModifier
{
    public BranchStatDef statDef;
    public BranchStatTransformer statTransformer;
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "statDef", xmlRoot);
        statTransformer = DirectXmlToObject.ObjectFromXml<BranchStatTransformer>(xmlRoot, doPostLoad: false);
    }

    public void PostLoad()
    {
        statTransformer.EnsureFactorMinMagnitude();
    }
}