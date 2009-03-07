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

using MediaPortal.Presentation.DataObjects;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class ProgressBar : Control
  {
    #region Private fields

    Property _valueProperty;
    Property _partIndicatorWidthProperty;

    #endregion

    #region Ctor

    public ProgressBar()
    {
      Init();
      Attach();
    }

    void Init()
    {
      Focusable = false;
      _valueProperty = new Property(typeof(float), 0.0f);
      _partIndicatorWidthProperty = new Property(typeof(float), 0.0f);
    }

    void Attach()
    {
      _valueProperty.Attach(OnValueChanged);
    }

    void Detach()
    {
      _valueProperty.Detach(OnValueChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ProgressBar pb = (ProgressBar) source;
      Value = copyManager.GetCopy(pb.Value);
      Attach();
    }

    #endregion

    void OnValueChanged(Property property, object oldValue)
    {
      PartIndicatorWidth = (float) (ActualWidth*Value/100.0);
    }

    public Property ValueProperty
    {
      get { return _valueProperty; }
    }

    public float Value
    {
      get { return (float) _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public Property PartIndicatorWidthProperty
    {
      get { return _partIndicatorWidthProperty; }
    }

    public float PartIndicatorWidth
    {
      get { return (float) _partIndicatorWidthProperty.GetValue(); }
      set { _partIndicatorWidthProperty.SetValue(value); }
    }
  }
}

