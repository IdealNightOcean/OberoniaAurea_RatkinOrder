using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchConstructionDef : Def
{
    /// <summary>
    /// 图标路径
    /// </summary>
    [NoTranslate]
    protected string iconPath;
    protected Texture2D iconTexture;
    public Texture2D IconTexture
    {
        get
        {
            if (iconTexture is null)
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    return null;
                }
                iconTexture = ContentFinder<Texture2D>.Get(iconPath);
            }
            return iconTexture;
        }
    }

    /// <summary>
    /// 拓展图标路径
    /// </summary>
    protected Texture2D expandingIconTexture;
    public Texture2D ExpandingIconTexture
    {
        get
        {
            if (expandingIconTexture is null)
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    return null;
                }
                expandingIconTexture = ContentFinder<Texture2D>.Get(iconPath + "_Expand");
            }
            return expandingIconTexture;
        }
    }
}