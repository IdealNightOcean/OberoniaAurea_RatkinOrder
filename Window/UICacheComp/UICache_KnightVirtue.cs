using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OberoniaAurea.RatkinOrder.Window;

public class UICache_KnightVirtue
{
    public ResidentKnight Knight { get; }

    public KnightVirtueHandler VirtueHandler { get; }

    public List<UICache_KnightAcademic> academicsUICache;

    public List<UICache_MentorshipStudent> studentUICache;



}