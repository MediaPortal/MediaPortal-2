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

using System.Collections.Generic;
using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  [ContentProperty("KeyFrames")]
  public class DoubleAnimationUsingKeyFrames: PropertyAnimationTimeline, IAddChild<DoubleKeyFrame>
  {
    #region Protected fields

    protected AbstractProperty _keyFramesProperty;

    #endregion

    #region Ctor

    public DoubleAnimationUsingKeyFrames()
    {
      Init();
    }

    void Init()
    {
      _keyFramesProperty = new SProperty(typeof(DoubleKeyFrameCollection), new DoubleKeyFrameCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DoubleAnimationUsingKeyFrames a = (DoubleAnimationUsingKeyFrames) source;
      IList<DoubleKeyFrame> keyFrames = KeyFrames;
      foreach (DoubleKeyFrame kf in a.KeyFrames)
        keyFrames.Add(copyManager.GetCopy(kf));
    }

    public override void Dispose()
    {
      foreach (DoubleKeyFrame doubleKeyFrame in KeyFrames)
        doubleKeyFrame.Dispose();
      base.Dispose();
    }

    #endregion

    #region Public properties

    public AbstractProperty KeyFramesProperty
    {
      get { return _keyFramesProperty; }
    }

    public DoubleKeyFrameCollection KeyFrames
    {
      get { return (DoubleKeyFrameCollection)_keyFramesProperty.GetValue(); }
    }

    public override double ActualDurationInMilliseconds
    {
      get
      {
        if (DurationSet)
          return Duration.TotalMilliseconds;
        return (KeyFrames.Count > 0) ? KeyFrames[KeyFrames.Count - 1].KeyTime.TotalMilliseconds : 0.0;
      }
    }

    #endregion

    #region Animation methods

    public override void Setup(TimelineContext context,
        IDictionary<IDataDescriptor, object> propertyConfigurations)
    {
      base.Setup(context, propertyConfigurations);
      if (KeyFrames.Count > 0)
        Duration = KeyFrames[KeyFrames.Count - 1].KeyTime;
    }

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;
      double time = 0;
      double start = (double) patc.StartValue;
      foreach (DoubleKeyFrame key in KeyFrames)
      {
        if (key.KeyTime.TotalMilliseconds >= timepassed)
        {
          double progress = timepassed - time;
          if (progress == 0)
            patc.DataDescriptor.Value = key.Value;
          else
          {
            progress /= key.KeyTime.TotalMilliseconds - time;
            double result = key.Interpolate(start, progress);
            patc.DataDescriptor.Value = result;
          }
          return;
        }
        time = key.KeyTime.TotalMilliseconds;
        start = key.Value;
      }
    }

    #endregion

    #region IAddChild Members

    public void AddChild(DoubleKeyFrame o)
    {
      KeyFrames.Add(o);
    }

    #endregion
  }
}
