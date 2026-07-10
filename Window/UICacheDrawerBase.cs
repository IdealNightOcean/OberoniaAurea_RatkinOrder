using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public abstract class UICacheDrawerBase<T> where T : UICacheBase
{
    public T CacheData { get; }


    public void Draw(Rect inRect)
    {
        CacheData.Refresh();
        DrawInner(inRect);
    }

    public abstract void DrawInner(Rect inRect);

}