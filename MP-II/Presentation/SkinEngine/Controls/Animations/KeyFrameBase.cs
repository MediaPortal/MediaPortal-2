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

using System;
using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.Controls.Animations
{
  public abstract class KeyFrameBase: DependencyObject
  {
    Property _keyTimeProperty;

    #region Ctor

    public KeyFrameBase(): base()
    {
      Init();
    }

    public KeyFrameBase(KeyFrameBase k): base(k)
    {
      Init();
      KeyTime = k.KeyTime;
    }

    void Init()
    {
      _keyTimeProperty = new Property(typeof(TimeSpan), new TimeSpan(0, 0, 0));
    }

    #endregion

    #region properties

    public Property KeyTimeProperty
    {
      get { return _keyTimeProperty; }
    }

    public TimeSpan KeyTime
    {
      get { return (TimeSpan)_keyTimeProperty.GetValue(); }
      set { _keyTimeProperty.SetValue(value); }
    }

    #endregion
  }
}
