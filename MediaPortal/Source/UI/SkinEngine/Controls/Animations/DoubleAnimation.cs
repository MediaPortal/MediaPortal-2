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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class DoubleAnimation : PropertyAnimationTimeline
  {
    #region Protected fields

    protected AbstractProperty _fromProperty;
    protected AbstractProperty _toProperty;
    protected AbstractProperty _byProperty;
    protected AbstractProperty _easingFunctionProperty;

    #endregion

    #region Ctor

    public DoubleAnimation()
    {
      Init();
    }

    void Init()
    {
      _fromProperty = new SProperty(typeof(double?), null);
      _toProperty = new SProperty(typeof(double?), null);
      _byProperty = new SProperty(typeof(double?), null);
      _easingFunctionProperty = new SProperty(typeof(IEasingFunction), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DoubleAnimation a = (DoubleAnimation) source;
      From = a.From;
      To = a.To;
      By = a.By;
      EasingFunction = copyManager.GetCopy(a.EasingFunction);
    }

    #endregion

    #region Public properties

    public AbstractProperty FromProperty
    {
      get { return _fromProperty; }
    }

    public double? From
    {
      get { return (double?) _fromProperty.GetValue(); }
      set { _fromProperty.SetValue(value); }
    }


    public AbstractProperty ToProperty
    {
      get { return _toProperty; }
    }

    public double? To
    {
      get { return (double?) _toProperty.GetValue(); }
      set { _toProperty.SetValue(value); }
    }

    public AbstractProperty ByProperty
    {
      get { return _byProperty; }
    }

    public double? By
    {
      get { return (double?) _byProperty.GetValue(); }
      set { _byProperty.SetValue(value); }
    }

    public AbstractProperty EasingFunctionProperty
    {
      get { return _easingFunctionProperty; }
    }

    public IEasingFunction EasingFunction
    {
      get { return (IEasingFunction)_easingFunctionProperty.GetValue(); }
      set { _easingFunctionProperty.SetValue(value); }
    }

    #endregion

    #region Animation properties

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;

      double from = From ?? (double) patc.StartValue;
      double to = To ?? (By.HasValue ? from + By.Value : (double) patc.OriginalValue);
      
      double duration = Duration.TotalMilliseconds;
      if (timepassed > duration)
      {
        patc.DataDescriptor.Value = to;
        return;
      }

      double progress = timepassed / duration;

      IEasingFunction easingFunction = EasingFunction;
      if (easingFunction != null)
        progress = easingFunction.Ease(progress);

      double dist = to - from;
      dist *= progress;
      dist += from;

      patc.DataDescriptor.Value = dist;
    }

    #endregion
  }
}
