using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public abstract class UICacheBase
{
    public bool IsReady { get; protected set; }

    public void Refresh()
    {
        if (!IsReady)
        {
            RefreshInner();
            IsReady = true;
        }
    }

    protected abstract void RefreshInner();
}