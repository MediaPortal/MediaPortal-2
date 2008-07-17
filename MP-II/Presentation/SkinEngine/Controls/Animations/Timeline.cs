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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Animations
{
  public enum RepeatBehavior { None, Forever };
  public enum FillBehavior { HoldEnd, Stop };

  public abstract class Timeline: DependencyObject
  {
    Property _beginTimeProperty;
    Property _accellerationProperty;
    Property _autoReverseProperty;
    Property _decelerationRatioProperty;
    Property _durationProperty;
    Property _repeatBehaviourProperty;
    Property _fillBehaviourProperty;

    #region Ctor

    public Timeline()
    {
      Init();
    }

    void Init()
    {
      _beginTimeProperty = new Property(typeof(TimeSpan), new TimeSpan(0, 0, 0));
      _accellerationProperty = new Property(typeof(double), 1.0);
      _autoReverseProperty = new Property(typeof(bool), false);
      _decelerationRatioProperty = new Property(typeof(double), 1.0);
      _durationProperty = new Property(typeof(TimeSpan), new TimeSpan(0, 0, 1));
      _repeatBehaviourProperty = new Property(typeof(RepeatBehavior), RepeatBehavior.None);
      _fillBehaviourProperty = new Property(typeof(FillBehavior), FillBehavior.HoldEnd);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Timeline t = source as Timeline;
      BeginTime = copyManager.GetCopy(t.BeginTime);
      Accelleration = copyManager.GetCopy(t.Accelleration);
      AutoReverse = copyManager.GetCopy(t.AutoReverse);
      DecelerationRatio = copyManager.GetCopy(t.DecelerationRatio);
      Duration = copyManager.GetCopy(t.Duration);
      FillBehavior = copyManager.GetCopy(t.FillBehavior);
      RepeatBehavior = copyManager.GetCopy(t.RepeatBehavior);
    }

    #endregion

    #region Public properties

    public Property BeginTimeProperty
    {
      get { return _beginTimeProperty; }
    }

    public TimeSpan BeginTime
    {
      get { return (TimeSpan)_beginTimeProperty.GetValue(); }
      set { _beginTimeProperty.SetValue(value); }
    }

    public Property AccellerationProperty
    {
      get { return _accellerationProperty; }
    }

    public double Accelleration
    {
      get { return (double)_accellerationProperty.GetValue(); }
      set { _accellerationProperty.SetValue(value); }
    }

    public Property AutoReverseProperty
    {
      get { return _autoReverseProperty; }
    }

    public bool AutoReverse
    {
      get { return (bool)_autoReverseProperty.GetValue(); }
      set { _autoReverseProperty.SetValue(value); }
    }

    public Property DecelerationRatioProperty
    {
      get { return _decelerationRatioProperty; }
      set { _decelerationRatioProperty = value; }
    }

    public double DecelerationRatio
    {
      get { return (double)_decelerationRatioProperty.GetValue(); }
      set { _decelerationRatioProperty.SetValue(value); }
    }

    public Property DurationProperty
    {
      get { return _durationProperty; }
    }

    public TimeSpan Duration
    {
      get { return (TimeSpan)_durationProperty.GetValue(); }
      set { _durationProperty.SetValue(value); }
    }

    public Property RepeatBehaviorProperty
    {
      get { return _repeatBehaviourProperty; }
    }

    public RepeatBehavior RepeatBehavior
    {
      get { return (RepeatBehavior) _repeatBehaviourProperty.GetValue(); }
      set { _repeatBehaviourProperty.SetValue(value); }
    }

    public Property FillBehaviourProperty
    {
      get { return _fillBehaviourProperty; }
    }

    public FillBehavior FillBehavior
    {
      get { return (FillBehavior) _fillBehaviourProperty.GetValue(); }
      set { _fillBehaviourProperty.SetValue(value); }
    }

    #endregion

    #region Animation control methods

    public void Start(TimelineContext context, uint timePassed)
    {
      Stop(context);
      Started(context, timePassed);
    }

    public void Stop(TimelineContext context)
    {
      if (IsStopped(context)) return;
      if (FillBehavior == FillBehavior.Stop)
        Reset(context);
      context.State = State.Idle;
    }

    #endregion

    #region Animation state methods

    /// <summary>
    /// Creates a timeline context object needed for this class of timeline.
    /// </summary>
    /// <returns>Instance of a subclass of <see cref="TimelineContext"/>, which will
    /// fit the need of this class.</returns>
    /// <remarks>
    /// The needs of <see cref="Timeline"/> subclasses for a context object are different.
    /// Timeline groups will, for example, employ a context class which can hold different
    /// contexts for each child. Animations will need to put current values in their context.
    /// So subclasses of <see cref="Timeline"/> will employ their own variation of a
    /// <see cref="TimelineContext"/>.
    /// The returned context object will be used throughout the animation for this
    /// class in every call to any animation method.
    /// </remarks>
    public abstract TimelineContext CreateTimelineContext(UIElement element);

    /// <summary>
    /// Sets up the specified <paramref name="context"/> object with all necessary
    /// values for this timeline.
    /// </summary>
    public abstract void Setup(TimelineContext context);

    /// <summary>
    /// Will restore the original values in all properties which have been animated
    /// by this timeline.
    /// </summary>
    /// <param name="context">Current animation context.</param>
    public virtual void Reset(TimelineContext context)
    { }

    /// <summary>
    /// Will do the real work of animating the underlaying property.
    /// </summary>
    /// <remarks>
    /// The animation progress should be calculated to the specified relative
    /// time. It is not necessary to evaluate time overflows or to revert
    /// the animation depending on properties; these calculations have been
    /// done before this method will be called and will be reflected in the
    /// <paramref name="reltime"/> parameter. 
    /// </remarks>
    /// <param name="reltime">This parameter holds the relative animation time in
    /// milliseconds from the <see cref="BeginTime"/> on, up to a maximum value
    /// of Duration.Milliseconds.</param>
    public virtual void Animate(TimelineContext context, uint reltime)
    { }

    public virtual void Started(TimelineContext context, uint timePassed)
    {
      context.TimeStarted = timePassed;
      context.State = State.WaitBegin;
    }

    /// <summary>
    /// Will be called if this timeline has ended.
    /// </summary>
    /// <param name="context">Current animation context.</param>
    public virtual void Ended(TimelineContext context)
    {
      if (IsStopped(context)) return;
      if (FillBehavior == FillBehavior.Stop)
        Reset(context);
    }

    /// <summary>
    /// Gets a value indicating whether this timeline is stopped. This method
    /// will be overridden by composed timelines which will depend on their
    /// composition parts.
    /// </summary>
    /// <param name="context">Current animation context.</param>
    /// <returns>
    /// <c>true</c> if this timeline is stopped; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsStopped(TimelineContext context)
    {
      return (context.State == State.Idle);
    }

    #endregion
  }
}
