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
using Presentation.SkinEngine;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Animations;
namespace Presentation.SkinEngine
{
  public class Animator
  {
    List<StoryboardContext> _runningAnimations;

    /// <summary>
    /// Initializes a new instance of the <see cref="Animator"/> class.
    /// </summary>
    public Animator()
    {
      _runningAnimations = new List<StoryboardContext>();
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <param name="board">The board.</param>
    /// <returns></returns>
    StoryboardContext GetContext(Storyboard board, UIElement element)
    {
      foreach (StoryboardContext context in _runningAnimations)
      {
        if (context.Storyboard == board && context.Element == element) return context;
      }
      return null;
    }

    /// <summary>
    /// Starts the storyboard.
    /// </summary>
    /// <param name="board">The board.</param>
    public void StartStoryboard(Storyboard board, UIElement element)
    {
      lock (_runningAnimations)
      {

        if (null == GetContext(board, element))
        {
          StoryboardContext context = new StoryboardContext(board, element);
          _runningAnimations.Add(context);
          context.Setup(element);
          context.Start(SkinContext.TimePassed);
        }
      }
    }

    /// <summary>
    /// Stops the storyboard.
    /// </summary>
    /// <param name="board">The board.</param>
    public void StopStoryboard(Storyboard board, UIElement element)
    {
      lock (_runningAnimations)
      {
        StoryboardContext context = GetContext(board, element);
        if (context == null) return;
        _runningAnimations.Remove(context);
        context.Stop();
      }
    }

    /// <summary>
    /// Animates any timelines 
    /// </summary>
    public virtual void Animate()
    {
      if (_runningAnimations.Count == 0) return;
      List<StoryboardContext> stoppedAnimations = new List<StoryboardContext>();
      lock (_runningAnimations)
      {
        foreach (StoryboardContext line in _runningAnimations)
        {
          line.Animate(SkinContext.TimePassed);
          if (line.IsStopped)
            stoppedAnimations.Add(line);
        }
        foreach (StoryboardContext line in stoppedAnimations)
        {
          line.Stop();
          _runningAnimations.Remove(line);
        }
      }
    }

    public void StopAll()
    {
      foreach (StoryboardContext line in _runningAnimations)
      {
        line.Stop();
      }
      _runningAnimations.Clear();
    }
  }
}
