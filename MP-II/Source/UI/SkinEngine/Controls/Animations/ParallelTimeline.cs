#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion


namespace MediaPortal.SkinEngine.Controls.Animations
{
  public class ParallelTimeline: TimelineGroup
  {
    public ParallelTimeline() { }

    public override void Start(TimelineContext context, uint timePassed)
    {
      base.Start(context, timePassed);
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        Children[i].Start(tgc[i], timePassed);
    }

    internal override void DoAnimation(TimelineContext context, uint reltime)
    {
      base.DoAnimation(context, reltime);
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        // Call Animate at the children, because the children have to do their own time management
        Children[i].Animate(tgc[i], reltime);
    }

    public override bool HasEnded(TimelineContext context)
    {
      if (base.HasEnded(context))
        return true;
      TimelineGroupContext tgc = (TimelineGroupContext) context;
      for (int i = 0; i < Children.Count; i++)
        if (!Children[i].HasEnded(tgc[i])) return false;
      return true;
    }
  }
}
