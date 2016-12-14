#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Xaml;
using SharpDX;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class PointAnimationUsingKeyFrames : PropertyAnimationTimeline, IAddChild<PointKeyFrame>
  {
    #region Protected fields

    protected AbstractProperty _keyFramesProperty;

    #endregion

    #region Ctor

    public PointAnimationUsingKeyFrames()
    {
      Init();
    }

    void Init()
    {
      _keyFramesProperty = new SProperty(typeof(PointKeyFrameCollection), new PointKeyFrameCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PointAnimationUsingKeyFrames a = (PointAnimationUsingKeyFrames) source;
      IList<PointKeyFrame> keyFrames = KeyFrames;
      foreach (PointKeyFrame kf in a.KeyFrames)
        keyFrames.Add(copyManager.GetCopy(kf));
    }

    #endregion

    #region Public properties

    public AbstractProperty KeyFramesProperty
    {
      get { return _keyFramesProperty; }
    }

    public PointKeyFrameCollection KeyFrames
    {
      get { return (PointKeyFrameCollection)_keyFramesProperty.GetValue(); }
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
      Vector2 start = (Vector2) patc.StartValue;
      foreach (PointKeyFrame key in KeyFrames)
      {
        if (key.KeyTime.TotalMilliseconds >= timepassed)
        {
          double progress = timepassed - time;
          if (progress == 0)
            patc.DataDescriptor.Value = key.Value;
          else
          {
            progress /= key.KeyTime.TotalMilliseconds - time;
            Vector2 result = key.Interpolate(start, progress);
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

    public void AddChild(PointKeyFrame o)
    {
      KeyFrames.Add(o);
    }

    #endregion
  }
}
