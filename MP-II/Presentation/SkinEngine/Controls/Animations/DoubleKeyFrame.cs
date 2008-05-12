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

using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.Controls.Animations
{
  public class DoubleKeyFrame : KeyFrameBase, IKeyFrame
  {
    Property _keyValueProperty;

    #region ctor
    public DoubleKeyFrame(): base()
    {
      Init();
    }

    public DoubleKeyFrame(DoubleKeyFrame k): base(k)
    {
      Init();
      Value = k.Value;
    }

    void Init()
    {
      _keyValueProperty = new Property(typeof(double), 0.0);
    }

    #endregion

    #region Properties

    public Property ValueProperty
    {
      get { return _keyValueProperty; }
    }

    public double Value
    {
      get { return (double)_keyValueProperty.GetValue(); }
      set { _keyValueProperty.SetValue(value); }
    }

    object IKeyFrame.Value
    {
      get { return this.Value; }
      set { this.Value = (double)value; }
    }

    #endregion

    public virtual double Interpolate(double start, double keyframe)
    {
      return 0;
    }
  }
}
