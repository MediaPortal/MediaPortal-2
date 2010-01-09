#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{

  public class ColorAnimation : PropertyAnimationTimeline
  {
    #region Private fields

    AbstractProperty _fromProperty;
    AbstractProperty _toProperty;
    AbstractProperty _byProperty;

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
      From = copyManager.GetCopy(a.From);
      To = copyManager.GetCopy(a.To);
      By = copyManager.GetCopy(a.By);
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

    #region Animation methods

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;

      Color from = From ?? (Color) patc.StartValue;
      Color to = To ?? (By.HasValue ? Color.FromArgb(
          from.A + By.Value.A,
          from.R + By.Value.R,
          from.G + By.Value.G,
          from.B + By.Value.B) : (Color) patc.OriginalValue);

      double distA = (to.A - from.A) / Duration.TotalMilliseconds;
      distA *= timepassed;
      distA += from.A;

      double distR = (to.R - from.R) / Duration.TotalMilliseconds;
      distR *= timepassed;
      distR += from.R;

      double distG = (to.G - from.G) / Duration.TotalMilliseconds;
      distG *= timepassed;
      distG += from.G;

      double distB = (to.B - from.B) / Duration.TotalMilliseconds;
      distB *= timepassed;
      distB += from.B;

      patc.DataDescriptor.Value = Color.FromArgb((int) distA, (int) distR, (int) distG, (int) distB);
    }

    #endregion
  }
}

