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
using System.Drawing;

namespace Presentation.SkinEngine.Controls.Animations
{
  public class ColorKeyFrame : KeyFrameBase, IKeyFrame
  {
    Property _keyValueProperty;

    #region Ctor

    public ColorKeyFrame(): base()
    {
      Init();
    }

    public ColorKeyFrame(ColorKeyFrame k): base(k)
    {
      Init();
      Value = k.Value;
    }

    void Init()
    {
      _keyValueProperty = new Property(typeof(Color), Color.White);
    }

    #endregion

    #region properties

    public Property ValueProperty
    {
      get { return _keyValueProperty; }
    }

    public Color Value
    {
      get { return (Color)_keyValueProperty.GetValue(); }
      set { _keyValueProperty.SetValue(value); }
    }

    object IKeyFrame.Value
    {
      get { return this.Value; }
      set { this.Value = (Color)value; }
    }

    #endregion

    public virtual Color Interpolate(Color start, double keyframe)
    {
      return start;
    }
  }
}
