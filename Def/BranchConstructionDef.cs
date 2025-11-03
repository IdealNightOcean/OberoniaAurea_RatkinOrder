using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchConstructionDef : Def
{
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

    [NoTranslate]
    protected string expandingIconPath;

    protected Texture2D expandingIconTexture;
    public Texture2D ExpandingIconTexture
    {
        get
        {
            if (expandingIconTexture is null)
            {
                if (string.IsNullOrEmpty(expandingIconPath))
                {
                    return null;
                }
                expandingIconTexture = ContentFinder<Texture2D>.Get(expandingIconPath);
            }
            return expandingIconTexture;
        }
    }
}
