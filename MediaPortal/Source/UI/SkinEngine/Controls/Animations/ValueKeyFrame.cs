#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public abstract class ValueKeyFrame<T> : DependencyObject
  {
    #region Protected fields

    protected AbstractProperty _keyTimeProperty;
    protected AbstractProperty _keyValueProperty;

    #endregion

    #region Ctor

    protected ValueKeyFrame()
    {
      Init();
    }

    void Init()
    {
      _keyTimeProperty = new SProperty(typeof(TimeSpan), new TimeSpan(0, 0, 0));
      _keyValueProperty = new SProperty(typeof(T), null); // Will be initialized in subclasses
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ValueKeyFrame<T> kf = (ValueKeyFrame<T>) source;
      KeyTime = kf.KeyTime;
      Value = typeof(T).IsPrimitive ? kf.Value : copyManager.GetCopy(kf.Value);
    }

    #endregion

    #region Properties

    public AbstractProperty KeyTimeProperty
    {
      get { return _keyTimeProperty; }
    }

    public AbstractProperty ValueProperty
    {
      get { return _keyValueProperty; }
    }

    public T Value
    {
      get { return (T) _keyValueProperty.GetValue(); }
      set { _keyValueProperty.SetValue(value); }
    }

    public TimeSpan KeyTime
    {
      get { return (TimeSpan)_keyTimeProperty.GetValue(); }
      set { _keyTimeProperty.SetValue(value); }
    }

    #endregion
  }
}
