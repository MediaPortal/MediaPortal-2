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

using System.Collections.Generic;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Animations;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine
{
  public class AnimationContext
  {
    protected Timeline _timeline;
    protected TimelineContext _timelineContext;

    public Timeline Timeline
    {
      get { return _timeline; }
      set { _timeline = value; }
    }

    public TimelineContext TimelineContext
    {
      get { return _timelineContext; }
      set { _timelineContext = value; }
    }
  }

  public class Animator
  {
    protected List<AnimationContext> _runningAnimations;

    public Animator()
    {
      _runningAnimations = new List<AnimationContext>();
    }

    protected AnimationContext GetContext(Timeline line, UIElement element)
    {
      foreach (AnimationContext context in _runningAnimations)
        if (context.Timeline == line && context.TimelineContext.VisualParent == element) return context;
      return null;
    }

    public void StartStoryboard(Storyboard board, UIElement element)
    {
      lock (_runningAnimations)
      {
        if (GetContext(board, element) == null)
        {
          AnimationContext context = new AnimationContext();
          context.Timeline = board;
          context.TimelineContext = board.CreateTimelineContext(element);
          board.Setup(context.TimelineContext);

          _runningAnimations.Add(context);
          board.Start(context.TimelineContext, SkinContext.TimePassed);
        }
      }
    }

    public void StopStoryboard(Storyboard board, UIElement element)
    {
      lock (_runningAnimations)
      {
        AnimationContext context = GetContext(board, element);
        if (context == null) return;
        _runningAnimations.Remove(context);
        context.Timeline.Stop(context.TimelineContext);
      }
    }

    /// <summary>
    /// Animates all timelines. This method will be called periodically to do all animation work.
    /// </summary>
    public void Animate()
    {
      if (_runningAnimations.Count == 0) return;
      List<AnimationContext> stoppedAnimations = new List<AnimationContext>();
      lock (_runningAnimations)
      {
        foreach (AnimationContext ac in _runningAnimations)
        {
          ac.Timeline.Animate(ac.TimelineContext, SkinContext.TimePassed);
          if (ac.Timeline.IsStopped(ac.TimelineContext))
            stoppedAnimations.Add(ac);
        }
        foreach (AnimationContext ac in stoppedAnimations)
        {
          ac.Timeline.Stop(ac.TimelineContext);
          _runningAnimations.Remove(ac);
        }
      }
    }

    public void StopAll()
    {
      foreach (AnimationContext ac in _runningAnimations)
        ac.Timeline.Stop(ac.TimelineContext);
      _runningAnimations.Clear();
    }
  }
}
