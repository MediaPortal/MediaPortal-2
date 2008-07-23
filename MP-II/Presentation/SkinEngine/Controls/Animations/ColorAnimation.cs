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

using System.Drawing;
using MediaPortal.Presentation.Properties;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Animations
{

  public class ColorAnimation : PropertyAnimationTimeline
  {
    #region Private fields

    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;

    #endregion

    #region Ctor

    public ColorAnimation()
    {
      Init();
    }

    void Init()
    {
      _fromProperty = new Property(typeof(Color), Color.Black);
      _toProperty = new Property(typeof(Color), Color.White);
      _byProperty = new Property(typeof(Color), Color.Beige);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ColorAnimation a = source as ColorAnimation;
      From = copyManager.GetCopy(a.From);
      To = copyManager.GetCopy(a.To);
      By = copyManager.GetCopy(a.By);
    }

    #endregion

    #region Public properties

    public Property FromProperty
    {
      get { return _fromProperty; }
    }

    public Color From
    {
      get { return (Color) _fromProperty.GetValue(); }
      set { _fromProperty.SetValue(value); }
    }

    public Property ToProperty
    {
      get { return _toProperty; }
    }

    public Color To
    {
      get { return (Color)_toProperty.GetValue(); }
      set { _toProperty.SetValue(value); }
    }

    public Property ByProperty
    {
      get { return _byProperty; }
    }

    public Color By
    {
      get { return (Color)_byProperty.GetValue(); }
      set { _byProperty.SetValue(value); }
    }

    #endregion

    #region Animation methods

    protected override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = context as PropertyAnimationTimelineContext;
      if (patc.DataDescriptor == null) return;
      Color c;
      double distA = ((double)(To.A - From.A)) / Duration.TotalMilliseconds;
      distA *= timepassed;
      distA += From.A;

      double distR = ((double)(To.R - From.R)) / Duration.TotalMilliseconds;
      distR *= timepassed;
      distR += From.R;

      double distG = ((double)(To.G - From.G)) / Duration.TotalMilliseconds;
      distG *= timepassed;
      distG += From.G;

      double distB = ((double)(To.B - From.B)) / Duration.TotalMilliseconds;
      distB *= timepassed;
      distB += From.B;

      c = Color.FromArgb((int)distA, (int)distR, (int)distG, (int)distB);

      patc.DataDescriptor.Value = c;
    }

    #endregion

  }
}

