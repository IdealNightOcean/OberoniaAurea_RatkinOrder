using System;
using System.Xml;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public class PathedTexture2D
{
    [NoTranslate]
    protected string path;

    private Texture2D texture;
    public Texture2D Texture
    {
        get
        {
            if (texture is null)
            {
                if (String.IsNullOrEmpty(path))
                {
                    texture = BaseContent.BadTex;
                }
                texture = ContentFinder<Texture2D>.Get(path) ?? BaseContent.BadTex;
            }
            return texture;
        }
    }

    public PathedTexture2D() { }
    public PathedTexture2D(string path)
    {
        this.path = path;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        path = ParseHelper.FromString<string>(xmlRoot.FirstChild.Value);
    }
}

public class PathedTexture2DWithExpanded : PathedTexture2D
{
    private Texture2D expandedTexture;
    public Texture2D ExpandedTexture
    {
        get
        {
            if (expandedTexture is null)
            {
                if (String.IsNullOrEmpty(path))
                {
                    expandedTexture = BaseContent.BadTex;
                }
                expandedTexture = ContentFinder<Texture2D>.Get(path + "_Expand") ?? BaseContent.BadTex;
            }
            return expandedTexture;
        }
    }
}
