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

using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class DoubleAnimation : PropertyAnimationTimeline
  {
    #region Private fields

    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;

    #endregion

    #region Ctor

    public DoubleAnimation()
    {
      Init();
    }
    
    void Init()
    {
      _fromProperty = new Property(typeof(double?), null);
      _toProperty = new Property(typeof(double?), null);
      _byProperty = new Property(typeof(double?), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DoubleAnimation a = (DoubleAnimation) source;
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

    public double? From
    {
      get { return (double?) _fromProperty.GetValue(); }
      set { _fromProperty.SetValue(value); }
    }


    public Property ToProperty
    {
      get { return _toProperty; }
    }

    public double? To
    {
      get { return (double?) _toProperty.GetValue(); }
      set { _toProperty.SetValue(value); }
    }

    public Property ByProperty
    {
      get { return _byProperty; }
    }

    public double? By
    {
      get { return (double?) _byProperty.GetValue(); }
      set { _byProperty.SetValue(value); }
    }

    #endregion

    #region Animation properties

    internal override void DoAnimation(TimelineContext context, uint timepassed)
    {
      PropertyAnimationTimelineContext patc = (PropertyAnimationTimelineContext) context;
      if (patc.DataDescriptor == null) return;

      double from = From ?? (double)patc.StartValue;
      double to = To ?? (By.HasValue ? from + By.Value : (double) patc.OriginalValue);

      double dist = (to - from) / Duration.TotalMilliseconds;
      dist *= timepassed;
      dist += from;

      patc.DataDescriptor.Value = dist;
    }

    #endregion
  }
}
