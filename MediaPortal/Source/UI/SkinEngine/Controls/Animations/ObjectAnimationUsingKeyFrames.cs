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

using System;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class ObjectAnimationUsingKeyFrames : PropertyAnimationTimeline, IAddChild<DiscreteObjectKeyFrame>
  {
    #region Protected fields

    protected AbstractProperty _keyFramesProperty;

    #endregion

    #region Ctor

    public ObjectAnimationUsingKeyFrames()
    {
      Init();
    }

    void Init()
    {
      _keyFramesProperty = new SProperty(typeof(ObjectKeyFrameCollection), new ObjectKeyFrameCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ObjectAnimationUsingKeyFrames a = (ObjectAnimationUsingKeyFrames) source;
      IList<DiscreteObjectKeyFrame> keyFrames = KeyFrames;
      foreach (DiscreteObjectKeyFrame kf in a.KeyFrames)
        keyFrames.Add(copyManager.GetCopy(kf));
    }

    public override void Dispose()
    {
      foreach (DiscreteObjectKeyFrame colorKeyFrame in KeyFrames)
        colorKeyFrame.Dispose();
      base.Dispose();
    }

    #endregion

    #region Public properties

    public AbstractProperty KeyFramesProperty
    {
      get { return _keyFramesProperty; }
    }

    public ObjectKeyFrameCollection KeyFrames
    {
      get { return (ObjectKeyFrameCollection)_keyFramesProperty.GetValue(); }
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

    public override void Setup(TimelineContext context, IDictionary<IDataDescriptor, object> propertyConfigurations)
    {
      base.Setup(context, propertyConfigurations);
      if (KeyFrames.Count > 0)
        Duration = KeyFrames[KeyFrames.Count - 1].KeyTime;
    }

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;
      foreach (DiscreteObjectKeyFrame key in KeyFrames)
      {
        if (key.KeyTime.TotalMilliseconds > timepassed)
          continue;
        object value = key.Value;
        if (patc.DataDescriptor.Value == value)
          return;
        if (TypeConverter.Convert(value, patc.DataDescriptor.DataType, out value))
          patc.DataDescriptor.Value = value;
        else
          throw new InvalidCastException(string.Format("Cannot from {0} to {1}", value == null ? null : value.GetType(), patc.DataDescriptor.DataType));
        return;
      }
    }

    #endregion

    #region IAddChild Members

    public void AddChild(DiscreteObjectKeyFrame o)
    {
      KeyFrames.Add(o);
    }

    #endregion
  }
}
