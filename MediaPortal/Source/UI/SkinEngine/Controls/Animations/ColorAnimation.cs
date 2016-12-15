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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{

  public class ColorAnimation : PropertyAnimationTimeline
  {
    #region Protected fields

    protected AbstractProperty _fromProperty;
    protected AbstractProperty _toProperty;
    protected AbstractProperty _byProperty;

    #endregion

    #region Ctor

    public ColorAnimation()
    {
      Init();
    }

    void Init()
    {
      _fromProperty = new SProperty(typeof(Color?), null);
      _toProperty = new SProperty(typeof(Color?), null);
      _byProperty = new SProperty(typeof(Color?), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ColorAnimation a = (ColorAnimation) source;
      From = a.From;
      To = a.To;
      By = a.By;
    }

    #endregion

    #region Public properties

    public AbstractProperty FromProperty
    {
      get { return _fromProperty; }
    }

    public Color? From
    {
      get { return (Color?) _fromProperty.GetValue(); }
      set { _fromProperty.SetValue(value); }
    }

    public AbstractProperty ToProperty
    {
      get { return _toProperty; }
    }

    public Color? To
    {
      get { return (Color?) _toProperty.GetValue(); }
      set { _toProperty.SetValue(value); }
    }

    public AbstractProperty ByProperty
    {
      get { return _byProperty; }
    }

    public Color? By
    {
      get { return (Color?) _byProperty.GetValue(); }
      set { _byProperty.SetValue(value); }
    }

    #endregion

    public static Color ConvertToColor(object value)
    {
      if (value == null)
        return Color.Black;
      if (value is Color)
        return (Color) value;
      SolidColorBrush scb = value as SolidColorBrush;
      if (scb != null)
        return scb.Color;
      throw new InvalidCastException(string.Format("Cannot cast from {0} to Color", value.GetType()));
    }

    #region Animation methods

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;

      Color from = From ?? ConvertToColor(patc.StartValue);
      Color to = To ?? (By.HasValue ? ColorConverter.FromArgb(
          from.A + By.Value.A,
          from.R + By.Value.R,
          from.G + By.Value.G,
          from.B + By.Value.B) : (Color) patc.OriginalValue);

      double duration = Duration.TotalMilliseconds;
      if (timepassed > duration)
      {
        patc.DataDescriptor.Value = to;
        return;
      }

      float progress = timepassed / (float)duration;
      object value = Color.SmoothStep(from, to, progress);
      if (TypeConverter.Convert(value, patc.DataDescriptor.DataType, out value))
        patc.DataDescriptor.Value = value;
      else
        throw new InvalidCastException(string.Format("Cannot from {0} to {1}", value == null ? null : value.GetType(), patc.DataDescriptor.DataType));
      patc.DataDescriptor.Value = value;
    }

    #endregion
  }
}

