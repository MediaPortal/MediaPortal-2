#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Animations;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
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
  /// Management class for a collection of active animations.
  /// </summary>
  /// <remarks>
  /// We manage a collection of animations in different states. Stopped animations will be cleaned
  /// up automatically.
  /// Animations in state <see cref="State.Ended"/> will remain in the collection until new instructions
  /// for the animation arrive. This makes every animation with <see cref="FillBehavior.HoldEnd"/> stay in
  /// the collection of animations until either it is stopped explicitly or another conflicting animation
  /// is started.
  /// </remarks>
  public class Animator
  {
    protected object _syncObject = new object();
    protected IList<AnimationContext> _scheduledAnimations;
    protected IDictionary<IDataDescriptor, object> _valuesToSet;

    public Animator()
    {
      _scheduledAnimations = new List<AnimationContext>();
      _valuesToSet = new Dictionary<IDataDescriptor, object>();
    }

    public void Dispose()
    {
      foreach (object value in _valuesToSet.Values)
        MPF.TryCleanupAndDispose(value);
      // 2011-09-10 Albert TODO: Proper disposal of animation contexts. Not necessary at the moment because currently,
      // no animation can cope with disposable objects. As soon as there are such animations, disposal should be implemented for AnimationContexts.
      //foreach (AnimationContext context in _scheduledAnimations)
      //  context.Dispose();
    }

    /// <summary>
    /// Mutex on which the animator class synchronizes multithread access.
    /// </summary>
    public object SyncObject
    {
      get { return _syncObject; }
    }

    /// <summary>
    /// Starts the specified <paramref name="board"/> in the context of the specified
    /// <paramref name="element"/>.
    /// </summary>
    /// <remarks>
    /// Depending on the parameter <paramref name="handoffBehavior"/>, the new storyboard will
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
      lock (_syncObject)
      {
        AnimationContext context = new AnimationContext
          {
              Timeline = board,
              TimelineContext = board.CreateTimelineContext(element)
          };

        IDictionary<IDataDescriptor, object> conflictingProperties;
        ICollection<AnimationContext> conflictingAnimations;
        FindConflicts(context, out conflictingAnimations, out conflictingProperties);
        ExecuteHandoff(context, conflictingAnimations, handoffBehavior);

        board.Setup(context.TimelineContext, conflictingProperties);

        _scheduledAnimations.Add(context);
        board.Start(context.TimelineContext, SkinContext.SystemTickCount);

        // No animation here - has to be done in the Animate method
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
      lock (_syncObject)
      {
        AnimationContext context = GetContext(board, element);
        if (context == null) return;
        ResetAllValues(context);
        _scheduledAnimations.Remove(context);
      }
    }

    /// <summary>
    /// Stops all storyboards.
    /// </summary>
    public void StopAll()
    {
      lock (_syncObject)
      {
        foreach (AnimationContext ac in _scheduledAnimations)
          ResetAllValues(ac);
        _scheduledAnimations.Clear();
      }
    }

    /// <summary>
    /// Stops all storyboards of the given <paramref name="element"/>.
    /// </summary>
    public void StopAll(UIElement element)
    {
      lock (_syncObject)
      {
        List<AnimationContext> removeContexts = new List<AnimationContext>(_scheduledAnimations.Where(ac => ac.TimelineContext.VisualParent == element));
        foreach (AnimationContext ac in removeContexts)
        {
          _scheduledAnimations.Remove(ac);
          ResetAllValues(ac);
        }
      }
    }

    /// <summary>
    /// Schedules the specified <paramref name="value"/> to be set at the specified
    /// <paramref name="dataDescriptor"/> the next time the <see cref="Animate"/> method runs.
    /// If the specified <paramref name="dataDescriptor"/> is already scheduled to be set to another
    /// value, the other job will be overridden by this call.
    /// </summary>
    /// <param name="dataDescriptor">Data descriptor to be used as target for the
    /// <paramref name="value"/>.</param>
    /// <param name="value">Value to be set.</param>
    public void SetValue(IDataDescriptor dataDescriptor, object value)
    {
      lock (_syncObject)
      {
        object curVal;
        if (_valuesToSet.TryGetValue(dataDescriptor, out curVal))
        {
          if (curVal == value)
            return;
          MPF.TryCleanupAndDispose(curVal);
        }
        else if (dataDescriptor.Value == value)
          return;
        _valuesToSet[dataDescriptor] = value;
      }
    }

    /// <summary>
    /// Returns the information if we have a value pending to be set. This can be the case if
    /// <see cref="SetValue"/> was called before with the specified <paramref name="dataDescriptor"/>
    /// but the <see cref="Animate"/> method wasn't called since then.
    /// </summary>
    /// <param name="dataDescriptor">Data descriptor to search for.</param>
    /// <param name="value">Will be set to the value which is pending to be set to the
    /// <paramref name="dataDescriptor"/>.</param>
    /// <returns><c>true</c>, if the method found a pending value for the specified
    /// <paramref name="dataDescriptor"/>, else <c>false</c>.</returns>
    public bool TryGetPendingValue(IDataDescriptor dataDescriptor, out object value)
    {
      lock (_syncObject)
        return _valuesToSet.TryGetValue(dataDescriptor, out value);
    }

    /// <summary>
    /// Animates all timelines and sets all scheduled values to set.
    /// This method has to be called periodically to do all animation work.
    /// </summary>
    public void Animate()
    {
      lock (_syncObject)
      {
        foreach (AnimationContext ac in _scheduledAnimations)
        {
          if (IsWaiting(ac))
            continue;
          // Animate timeline
          ac.Timeline.Animate(ac.TimelineContext, SkinContext.SystemTickCount);
          if (ac.Timeline.IsStopped(ac.TimelineContext))
            // Only remove stopped animations here, not ended animations. Ended animations
            // will remain active.
            stoppedAnimations.Add(ac);
        }
        foreach (AnimationContext ac in stoppedAnimations)
        {
          ac.Timeline.Finish(ac.TimelineContext);
          _scheduledAnimations.Remove(ac);
        }
        stoppedAnimations.Clear();
      }
    }

    public void SetValues()
    {
      // We need to copy the values dictionary, because we need to execute the setters outside our lock.
      // Furthermore could the setting of each value cause changes to the values dictionary.
      IDictionary<IDataDescriptor, object> values = new Dictionary<IDataDescriptor, object>();
      lock (_syncObject)
      {
        CollectionUtils.AddAll(values, _valuesToSet);
        _valuesToSet.Clear();
      }
      foreach (KeyValuePair<IDataDescriptor, object> valueToSet in values) // Outside the lock - will change properties in the screen
        DependencyObject.SetDataDescriptorValueWithLP(valueToSet.Key, valueToSet.Value);
    }

    protected void ResetAllValues(AnimationContext ac)
    {
      IDictionary<IDataDescriptor, object> animProperties = new Dictionary<IDataDescriptor, object>();
      ac.Timeline.AddAllAnimatedProperties(ac.TimelineContext, animProperties);
      foreach (KeyValuePair<IDataDescriptor, object> animProperty in animProperties)
        SetValue(animProperty.Key, animProperty.Value);
    }

    protected AnimationContext GetContext(Timeline line, UIElement element)
    {
      lock (_syncObject)
        return _scheduledAnimations.FirstOrDefault(context => context.Timeline == line && context.TimelineContext.VisualParent == element);
    }

    // For performance reasons, store those local variables as fields
    private readonly IList<AnimationContext> stoppedAnimations = new List<AnimationContext>();
    private readonly IList<AnimationContext> endedWaitForAnimations = new List<AnimationContext>();

    /// <summary>
    /// Checks the state of all wait dependencies for the specified animation
    /// <paramref name="context"/> and tidies up the wait hierarchy, if appropriate.
    /// </summary>
    /// <returns><c>true</c>, if the specified animation is ready to be animated, else <c>false</c>.</returns>
    protected bool IsWaiting(AnimationContext context)
    {
      // Tidy up wait dependencies
      if (context.WaitingFor.Count == 0)
        return false;

      bool allEndedOrStopped = true;
      foreach (AnimationContext waitForAc in context.WaitingFor)
      {
        int index = _scheduledAnimations.IndexOf(waitForAc);
        AnimationContext ac;
        if (index != -1)
          if ((ac = _scheduledAnimations[index]).Timeline.HasEnded(ac.TimelineContext))
            endedWaitForAnimations.Add(waitForAc);
          else
          {
            allEndedOrStopped = false;
            break;
          }
      }
      try
      {
        if (allEndedOrStopped)
        {
          // Stop all parent animations at once via the ExecuteHandoff method, when the last
          // one ended. This will preserve all animations with FillBehavior.HoldEnd until
          // the new animation starts.
          context.WaitingFor.Clear();
          ExecuteHandoff(context, endedWaitForAnimations, HandoffBehavior.SnapshotAndReplace);
          return false;
        }
        else
          // Animation isn't ready yet.
          return true;
      }
      finally
      {
        endedWaitForAnimations.Clear();
      }
    }

    /// <summary>
    /// Will check the specified <paramref name="animationContext"/> for conflicts with already
    /// scheduled animations and returns those conflicts.
    /// </summary>
    /// <param name="animationContext">The new animation context to check against the running
    /// animations.</param>
    /// <param name="conflictingAnimations">Returns all already running or sleeping animations with
    /// conflicting properties.</param>
    /// <param name="conflictingProperties">Conflicting data descriptors mapped to their original
    /// values. This returned value can be used to initialize the original values of the new animation.</param>
    protected void FindConflicts(
        AnimationContext animationContext,
        out ICollection<AnimationContext> conflictingAnimations,
        out IDictionary<IDataDescriptor, object> conflictingProperties)
    {
      Timeline newTL = animationContext.Timeline;
      TimelineContext context = animationContext.TimelineContext;
      IDictionary<IDataDescriptor, object> newProperties = new Dictionary<IDataDescriptor, object>();
      newTL.AddAllAnimatedProperties(context, newProperties);
      ICollection<IDataDescriptor> newPDs = newProperties.Keys;
      conflictingAnimations = new List<AnimationContext>();
      conflictingProperties = new Dictionary<IDataDescriptor, object>();
      lock (_syncObject)
      {
        // Find conflicting properties in the values to be set
        foreach (KeyValuePair<IDataDescriptor, object> property in new Dictionary<IDataDescriptor, object>(_valuesToSet))
        {
          if (!newPDs.Contains(property.Key))
            continue;
          conflictingProperties[property.Key] = property.Value;
          _valuesToSet.Remove(property.Key);
        }
        // Find conflicting animations and conflicting animated properties
        foreach (AnimationContext ac in _scheduledAnimations)
        {
          IDictionary<IDataDescriptor, object> animProperties = new Dictionary<IDataDescriptor, object>();
          ac.Timeline.AddAllAnimatedProperties(ac.TimelineContext, animProperties);
          bool isConflict = false;
          foreach (KeyValuePair<IDataDescriptor, object> animProperty in animProperties)
          {
            if (!newPDs.Contains(animProperty.Key))
              continue;
            isConflict = true;
            conflictingProperties[animProperty.Key] = animProperty.Value;
          }
          if (isConflict)
            conflictingAnimations.Add(ac);
        }
      }
    }

    /// <summary>
    /// Handles the handoff between conflicting animations.
    /// This method will, depending on the specified <paramref name="handoffBehavior"/>, stop conflicting
    /// animations (in case see cref="HandoffBehavior.SnapshotAndReplace"/>)
    /// or add them to the wait set for the given <paramref name="animationContext"/>
    /// (in case <see cref="HandoffBehavior.Compose"/>).
    /// The handoff behavior <see cref="HandoffBehavior.TemporaryReplace"/> will stop the conflicting
    /// animations, let the new animation run, and re-schedule the conflicting animations after the new animation.
    /// </summary>
    protected void ExecuteHandoff(AnimationContext animationContext, ICollection<AnimationContext> conflictingAnimations,
        HandoffBehavior handoffBehavior)
    {
      // Do the handoff depending on HandoffBehavior
      switch (handoffBehavior)
      {
        case HandoffBehavior.Compose:
          foreach (AnimationContext ac in conflictingAnimations)
            animationContext.WaitingFor.Add(ac);
          break;
        case HandoffBehavior.TemporaryReplace:
          foreach (AnimationContext ac in conflictingAnimations)
            ac.WaitingFor.Add(animationContext);
          break;
        case HandoffBehavior.SnapshotAndReplace:
          // Reset values of conflicting animations
          foreach (AnimationContext ac in conflictingAnimations)
            ResetAllValues(ac);
          // And remove those values which are handled by the new animation -
          // avoids flickering
          IDictionary<IDataDescriptor, object> animProperties = new Dictionary<IDataDescriptor, object>();
          animationContext.Timeline.AddAllAnimatedProperties(animationContext.TimelineContext, animProperties);
          foreach (IDataDescriptor dd in animProperties.Keys)
            _valuesToSet.Remove(dd);
          break;
        default:
          throw new NotImplementedException("Animator.HandleConflicts: HandoffBehavior '" + handoffBehavior + "' is not implemented");
      }
    }
  }
}
