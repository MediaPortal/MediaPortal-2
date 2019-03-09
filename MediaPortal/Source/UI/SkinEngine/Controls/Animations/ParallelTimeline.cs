#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class ParallelTimeline: TimelineGroup
  {
    public override void Start(TimelineContext context, uint timePassed)
    {
      base.Start(context, timePassed);
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        Children[i].Start(tgc[i], timePassed);
    }

    internal override void DoAnimation(TimelineContext context, uint reltime)
    {
      try
      {
        base.DoAnimation(context, reltime);
        TimelineGroupContext tgc = (TimelineGroupContext) context;
        for (int i = 0; i < Children.Count; i++)
          // Call Animate at the children, because the children have to do their own time management
          Children[i].Animate(tgc[i], reltime);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error executing animation", ex);
      }
    }

    public override bool HasEnded(TimelineContext context)
    {
      if (base.HasEnded(context))
        return true;
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      return !Children.Where((t, i) => !t.HasEnded(tgc[i])).Any();
    }
  }
}
