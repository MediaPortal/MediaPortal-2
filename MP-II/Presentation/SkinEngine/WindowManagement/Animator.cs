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

using System;
using System.Collections.Generic;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Animations;
using Presentation.SkinEngine.SkinManagement;
using Presentation.SkinEngine.Xaml;

namespace Presentation.SkinEngine
{
  /// <summary>
  /// Context for a single animation. This data class holds
  /// the animation (the <see cref="Timeline"/>) and its according
  /// <see cref="TimelineContext"/>. It also contains a set of animations, which will have to
  /// be finished before this animation can start.
  /// </summary>
  public class AnimationContext
  {
    protected Timeline _timeline;
    protected TimelineContext _timelineContext;
    protected ICollection<AnimationContext> _waitingFor = new List<AnimationContext>();

    /// <summary>
    /// The timeline to be executed in this animation.
    /// </summary>
    public Timeline Timeline
    {
      get { return _timeline; }
      set { _timeline = value; }
    }

    /// <summary>
    /// The animation's context to be used in the <see cref="Timeline"/> for the current animation.
    /// </summary>
    public TimelineContext TimelineContext
    {
      get { return _timelineContext; }
      set { _timelineContext = value; }
    }

    /// <summary>
    /// Stores a collection of animations, which have to be finished before this animation
    /// is able to run.
    /// </summary>
    /// <remarks>
    /// This animation remains delayed until the returned collection is empty.
    /// </remarks>
    public ICollection<AnimationContext> WaitingFor
    {
      get { return _waitingFor; }
    }
  }

  /// <summary>
  /// Management class for a set of running animations.
  /// </summary>
  public class Animator
  {
    protected List<AnimationContext> _scheduledAnimations;

    public Animator()
    {
      _scheduledAnimations = new List<AnimationContext>();
    }

    protected AnimationContext GetContext(Timeline line, UIElement element)
    {
      lock (_scheduledAnimations)
      {
        foreach (AnimationContext context in _scheduledAnimations)
          if (context.Timeline == line && context.TimelineContext.VisualParent == element) return context;
      }
      return null;
    }

    /// <summary>
    /// Starts the specified <paramref name="board"/> in the context of the specified
    /// <paramref name="element"/>.
    /// </summary>
    /// <remarks>
    /// Depending on the parameter handoffBehavior, the new storyboard will
    /// be started when the last other storyboard, which occupies a conflicting property,
    /// has finished.
    /// </remarks>
    /// <param name="board">The storyboard to start.</param>
    /// <param name="element">Context element which will be used as
    /// <see cref="TimelineContext.VisualParent"/> for the new <paramref name="board"/>.</param>
    /// <param name="handoffBehavior">Controls how the new storyboard animation will be
    /// attached to already running animations, if there are conflicting properties animated
    /// by an already running anmiation an by the new <paramref name="board"/>.</param>
    public void StartStoryboard(Storyboard board, UIElement element,
        HandoffBehavior handoffBehavior)
    {
      lock (_scheduledAnimations)
      {
        AnimationContext context = new AnimationContext();
        context.Timeline = board;
        context.TimelineContext = board.CreateTimelineContext(element);

        IDictionary<IDataDescriptor, object> conflictingProperties =
            HandleConflicts(context, handoffBehavior);

        board.Setup(context.TimelineContext, conflictingProperties);

        _scheduledAnimations.Add(context);
        board.Start(context.TimelineContext, SkinContext.TimePassed);
      }
    }

    /// <summary>
    /// Stops the specified <paramref name="board"/> which runs in the context of the
    /// given <paramref name="element"/>.
    /// </summary>
    /// <param name="board">The storyboard to stop.</param>
    /// <param name="element">Context element on which the <paramref name="board"/> runs.</param>
    public void StopStoryboard(Storyboard board, UIElement element)
    {
      lock (_scheduledAnimations)
      {
        AnimationContext context = GetContext(board, element);
        if (context == null) return;
        _scheduledAnimations.Remove(context);
        context.Timeline.Stop(context.TimelineContext);
      }
    }

    public void StopAll()
    {
      lock (_scheduledAnimations)
      {
        foreach (AnimationContext ac in _scheduledAnimations)
          ac.Timeline.Stop(ac.TimelineContext);
        _scheduledAnimations.Clear();
      }
    }

    // For performance reasons, store those local variables as fields
    private readonly IList<AnimationContext> finishedAnimations = new List<AnimationContext>();
    private readonly IList<AnimationContext> removingWaitForAnimations = new List<AnimationContext>();

    /// <summary>
    /// Animates all timelines. This method will be called periodically to do all animation work.
    /// </summary>
    public void Animate()
    {
      lock (_scheduledAnimations)
      {
        if (_scheduledAnimations.Count == 0) return;
        foreach (AnimationContext ac in _scheduledAnimations)
        {
          // Tidy up wait dependencies
          foreach (AnimationContext waitForAc in ac.WaitingFor)
            if (!_scheduledAnimations.Contains(waitForAc))
              removingWaitForAnimations.Add(waitForAc);
          foreach (AnimationContext removeAc in removingWaitForAnimations)
            ac.WaitingFor.Remove(removeAc);
          removingWaitForAnimations.Clear();
          if (ac.WaitingFor.Count > 0)
            continue;
          // Animate timeline
          ac.Timeline.Animate(ac.TimelineContext, SkinContext.TimePassed);
          if (ac.Timeline.IsStopped(ac.TimelineContext))
            finishedAnimations.Add(ac);
        }
        foreach (AnimationContext ac in finishedAnimations)
        {
          ac.Timeline.Finish(ac.TimelineContext);
          _scheduledAnimations.Remove(ac);
        }
      }
      finishedAnimations.Clear();
    }

    /// <summary>
    /// Will check the specified <paramref name="animationContext"/> for conflicts with already
    /// scheduled animations. If there are conflicting animations, this method will stop them
    /// (in case <c><paramref name="handoffBehavior"/>==<see cref="HandoffBehavior.SnapshotAndReplace"/></c>)
    /// or add them to the wait set for the given <paramref name="animationContext"/>
    /// (in case <c><paramref name="handoffBehavior"/>==<see cref="HandoffBehavior.Compose"/></c>).
    /// The handoff behavior <see cref="HandoffBehavior.TemporaryReplace"/> will make the new
    /// animation run, letting conflicting animations proceed when the new animation has finished.
    /// </summary>
    /// <param name="animationContext">The new animation context to check against the running
    /// animations.</param>
    /// <param name="handoffBehavior">The handoff behavior which defines what will be done
    /// with conflicting animations.</param>
    /// <returns>Conflicting data descriptors mapped to their original value. This return value
    /// can be used to initialize the original values of the new animation.</returns>
    protected IDictionary<IDataDescriptor, object> HandleConflicts(
        AnimationContext animationContext, HandoffBehavior handoffBehavior)
    {
      Timeline line = animationContext.Timeline;
      TimelineContext context = animationContext.TimelineContext;
      IDictionary<IDataDescriptor, object> newProperties = new Dictionary<IDataDescriptor, object>();
      line.AddAllAnimatedProperties(context, newProperties);
      ICollection<AnimationContext> conflictingAnimations = new List<AnimationContext>();
      IDictionary<IDataDescriptor, object> conflictingProperties = new Dictionary<IDataDescriptor, object>();
      lock (_scheduledAnimations)
      {
        // Find conflicting animations and conflicting animated properties
        foreach (AnimationContext ac in _scheduledAnimations)
        {
          IDictionary<IDataDescriptor, object> animProperties = new Dictionary<IDataDescriptor, object>();
          ac.Timeline.AddAllAnimatedProperties(ac.TimelineContext, animProperties);
          ICollection<IDataDescriptor> conflicts = Intersection(
              newProperties.Keys, animProperties.Keys);
              // Intersection can be calculated with the Intersect extension method from the beginning of .net 3.5
          if (conflicts.Count > 0)
          {
            conflictingAnimations.Add(ac);
            foreach (IDataDescriptor prop in conflicts)
              conflictingProperties.Add(prop, animProperties[prop]);
          }
        }
      }
      // Do the handoff depending on HandoffBehavior
      if (handoffBehavior == HandoffBehavior.Compose)
        foreach (AnimationContext ac in conflictingAnimations)
          animationContext.WaitingFor.Add(ac);
      else if (handoffBehavior == HandoffBehavior.TemporaryReplace)
        foreach (AnimationContext ac in conflictingAnimations)
          ac.WaitingFor.Add(animationContext);
      else if (handoffBehavior == HandoffBehavior.SnapshotAndReplace)
        foreach (AnimationContext ac in conflictingAnimations)
          ac.Timeline.Finish(ac.TimelineContext);
      else
        throw new NotImplementedException("Animator.HandleConflicts: handoff behavior '" + handoffBehavior.ToString() +
                                          "' is not implemented");
      return conflictingProperties;
    }

    protected static ICollection<T> Intersection<T>(ICollection<T> c1, ICollection<T> c2)
    {
      ICollection<T> result = new List<T>();
      foreach (T o in c1)
        if (c2.Contains(o))
          result.Add(o);
      return result;
    }
  }
}
